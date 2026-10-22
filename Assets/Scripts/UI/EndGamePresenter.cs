using System;
using System.Globalization;
using SpaceExplorer.Audio;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class EndGamePresenter : MonoBehaviour
    {
        [Header("Result Labels")]
        [SerializeField] private Text _scoreLabel;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _survivalTimeLabel;
        [SerializeField] private Text _statusLabel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        private bool _transitionStarted;
        private bool _configurationErrorReported;

        // Gắn callback điều hướng, hiển thị kết quả và kiểm tra tham chiếu khi màn hình bật.
        private void OnEnable()
        {
            AddButtonListener(_playAgainButton, HandlePlayAgain);
            AddButtonListener(_mainMenuButton, HandleMainMenu);
            RefreshResultLabels();
            ReportMissingReferencesOnce();
        }

        // Đọc lại kết quả sau khi scene hoạt động để nhận manager tồn tại xuyên scene.
        private void Start()
        {
            RefreshResultLabels();
        }

        // Gỡ callback runtime khỏi hai nút khi màn hình bị tắt.
        private void OnDisable()
        {
            RemoveButtonListener(_playAgainButton, HandlePlayAgain);
            RemoveButtonListener(_mainMenuButton, HandleMainMenu);
        }

        // Đọc snapshot authoritative từ GameManager; màn hình vẫn dùng được nếu không có kết quả.
        private void RefreshResultLabels()
        {
            GameManager manager = GameManager.Instance;
            RunResult? result = manager != null ? manager.LastResult : null;

            if (!result.HasValue)
            {
                SetLabel(_scoreLabel, "Score: —");
                SetLabel(_levelLabel, "Level: —");
                SetLabel(_survivalTimeLabel, "Survival Time: —");
                SetLabel(_statusLabel, "No run result is available.");
                return;
            }

            RunResult value = result.Value;
            SetLabel(_scoreLabel, $"Score: {value.FinalScore.ToString("N0", CultureInfo.CurrentCulture)}");
            SetLabel(_levelLabel, $"Level: {value.LevelReached}");
            SetLabel(_survivalTimeLabel, $"Survival Time: {FormatSurvivalTime(value.SurvivalTimeSeconds)}");
            SetLabel(_statusLabel, string.Empty);
        }

        // Phát âm thanh xác nhận rồi yêu cầu mở Gameplay để chơi lại.
        private void HandlePlayAgain()
        {
            SfxAudioService.PlayClick();
            LoadScene(SceneLoader.Gameplay);
        }

        // Phát âm thanh xác nhận rồi yêu cầu quay về MainMenu.
        private void HandleMainMenu()
        {
            SfxAudioService.PlayClick();
            LoadScene(SceneLoader.MainMenu);
        }

        // Chặn click lặp trong lúc chuyển scene và mở lại nút nếu SceneLoader báo lỗi.
        private void LoadScene(string sceneName)
        {
            if (_transitionStarted)
                return;

            _transitionStarted = true;
            if (SceneLoader.TryLoad(sceneName, out string error))
                return;

            _transitionStarted = false;
            Debug.LogError($"EndGame could not load '{sceneName}': {error}", this);
        }

        // Thêm callback runtime sau khi gỡ bản trùng để mỗi nút chỉ gọi một lần.
        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        // Gỡ callback runtime nếu Button vẫn còn tồn tại.
        private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button != null)
                button.onClick.RemoveListener(callback);
        }

        // Gán nội dung cho nhãn khi tham chiếu đã được nối trong Inspector.
        private static void SetLabel(Text label, string value)
        {
            if (label != null)
                label.text = value;
        }

        // Báo thiếu nhãn hoặc nút một lần mà không chặn điều hướng còn dùng được.
        private void ReportMissingReferencesOnce()
        {
            if (_configurationErrorReported)
                return;

            if (_scoreLabel != null && _levelLabel != null && _survivalTimeLabel != null
                && _playAgainButton != null && _mainMenuButton != null)
                return;

            Debug.LogError("EndGamePresenter is missing one or more result labels or navigation buttons. Available buttons remain usable.", this);
            _configurationErrorReported = true;
        }

        // Định dạng thời gian thành giờ, phút và giây kể cả khi run dài hơn một giờ.
        private static string FormatSurvivalTime(float seconds)
        {
            double safeSeconds = seconds;
            if (double.IsNaN(safeSeconds) || double.IsInfinity(safeSeconds) || safeSeconds < 0d)
                safeSeconds = 0d;

            double wholeSecondsValue = Math.Floor(safeSeconds);
            long totalSeconds = wholeSecondsValue >= long.MaxValue
                ? long.MaxValue
                : (long)wholeSecondsValue;

            long totalMinutes = totalSeconds / 60;
            long hours = totalMinutes / 60;
            long minutes = totalMinutes % 60;
            long remainingSeconds = totalSeconds % 60;

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", hours, minutes, remainingSeconds);
        }
    }
}
