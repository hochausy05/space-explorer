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

        private void Awake()
        {
            SetPanelVisible(false);
            RefreshMuteLabel();
            ReportMissingReferencesOnce();
        }

        private void OnEnable()
        {
            AddButtonListener(_settingsButton, HandleSettings);
            AddButtonListener(_resumeButton, HandleResume);
            AddButtonListener(_muteButton, HandleMute);
            AddButtonListener(_exitButton, HandleExit);
            TryBindGameManager();
            RefreshSettingsButton();
        }

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

        private void Update()
        {
            TryBindGameManager();

            if (_settingsPanel != null && _settingsPanel.activeSelf
                && _gameManager != null && _gameManager.State == RunState.Finished)
                CloseAfterRunEnd();

            RefreshSettingsButton();
        }

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

        private void HandleMute()
        {
            // The click is heard when unmuting only if SFX were already enabled.
            SfxAudioService.PlayClick();
            SfxAudioService.SetMuted(!SfxAudioService.IsMuted);
            RefreshMuteLabel();
        }

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

            // Scene loading is synchronous here; abandon only after MainMenu is accepted so a
            // missing Build Settings entry cannot discard the still-running session.
            if (!manager.TryAbandonRun(out string abandonError))
                Debug.LogError($"Gameplay was left, but the run could not be abandoned: {abandonError}");
        }

        private void HandleRunStarted()
        {
            _hasPauseRequest = false; // GameManager clears all old pause requests when a run starts.
            _transitionStarted = false;
            SetPanelVisible(false);
            RefreshMuteLabel();
        }

        private void HandleRunFinished(RunResult result)
        {
            _hasPauseRequest = false; // Finishing a run invalidates every pause token.
            _transitionStarted = true;
            SetPanelVisible(false);
        }

        private void CloseAfterRunEnd()
        {
            _hasPauseRequest = false;
            _transitionStarted = true;
            SetPanelVisible(false);
        }

        private void RefreshSettingsButton()
        {
            if (_settingsButton == null)
                return;

            _settingsButton.interactable = _gameManager != null
                && _gameManager.State == RunState.Playing
                && _gameManager.ActivePauseRequestCount == 0
                && !_transitionStarted;
        }

        private void RefreshMuteLabel()
        {
            if (_muteButtonLabel != null)
                _muteButtonLabel.text = SfxAudioService.IsMuted ? "BẬT TIẾNG" : "TẮT TIẾNG";
        }

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

        private void UnbindGameManager()
        {
            if (_gameManager != null)
                _gameManager.RunFinished -= HandleRunFinished;

            GameManager.RunStarted -= HandleRunStarted;
            _gameManager = null;
        }

        private void ReportMissingReferencesOnce()
        {
            if (_configurationErrorReported || (_settingsButton != null && _settingsPanel != null
                && _resumeButton != null && _muteButton != null && _muteButtonLabel != null && _exitButton != null))
                return;

            ReportConfigurationError("GameplayPauseMenuController requires Settings, panel, Resume, Mute, mute label, and Exit references.");
        }

        private void ReportConfigurationError(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }

        private void SetPanelVisible(bool visible)
        {
            if (_settingsPanel != null && _settingsPanel.activeSelf != visible)
                _settingsPanel.SetActive(visible);
        }

        private static void AddButtonListener(Button button, UnityAction callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private static void RemoveButtonListener(Button button, UnityAction callback)
        {
            if (button != null)
                button.onClick.RemoveListener(callback);
        }
    }
}
