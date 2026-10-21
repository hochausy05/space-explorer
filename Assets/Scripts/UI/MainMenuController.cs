using System.Collections.Generic;
using SpaceExplorer.Audio;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum InfoView
        {
            None,
            Instructions,
            Records
        }

        [Header("Navigation")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _instructionsButton;
        [SerializeField] private Button _recordsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _closeButton;

        [Header("Information Panel")]
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private GameObject _instructionsTitle;
        [SerializeField] private GameObject _instructionsText;
        [SerializeField] private GameObject _recordsContent;

        [Header("Recent Runs")]
        [SerializeField] private RectTransform _recordsListRoot;
        [SerializeField] private RecentRunRecordRow _recordRowTemplate;
        [SerializeField] private Text _recordsEmptyStateText;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>(RecentRunHistory.MaximumRecords);
        private InfoView _activeView;
        private bool _transitionStarted;
        private bool _configurationErrorReported;
        private bool _editorQuitMessageReported;

        private void Awake()
        {
            CloseInfoPanel();

            if (_recordRowTemplate != null)
                _recordRowTemplate.gameObject.SetActive(false);

            ReportMissingReferencesOnce();
        }

        private void OnEnable()
        {
            AddButtonListener(_playButton, HandlePlay);
            AddButtonListener(_instructionsButton, HandleInstructions);
            AddButtonListener(_recordsButton, HandleRecords);
            AddButtonListener(_quitButton, HandleQuit);
            AddButtonListener(_closeButton, HandleClose);
        }

        private void OnDisable()
        {
            RemoveButtonListener(_playButton, HandlePlay);
            RemoveButtonListener(_instructionsButton, HandleInstructions);
            RemoveButtonListener(_recordsButton, HandleRecords);
            RemoveButtonListener(_quitButton, HandleQuit);
            RemoveButtonListener(_closeButton, HandleClose);
        }

        private void HandlePlay()
        {
            SfxAudioService.PlayClick();
            LoadScene(SceneLoader.Gameplay);
        }

        private void HandleInstructions()
        {
            SfxAudioService.PlayClick();
            ToggleInfoView(InfoView.Instructions);
        }

        private void HandleRecords()
        {
            SfxAudioService.PlayClick();
            ToggleInfoView(InfoView.Records);
        }

        private void HandleClose()
        {
            SfxAudioService.PlayClick();
            CloseInfoPanel();
        }

        private void HandleQuit()
        {
            SfxAudioService.PlayClick();
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

        private void ToggleInfoView(InfoView requestedView)
        {
            if (_infoPanel == null)
                return;

            bool shouldOpen = !_infoPanel.activeSelf || _activeView != requestedView;
            if (!shouldOpen)
            {
                CloseInfoPanel();
                return;
            }

            _infoPanel.SetActive(true);
            _activeView = requestedView;
            SetActive(_instructionsTitle, requestedView == InfoView.Instructions);
            SetActive(_instructionsText, requestedView == InfoView.Instructions);
            SetActive(_recordsContent, requestedView == InfoView.Records);

            if (requestedView == InfoView.Records)
                RefreshRecordsList();
        }

        private void RefreshRecordsList()
        {
            HideSpawnedRows();

            if (_recordsListRoot == null || _recordRowTemplate == null)
                return;

            List<RecentRunRecord> records = RecentRunHistory.LoadRecentRuns();
            bool hasRecords = records.Count > 0;
            if (_recordsEmptyStateText != null)
            {
                _recordsEmptyStateText.text = "Chưa có kỷ lục nào.";
                _recordsEmptyStateText.gameObject.SetActive(!hasRecords);
            }

            if (!hasRecords)
                return;

            if (!_recordRowTemplate.IsConfigured)
            {
                ReportConfigurationError("RecordRowTemplate requires four Legacy UI Text references for TRẬN, ĐIỂM, THỜI GIAN, and CẤP.");
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                RecentRunRecordRow row;
                if (i < _spawnedRows.Count)
                {
                    row = _spawnedRows[i] != null ? _spawnedRows[i].GetComponent<RecentRunRecordRow>() : null;
                }
                else
                {
                    row = Instantiate(_recordRowTemplate, _recordsListRoot, false);
                    row.name = $"RecordRow_{i + 1:00}";
                    _spawnedRows.Add(row.gameObject);
                }

                if (row == null)
                    continue;

                row.SetRecord(i + 1, records[i]);
                row.gameObject.SetActive(true);
            }
        }

        private void HideSpawnedRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                    _spawnedRows[i].SetActive(false);
            }
        }

        private void LoadScene(string sceneName)
        {
            if (_transitionStarted)
                return;

            _transitionStarted = true;
            if (SceneLoader.TryLoad(sceneName, out string error))
                return;

            _transitionStarted = false;
            Debug.LogError($"MainMenu could not load '{sceneName}': {error}", this);
        }

        private void CloseInfoPanel()
        {
            _activeView = InfoView.None;
            SetActive(_instructionsTitle, false);
            SetActive(_instructionsText, false);
            SetActive(_recordsContent, false);
            SetActive(_infoPanel, false);
        }

        private void ReportMissingReferencesOnce()
        {
            if (_configurationErrorReported)
                return;

            if (_playButton != null && _instructionsButton != null && _recordsButton != null
                && _quitButton != null && _closeButton != null && _infoPanel != null
                && _instructionsTitle != null && _instructionsText != null && _recordsContent != null
                && _recordsListRoot != null && _recordRowTemplate != null && _recordsEmptyStateText != null)
                return;

            ReportConfigurationError("MainMenuController is missing one or more button, panel, instruction, records, or row references.");
        }

        private void ReportConfigurationError(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
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
    }
}
