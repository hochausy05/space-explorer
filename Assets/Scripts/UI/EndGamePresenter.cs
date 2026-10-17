using System;
using System.Globalization;
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
        [SerializeField] private Button _quitButton;

        private bool _transitionStarted;
        private bool _configurationErrorReported;
        private bool _editorQuitMessageReported;

        private void OnEnable()
        {
            AddButtonListener(_playAgainButton, HandlePlayAgain);
            AddButtonListener(_mainMenuButton, HandleMainMenu);
            AddButtonListener(_quitButton, HandleQuit);
            RefreshResultLabels();
            ReportMissingReferencesOnce();
        }

        private void Start()
        {
            // Refresh after scene activation in case the persistent manager initialized later.
            RefreshResultLabels();
        }

        private void OnDisable()
        {
            RemoveButtonListener(_playAgainButton, HandlePlayAgain);
            RemoveButtonListener(_mainMenuButton, HandleMainMenu);
            RemoveButtonListener(_quitButton, HandleQuit);
        }

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

        private void HandlePlayAgain()
        {
            LoadScene(SceneLoader.Gameplay);
        }

        private void HandleMainMenu()
        {
            LoadScene(SceneLoader.MainMenu);
        }

        private void HandleQuit()
        {
#if UNITY_EDITOR
            if (!_editorQuitMessageReported)
            {
                Debug.Log("Quit is ignored in the Unity Editor. Verify this button in a standalone build.", this);
                _editorQuitMessageReported = true;
            }
#else
            if (_transitionStarted)
                return;

            _transitionStarted = true;
            Application.Quit();
#endif
        }

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

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button != null)
                button.onClick.RemoveListener(callback);
        }

        private static void SetLabel(Text label, string value)
        {
            if (label != null)
                label.text = value;
        }

        private void ReportMissingReferencesOnce()
        {
            if (_configurationErrorReported)
                return;

            if (_scoreLabel != null && _levelLabel != null && _survivalTimeLabel != null
                && _playAgainButton != null && _mainMenuButton != null && _quitButton != null)
                return;

            Debug.LogError("EndGamePresenter is missing one or more result labels or navigation buttons. Available buttons remain usable.", this);
            _configurationErrorReported = true;
        }

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
