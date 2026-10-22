using System;
using System.Collections.Generic;
using SpaceExplorer.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceExplorer.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.Ready;
        public RunResult? LastResult { get; private set; }
        public int CurrentScore { get; private set; }
        // Cho biết có bao nhiêu hệ thống đang giữ yêu cầu pause.
        public int ActivePauseRequestCount => _pauseRequests.Count;

        public static event Action RunStarted;
        public event Action<int> ScoreChanged;
        public event Action<RunResult> RunFinished;

        private readonly HashSet<int> _pauseRequests = new HashSet<int>();
        private int _nextPauseRequestId = 1;
        private PauseRequestToken? _legacyPauseRequest;

        public readonly struct PauseRequestToken
        {
            internal readonly GameManager Owner;
            internal readonly int Id;

        // Tạo token gắn với đúng GameManager để mỗi hệ thống giữ quyền pause riêng.
            internal PauseRequestToken(GameManager owner, int id)
            {
                Owner = owner;
                Id = id;
            }

            // Token chỉ hợp lệ khi có owner còn tồn tại và mã dương.
            public bool IsValid => Owner != null && Id > 0;
        }

        // Đặt singleton duy nhất và giữ GameManager qua các lần đổi scene.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        // Bắt đầu run nếu game được mở trực tiếp vào Gameplay.
        private void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

        // Bắt đầu run mới mỗi khi scene Gameplay được tải.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

        // Chuyển run về trạng thái bắt đầu rồi chạy lifecycle hiện có.
        private void BeginRunForGameplay()
        {
            if (State == RunState.Finished && !TryResetRun(out string resetError))
            {
                Debug.LogError(resetError, this);
                return;
            }

            if (State == RunState.Ready && !TryBeginRun(out string beginError))
                Debug.LogError(beginError, this);
        }

        // Gỡ đăng ký scene callback và xóa singleton khi instance bị hủy.
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        // Chỉ bắt đầu từ Ready; xóa kết quả cũ, reset Score và phát tín hiệu cho hệ thống trong run.
        public bool TryBeginRun(out string error)
        {
            if (State != RunState.Ready)
                return RejectTransition("begin a run", RunState.Ready, out error);

            LastResult = null;
            CurrentScore = 0;
            ScoreChanged?.Invoke(CurrentScore);
            _pauseRequests.Clear();
            _legacyPauseRequest = null;
            State = RunState.Playing;
            ApplyTimeScale();
            RunStarted?.Invoke();
            error = null;
            return true;
        }

        // Cộng điểm chỉ khi run đang Playing và số điểm hợp lệ.
        public bool TryAddScore(int amount)
        {
            if (State != RunState.Playing || amount <= 0)
                return false;

            CurrentScore += amount;
            ScoreChanged?.Invoke(CurrentScore);
            return true;
        }

        // Trừ điểm trong giới hạn không âm và phát thay đổi Score.
        public bool TryApplyScorePenalty(int amount, out string error)
        {
            if (State != RunState.Playing && State != RunState.Paused)
            {
                error = $"Cannot apply a Score penalty while the game is {State}. A run must be Playing or Paused.";
                return false;
            }

            if (amount < 0)
            {
                error = "A Score penalty cannot be negative.";
                return false;
            }

            int nextScore = Mathf.Max(0, CurrentScore - amount);
            if (nextScore != CurrentScore)
            {
                CurrentScore = nextScore;
                ScoreChanged?.Invoke(CurrentScore);
            }

            error = null;
            return true;
        }

        // Tạo yêu cầu pause tương thích cũ, không cho cùng caller pause hai lần.
        public bool TryPauseRun(out string error)
        {
            if (State != RunState.Playing && State != RunState.Paused)
                return RejectTransition("pause the run", RunState.Playing, out error);

            if (_legacyPauseRequest.HasValue)
            {
                error = "The legacy pause request is already active.";
                return false;
            }

            if (!TryAcquirePauseRequest(out PauseRequestToken token, out error))
                return false;

            _legacyPauseRequest = token;
            return true;
        }

        // Nhả đúng yêu cầu pause cũ do chính GameManager lưu giữ.
        public bool TryResumeRun(out string error)
        {
            if (State != RunState.Paused || !_legacyPauseRequest.HasValue)
            {
                error = $"Cannot resume the run while the game is {State} or without an owned pause request.";
                return false;
            }

            PauseRequestToken token = _legacyPauseRequest.Value;
            _legacyPauseRequest = null;
            return TryReleasePauseRequest(token, out error);
        }

        // Mỗi bên giữ token riêng để chỉ có thể gỡ yêu cầu pause do chính mình tạo.
        public bool TryAcquirePauseRequest(out PauseRequestToken token, out string error)
        {
            token = default;
            if (State != RunState.Playing && State != RunState.Paused)
            {
                error = $"Cannot pause while the game is {State}. A run must be Playing or Paused.";
                return false;
            }

            int id = _nextPauseRequestId++;
            if (_nextPauseRequestId <= 0)
                _nextPauseRequestId = 1;

            _pauseRequests.Add(id);
            token = new PauseRequestToken(this, id);
            State = RunState.Paused;
            ApplyTimeScale();
            error = null;
            return true;
        }

        // Chỉ trở lại Playing khi token hợp lệ được nhả và không còn bên nào yêu cầu pause.
        public bool TryReleasePauseRequest(PauseRequestToken token, out string error)
        {
            if (token.Owner != this || token.Id <= 0 || !_pauseRequests.Remove(token.Id))
            {
                error = "The pause request is invalid, already released, or owned by another GameManager.";
                return false;
            }

            if (_legacyPauseRequest.HasValue && _legacyPauseRequest.Value.Id == token.Id)
                _legacyPauseRequest = null;

            if (State == RunState.Paused && _pauseRequests.Count == 0)
                State = RunState.Playing;

            ApplyTimeScale();
            error = null;
            return true;
        }

        // Chốt snapshot, lưu lịch sử và phát sự kiện kết thúc chỉ khi run chưa kết thúc.
        public bool TryFinishRun(RunResult result, out string error)
        {
            if (State != RunState.Playing && State != RunState.Paused)
            {
                error = $"Cannot finish a run while the game is {State}. A run must be Playing or Paused.";
                return false;
            }

            LastResult = result;
            State = RunState.Finished;
            _pauseRequests.Clear();
            _legacyPauseRequest = null;
            ApplyTimeScale();
            error = null;
            RecentRunHistory.RecordCompletedRun(result);
            SfxAudioService.PlayGameOver();
            RunFinished?.Invoke(result);
            return true;
        }

        // Rời run chưa hoàn tất mà không ghi lịch sử hoặc tạo kết quả Game Over.
        public bool TryAbandonRun(out string error)
        {
            if (State != RunState.Playing && State != RunState.Paused)
            {
                error = $"Cannot abandon a run while the game is {State}. A run must be Playing or Paused.";
                return false;
            }

            State = RunState.Ready;
            LastResult = null;
            CurrentScore = 0;
            _pauseRequests.Clear();
            _legacyPauseRequest = null;
            ApplyTimeScale();
            ScoreChanged?.Invoke(CurrentScore);
            error = null;
            return true;
        }

        // Chuyển trạng thái run về Ready sau khi kết thúc.
        public bool TryResetRun(out string error)
        {
            if (State != RunState.Finished)
                return RejectTransition("reset the run", RunState.Finished, out error);

            LastResult = null;
            State = RunState.Ready;
            _pauseRequests.Clear();
            _legacyPauseRequest = null;
            ApplyTimeScale();
            error = null;
            return true;
        }

        // Đồng bộ timeScale với trạng thái Playing, Paused hoặc Finished.
        private void ApplyTimeScale()
        {
            Time.timeScale = State == RunState.Paused || State == RunState.Finished ? 0f : 1f;
        }

        // Tạo thông báo khi chuyển trạng thái không hợp lệ.
        private bool RejectTransition(string action, RunState requiredState, out string error)
        {
            error = $"Cannot {action} while the game is {State}. The required state is {requiredState}.";
            return false;
        }
    }
}
