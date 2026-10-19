using System;
using System.Collections.Generic;
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

            internal PauseRequestToken(GameManager owner, int id)
            {
                Owner = owner;
                Id = id;
            }

            public bool IsValid => Owner != null && Id > 0;
        }

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

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

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

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

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

        public bool TryAddScore(int amount)
        {
            if (State != RunState.Playing || amount <= 0)
                return false;

            CurrentScore += amount;
            ScoreChanged?.Invoke(CurrentScore);
            return true;
        }

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
            RunFinished?.Invoke(result);
            return true;
        }

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

        private void ApplyTimeScale()
        {
            Time.timeScale = State == RunState.Paused || State == RunState.Finished ? 0f : 1f;
        }

        private bool RejectTransition(string action, RunState requiredState, out string error)
        {
            error = $"Cannot {action} while the game is {State}. The required state is {requiredState}.";
            return false;
        }
    }
}
