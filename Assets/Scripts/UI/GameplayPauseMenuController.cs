using SpaceExplorer.Audio;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class GameplayPauseMenuController : MonoBehaviour
    {
        [Header("Gameplay UI")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _muteButton;
        [SerializeField] private Text _muteButtonLabel;
        [SerializeField] private Button _exitButton;

        private GameManager _gameManager;
        private GameManager.PauseRequestToken _pauseToken;
        private bool _hasPauseRequest;
        private bool _transitionStarted;
        private bool _configurationErrorReported;

        // Ẩn Settings lúc đầu, cập nhật nhãn âm thanh và kiểm tra tham chiếu UI.
        private void Awake()
        {
            SetPanelVisible(false);
            RefreshMuteLabel();
            ReportMissingReferencesOnce();
        }

        // Gắn callback các nút và liên kết với GameManager đang quản lý run.
        private void OnEnable()
        {
            AddButtonListener(_settingsButton, HandleSettings);
            AddButtonListener(_resumeButton, HandleResume);
            AddButtonListener(_muteButton, HandleMute);
            AddButtonListener(_exitButton, HandleExit);
            TryBindGameManager();
            RefreshSettingsButton();
        }

        // Gỡ callback, trả pause token do menu sở hữu và ngắt liên kết sự kiện.
        private void OnDisable()
        {
            RemoveButtonListener(_settingsButton, HandleSettings);
            RemoveButtonListener(_resumeButton, HandleResume);
            RemoveButtonListener(_muteButton, HandleMute);
            RemoveButtonListener(_exitButton, HandleExit);

            if (_hasPauseRequest && !_transitionStarted && _gameManager != null
                && _gameManager.State != RunState.Finished)
            {
                _gameManager.TryReleasePauseRequest(_pauseToken, out _);
            }

            _hasPauseRequest = false;
            SetPanelVisible(false);
            UnbindGameManager();
        }

        // Theo dõi run kết thúc để đóng panel và cập nhật trạng thái nút Settings.
        private void Update()
        {
            TryBindGameManager();

            if (_settingsPanel != null && _settingsPanel.activeSelf
                && _gameManager != null && _gameManager.State == RunState.Finished)
                CloseAfterRunEnd();

            RefreshSettingsButton();
        }

        // Menu chỉ lấy pause token khi run đang chạy và chưa có owner pause khác.
        private void HandleSettings()
        {
            SfxAudioService.PlayClick();
            if (_gameManager == null && !TryBindGameManager())
                return;

            if (_transitionStarted || _settingsPanel == null || _settingsPanel.activeSelf
                || _gameManager.State != RunState.Playing || _gameManager.ActivePauseRequestCount != 0)
                return;

            if (!_gameManager.TryAcquirePauseRequest(out _pauseToken, out string error))
            {
                ReportConfigurationError($"Settings menu could not pause gameplay: {error}");
                return;
            }

            _hasPauseRequest = true;
            SetPanelVisible(true);
            RefreshSettingsButton();
        }

        // Hủy pause token của menu và tiếp tục gameplay nếu run còn hoạt động.
        private void HandleResume()
        {
            SfxAudioService.PlayClick();
            if (_transitionStarted || _gameManager == null || !_hasPauseRequest)
                return;

            if (!_gameManager.TryReleasePauseRequest(_pauseToken, out string error))
                ReportConfigurationError($"Settings menu could not release its pause request: {error}");

            _hasPauseRequest = false;
            SetPanelVisible(false);
            RefreshSettingsButton();
        }

        // Phát tiếng click, đổi trạng thái tắt tiếng và cập nhật nhãn nút.
        private void HandleMute()
        {
            // Tiếng click chỉ nghe được khi bật tiếng nếu SFX đang không bị tắt.
            SfxAudioService.PlayClick();
            SfxAudioService.SetMuted(!SfxAudioService.IsMuted);
            RefreshMuteLabel();
        }

        // Bỏ run chưa hoàn tất sau khi SceneLoader chấp nhận MainMenu để tránh mất dữ liệu do lỗi cấu hình.
        private void HandleExit()
        {
            SfxAudioService.PlayClick();
            if (_transitionStarted || _gameManager == null || !_hasPauseRequest)
                return;

            GameManager manager = _gameManager;
            _transitionStarted = true;
            if (!SceneLoader.TryLoad(SceneLoader.MainMenu, out string sceneError))
            {
                _transitionStarted = false;
                ReportConfigurationError($"Could not return to MainMenu: {sceneError}");
                return;
            }

            // Chỉ bỏ run sau khi SceneLoader nhận MainMenu để lỗi Build Settings không làm mất phiên đang chơi.
            if (!manager.TryAbandonRun(out string abandonError))
                Debug.LogError($"Gameplay was left, but the run could not be abandoned: {abandonError}");
        }

        // Xóa pause cũ và đóng Settings khi GameManager bắt đầu run mới.
        private void HandleRunStarted()
        {
            _hasPauseRequest = false; // GameManager đã xóa các pause request cũ khi bắt đầu run.
            _transitionStarted = false;
            SetPanelVisible(false);
            RefreshMuteLabel();
        }

        // Xóa trạng thái pause riêng và đóng Settings khi run kết thúc.
        private void HandleRunFinished(RunResult result)
        {
            _hasPauseRequest = false; // Kết thúc run làm mọi pause token hết hiệu lực.
            _transitionStarted = true;
            SetPanelVisible(false);
        }

        // Khóa menu và ẩn panel sau khi kết thúc run.
        private void CloseAfterRunEnd()
        {
            _hasPauseRequest = false;
            _transitionStarted = true;
            SetPanelVisible(false);
        }

        // Chỉ cho mở Settings khi run chạy và không có hệ thống khác giữ pause.
        private void RefreshSettingsButton()
        {
            if (_settingsButton == null)
                return;

            _settingsButton.interactable = _gameManager != null
                && _gameManager.State == RunState.Playing
                && _gameManager.ActivePauseRequestCount == 0
                && !_transitionStarted;
        }

        // Hiển thị hành động hiện tại để người chơi biết có thể bật hay tắt tiếng.
        private void RefreshMuteLabel()
        {
            if (_muteButtonLabel != null)
                _muteButtonLabel.text = SfxAudioService.IsMuted ? "BẬT TIẾNG" : "TẮT TIẾNG";
        }

        // Liên kết GameManager hiện tại và đăng ký sự kiện không trùng lặp.
        private bool TryBindGameManager()
        {
            GameManager authoritative = GameManager.Instance;
            if (ReferenceEquals(authoritative, _gameManager))
                return authoritative != null;

            UnbindGameManager();
            _gameManager = authoritative;
            if (_gameManager == null)
                return false;

            _gameManager.RunFinished += HandleRunFinished;
            GameManager.RunStarted += HandleRunStarted;
            if (_gameManager.State == RunState.Finished)
                CloseAfterRunEnd();
            return true;
        }

        // Hủy đăng ký sự kiện và xóa tham chiếu GameManager đã liên kết.
        private void UnbindGameManager()
        {
            if (_gameManager != null)
                _gameManager.RunFinished -= HandleRunFinished;

            GameManager.RunStarted -= HandleRunStarted;
            _gameManager = null;
        }

        // Kiểm tra đủ tham chiếu Settings và báo thiếu cấu hình một lần.
        private void ReportMissingReferencesOnce()
        {
            if (_configurationErrorReported || (_settingsButton != null && _settingsPanel != null
                && _resumeButton != null && _muteButton != null && _muteButtonLabel != null && _exitButton != null))
                return;

            ReportConfigurationError("GameplayPauseMenuController requires Settings, panel, Resume, Mute, mute label, and Exit references.");
        }

        // Ghi lỗi cấu hình tối đa một lần để tránh log lặp trong vòng đời menu.
        private void ReportConfigurationError(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }

        // Bật hoặc ẩn panel nếu trạng thái hiện tại khác yêu cầu.
        private void SetPanelVisible(bool visible)
        {
            if (_settingsPanel != null && _settingsPanel.activeSelf != visible)
                _settingsPanel.SetActive(visible);
        }

        // Đăng ký callback runtime mà không để lại bản đăng ký trùng.
        private static void AddButtonListener(Button button, UnityAction callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        // Gỡ callback runtime khi menu không còn hoạt động.
        private static void RemoveButtonListener(Button button, UnityAction callback)
        {
            if (button != null)
                button.onClick.RemoveListener(callback);
        }
    }
}
