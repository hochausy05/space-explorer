using System;
using System.Collections.Generic;
using SpaceExplorer.Core;
using SpaceExplorer.Pickups;
using SpaceExplorer.UI;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public sealed class UpgradeManager : MonoBehaviour
    {
        public const int MaximumChoices = 3;

        private const float PanelRetryInterval = 0.5f;

        [Header("Run References")]
        [SerializeField] private StarXPCollector _collector;
        [SerializeField] private UpgradeSelectionPanel _selectionPanel;

        [Header("Production Upgrade Registry")]
        [SerializeField] private UpgradeDefinition[] _registeredUpgrades;

        private readonly Queue<LevelUpRequest> _pendingLevels = new Queue<LevelUpRequest>();
        private readonly List<IUpgradeOption> _choices = new List<IUpgradeOption>(MaximumChoices);
        private readonly List<IUpgradeOption> _eligibleChoices = new List<IUpgradeOption>();
        private readonly List<ChoiceIndices> _alternativeChoiceSets = new List<ChoiceIndices>();
        private readonly HashSet<string> _seenIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _previousProductionChoiceIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _previousEditorChoiceIds = new HashSet<string>(StringComparer.Ordinal);

#if UNITY_EDITOR
        private readonly List<IUpgradeOption> _editorTestUpgrades = new List<IUpgradeOption>();
#endif

        private GameManager.PauseRequestToken _pauseToken;
        private int _selectionToken;
        private int _runVersion;
        private int _gameManagerBindingVersion;
        private float _nextPanelRetryTime;
        private bool _hasPauseRequest;
        private bool _selectionActive;
        private bool _isApplyingSelection;
        private bool _isProcessingQueue;
        private bool _waitingForPanel;
        private bool _configurationErrorReported;
        private bool _eligibilityErrorReported;
        private bool _runStartedSubscribed;
        private bool _hasPreviousProductionChoiceSet;
        private bool _hasPreviousEditorChoiceSet;
        private GameManager _gameManager;

        private readonly struct ChoiceIndices
        {
            public readonly int First;
            public readonly int Second;
            public readonly int Third;

            public ChoiceIndices(int first, int second, int third)
            {
                First = first;
                Second = second;
                Third = third;
            }
        }

        private readonly struct LevelUpRequest
        {
            public readonly int Level;
            public readonly bool IsEditorTest;

            public LevelUpRequest(int level, bool isEditorTest)
            {
                Level = level;
                IsEditorTest = isEditorTest;
            }
        }

        private void OnEnable()
        {
            if (_collector == null)
            {
                FailConfiguration("UpgradeManager requires the Gameplay StarXPCollector reference.");
                return;
            }

            _collector.LevelUp += HandleLevelUp;
            SubscribeRunStarted();

            // A selection is kept at the queue head until accepted, so re-enabling can safely retry it.
            _waitingForPanel = false;
            ProcessPendingLevels();
        }

        private void OnDisable()
        {
            if (_collector != null)
                _collector.LevelUp -= HandleLevelUp;

            // Invalidate any callback captured by the old enabled lifetime, but retain queued requests.
            _runVersion++;
            _isApplyingSelection = false;
            _waitingForPanel = false;
            InvalidateSelection(releasePause: true);
            UnbindGameManager();
        }

        private void OnDestroy()
        {
            if (_runStartedSubscribed)
            {
                GameManager.RunStarted -= HandleRunStarted;
                _runStartedSubscribed = false;
            }

            UnbindGameManager();
        }

        private void Update()
        {
            int previousBindingVersion = _gameManagerBindingVersion;
            if (!TryBindGameManager())
                return;

            if (previousBindingVersion != _gameManagerBindingVersion)
                ProcessPendingLevels();

            if (_selectionActive && (_selectionPanel == null || !_selectionPanel.isActiveAndEnabled))
            {
                _selectionActive = false;
                _selectionToken++;
                _choices.Clear();
                ReleaseOwnedPauseRequest();
                WaitForPanelRepair();
            }

            if (_waitingForPanel && Time.unscaledTime >= _nextPanelRetryTime)
            {
                _nextPanelRetryTime = Time.unscaledTime + PanelRetryInterval;
                if (_selectionPanel != null && _selectionPanel.IsConfigured)
                {
                    _waitingForPanel = false;
                    ProcessPendingLevels();
                }
            }
        }

        private void HandleLevelUp(int level)
        {
            TryBindGameManager();
            if (_gameManager != null && (_gameManager.State == RunState.Finished || _gameManager.State == RunState.Ready))
                return;

            _pendingLevels.Enqueue(new LevelUpRequest(level, isEditorTest: false));
            ProcessPendingLevels();
        }

        private void ProcessPendingLevels()
        {
            if (_gameManager == null && !TryBindGameManager())
                return;

            if (_isProcessingQueue || _selectionActive || _isApplyingSelection || _waitingForPanel || _gameManager == null)
                return;

            int processingRunVersion = _runVersion;
            _isProcessingQueue = true;
            try
            {
                while (_pendingLevels.Count > 0)
                {
                    if (_gameManager.State == RunState.Finished)
                    {
                        ClearPendingLevels();
                        break;
                    }

                    if (_gameManager.State != RunState.Playing && _gameManager.State != RunState.Paused)
                        break;

                    LevelUpRequest request = _pendingLevels.Peek();
                    BuildEligibleChoices(request);
                    if (!isActiveAndEnabled || processingRunVersion != _runVersion)
                        break;

                    if (_choices.Count == 0)
                    {
                        // No fake option or empty panel: consume only this request, then continue in order.
                        _pendingLevels.Dequeue();
                        ClearEditorMocksWhenNoTestRequests();
                        continue;
                    }

                    if (_selectionPanel == null || !_selectionPanel.IsConfigured)
                    {
                        ReportConfigurationError("UpgradeManager has eligible upgrades but no configured UpgradeSelectionPanel; the request remains queued.");
                        WaitForPanelRepair();
                        break;
                    }

                    if (!_gameManager.TryAcquirePauseRequest(out GameManager.PauseRequestToken token, out string error))
                    {
                        if (_gameManager.State == RunState.Finished)
                            ClearPendingLevels();
                        else
                            ReportConfigurationError($"UpgradeManager could not acquire its pause request: {error}");
                        break;
                    }

                    _pauseToken = token;
                    _hasPauseRequest = true;
                    _selectionToken++;
                    _selectionActive = true;
                    int selectionRunVersion = _runVersion;

                    if (_selectionPanel.Show(_choices, _selectionToken, HandleChoiceSelected))
                    {
                        RecordPreviousChoiceSet(request.IsEditorTest);
                        break;
                    }

                    // Show can fail after configuration was inspected. Release immediately and retain the queue head.
                    ReportConfigurationError("UpgradeSelectionPanel rejected a non-empty choice list; the request remains queued.");
                    _selectionActive = false;
                    _selectionToken++;
                    _choices.Clear();
                    ReleaseOwnedPauseRequest();
                    if (selectionRunVersion != _runVersion || _gameManager.State == RunState.Finished)
                    {
                        if (_gameManager.State == RunState.Finished)
                            ClearPendingLevels();
                        break;
                    }

                    WaitForPanelRepair();
                    break;
                }

                ClearEditorMocksWhenNoTestRequests();
            }
            finally
            {
                _isProcessingQueue = false;
                if (isActiveAndEnabled && processingRunVersion != _runVersion)
                    ProcessPendingLevels();
            }
        }

        private void BuildEligibleChoices(LevelUpRequest request)
        {
            _choices.Clear();
            _eligibleChoices.Clear();
            _seenIds.Clear();

#if UNITY_EDITOR
            if (request.IsEditorTest)
            {
                for (int i = 0; i < _editorTestUpgrades.Count; i++)
                    TryAddEligible(_editorTestUpgrades[i]);
                SelectEligibleChoices(isEditorTest: true);
                return;
            }
#else
            if (request.IsEditorTest)
                return;
#endif

            if (_registeredUpgrades == null)
                return;

            for (int i = 0; i < _registeredUpgrades.Length; i++)
                TryAddEligible(_registeredUpgrades[i]);

            SelectEligibleChoices(isEditorTest: false);
        }

        private void TryAddEligible(IUpgradeOption upgrade)
        {
            if (upgrade == null)
                return;

            try
            {
                if (!upgrade.HasImplementedEffect
                    || string.IsNullOrWhiteSpace(upgrade.Id) || string.IsNullOrWhiteSpace(upgrade.DisplayName)
                    || !_seenIds.Add(upgrade.Id))
                    return;

                if (upgrade.IsEligible(_collector.gameObject))
                    _eligibleChoices.Add(upgrade);
            }
            catch (Exception exception)
            {
                if (!_eligibilityErrorReported)
                {
                    Debug.LogException(exception, this);
                    _eligibilityErrorReported = true;
                }
            }
        }

        private void SelectEligibleChoices(bool isEditorTest)
        {
            if (_eligibleChoices.Count == 0)
                return;

            if (_eligibleChoices.Count <= MaximumChoices)
            {
                _choices.AddRange(_eligibleChoices);
                return;
            }

            ShuffleEligibleChoices();
            HashSet<string> previousChoiceIds = isEditorTest
                ? _previousEditorChoiceIds
                : _previousProductionChoiceIds;
            bool hasPreviousChoiceSet = isEditorTest
                ? _hasPreviousEditorChoiceSet
                : _hasPreviousProductionChoiceSet;

            if (hasPreviousChoiceSet && MatchesPreviousChoiceSet(previousChoiceIds))
            {
                // Choose uniformly from every alternative set; this is finite and cannot retry forever.
                SelectAlternativeChoiceSet(previousChoiceIds);
                return;
            }

            AddShuffledChoices(0, 1, 2);
        }

        private void ShuffleEligibleChoices()
        {
            for (int i = _eligibleChoices.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                IUpgradeOption item = _eligibleChoices[i];
                _eligibleChoices[i] = _eligibleChoices[swapIndex];
                _eligibleChoices[swapIndex] = item;
            }
        }

        private bool MatchesPreviousChoiceSet(HashSet<string> previousChoiceIds)
        {
            if (previousChoiceIds.Count != MaximumChoices)
                return false;

            for (int i = 0; i < MaximumChoices; i++)
            {
                if (!previousChoiceIds.Contains(_eligibleChoices[i].Id))
                    return false;
            }

            return true;
        }

        private void SelectAlternativeChoiceSet(HashSet<string> previousChoiceIds)
        {
            _alternativeChoiceSets.Clear();
            for (int first = 0; first < _eligibleChoices.Count - 2; first++)
            {
                for (int second = first + 1; second < _eligibleChoices.Count - 1; second++)
                {
                    for (int third = second + 1; third < _eligibleChoices.Count; third++)
                    {
                        if (IsPreviousChoiceSet(previousChoiceIds, first, second, third))
                            continue;

                        _alternativeChoiceSets.Add(new ChoiceIndices(first, second, third));
                    }
                }
            }

            // More than three eligible options guarantee at least one alternative set.
            if (_alternativeChoiceSets.Count == 0)
            {
                AddShuffledChoices(0, 1, 2);
                return;
            }

            ChoiceIndices selected = _alternativeChoiceSets[UnityEngine.Random.Range(0, _alternativeChoiceSets.Count)];
            AddShuffledChoices(selected.First, selected.Second, selected.Third);
        }

        private bool IsPreviousChoiceSet(HashSet<string> previousChoiceIds, int first, int second, int third)
        {
            return previousChoiceIds.Contains(_eligibleChoices[first].Id)
                && previousChoiceIds.Contains(_eligibleChoices[second].Id)
                && previousChoiceIds.Contains(_eligibleChoices[third].Id);
        }

        private void AddShuffledChoices(int first, int second, int third)
        {
            _choices.Add(_eligibleChoices[first]);
            _choices.Add(_eligibleChoices[second]);
            _choices.Add(_eligibleChoices[third]);
        }

        private void RecordPreviousChoiceSet(bool isEditorTest)
        {
            HashSet<string> previousChoiceIds = isEditorTest
                ? _previousEditorChoiceIds
                : _previousProductionChoiceIds;
            previousChoiceIds.Clear();
            for (int i = 0; i < _choices.Count; i++)
                previousChoiceIds.Add(_choices[i].Id);

            if (isEditorTest)
                _hasPreviousEditorChoiceSet = true;
            else
                _hasPreviousProductionChoiceSet = true;
        }

        private void ClearPreviousChoiceSets()
        {
            _previousProductionChoiceIds.Clear();
            _previousEditorChoiceIds.Clear();
            _hasPreviousProductionChoiceSet = false;
            _hasPreviousEditorChoiceSet = false;
        }

        private void HandleChoiceSelected(int index, int selectionToken)
        {
            if (!_selectionActive || _isApplyingSelection || selectionToken != _selectionToken
                || index < 0 || index >= _choices.Count || !_hasPauseRequest
                || _gameManager == null || _gameManager.State != RunState.Paused || _pendingLevels.Count == 0)
                return;

            IUpgradeOption selectedUpgrade = _choices[index];
            int runVersion = _runVersion;
            _selectionActive = false;
            _selectionToken++;
            _isApplyingSelection = true;
            _pendingLevels.Dequeue(); // Consume before calling user effect code, which may disable this component.
            ClearEditorMocksWhenNoTestRequests();
            _selectionPanel.Hide();

            try
            {
                bool effectImplemented = selectedUpgrade.HasImplementedEffect;
                bool stillCurrent = IsSelectionApplicationCurrent(runVersion);
                bool eligible = effectImplemented && stillCurrent && selectedUpgrade.IsEligible(_collector.gameObject);

                // Eligibility is option code too; it may synchronously disable this manager or finish the run.
                stillCurrent = IsSelectionApplicationCurrent(runVersion);
                if (stillCurrent && eligible)
                    selectedUpgrade.Apply(_collector.gameObject);
                else if (stillCurrent)
                    ReportConfigurationError($"Upgrade '{selectedUpgrade.Id}' became ineligible before it could be applied.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (runVersion != _runVersion || _gameManager == null || _gameManager.State == RunState.Finished)
                return;

            _isApplyingSelection = false;
            _choices.Clear();
            ReleaseOwnedPauseRequest();
            ProcessPendingLevels();
        }

        private bool IsSelectionApplicationCurrent(int runVersion)
        {
            return runVersion == _runVersion && isActiveAndEnabled && _gameManager != null
                && _gameManager.State == RunState.Paused;
        }

        private void HandleRunFinished(RunResult result)
        {
            ResetForFinishedRun();
        }

        private void ResetForFinishedRun()
        {
            _runVersion++;
            ClearPreviousChoiceSets();
            ClearPendingLevels();
            _isApplyingSelection = false;
            _hasPauseRequest = false; // GameManager invalidates all requests before publishing RunFinished.
            _pauseToken = default;
            InvalidateSelection(releasePause: false);
#if UNITY_EDITOR
            _editorTestUpgrades.Clear();
#endif
        }

        private void HandleRunStarted()
        {
            _runVersion++;
            ClearPreviousChoiceSets();
            ClearPendingLevels();
            _isApplyingSelection = false;
            _hasPauseRequest = false;
            _pauseToken = default;
            InvalidateSelection(releasePause: false);
#if UNITY_EDITOR
            _editorTestUpgrades.Clear();
#endif
        }

        private void InvalidateSelection(bool releasePause)
        {
            _selectionActive = false;
            _selectionToken++;
            _choices.Clear();
            if (_selectionPanel != null)
                _selectionPanel.Hide();

            if (releasePause)
                ReleaseOwnedPauseRequest();
            else
            {
                _hasPauseRequest = false;
                _pauseToken = default;
            }
        }

        private void WaitForPanelRepair()
        {
            _waitingForPanel = true;
            _nextPanelRetryTime = Time.unscaledTime + PanelRetryInterval;
        }

        private void ClearPendingLevels()
        {
            _pendingLevels.Clear();
            _waitingForPanel = false;
        }

        private void ClearEditorMocksWhenNoTestRequests()
        {
#if UNITY_EDITOR
            foreach (LevelUpRequest request in _pendingLevels)
            {
                if (request.IsEditorTest)
                    return;
            }

            _editorTestUpgrades.Clear();
#endif
        }

        private void ReleaseOwnedPauseRequest()
        {
            if (!_hasPauseRequest)
                return;

            _hasPauseRequest = false;
            GameManager.PauseRequestToken token = _pauseToken;
            _pauseToken = default;
            if (_gameManager != null && token.IsValid)
                _gameManager.TryReleasePauseRequest(token, out _);
        }

        private void FailConfiguration(string message)
        {
            ReportConfigurationError(message);
            enabled = false;
        }

        private void ReportConfigurationError(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }

        private void SubscribeRunStarted()
        {
            if (_runStartedSubscribed)
                return;

            // Keep this lifecycle-only subscription through disable so a new run still clears retained requests.
            GameManager.RunStarted += HandleRunStarted;
            _runStartedSubscribed = true;
        }

        private bool TryBindGameManager()
        {
            GameManager authoritative = GameManager.Instance;
            if (ReferenceEquals(authoritative, _gameManager))
                return authoritative != null;

            GameManager previous = _gameManager;
            if (!ReferenceEquals(previous, null))
            {
                _runVersion++;
                _isApplyingSelection = false;
                _waitingForPanel = false;
                InvalidateSelection(releasePause: true);
                if (previous != null)
                    previous.RunFinished -= HandleRunFinished;
                ClearPendingLevels();
            }

            _gameManager = authoritative;
            _gameManagerBindingVersion++;
            if (_gameManager == null)
                return false;

            _gameManager.RunFinished += HandleRunFinished;
            if (_gameManager.State == RunState.Finished)
                ResetForFinishedRun();

            return true;
        }

        private void UnbindGameManager()
        {
            GameManager previous = _gameManager;
            _gameManager = null;
            _gameManagerBindingVersion++;
            if (previous != null)
                previous.RunFinished -= HandleRunFinished;
        }

#if UNITY_EDITOR
        public bool BeginEditorTestScenario(IReadOnlyList<IUpgradeOption> mockUpgrades, int queuedLevelUps, out string error)
        {
            if (!Application.isEditor || !Application.isPlaying || mockUpgrades == null || mockUpgrades.Count == 0
                || queuedLevelUps < 1 || _collector == null || !isActiveAndEnabled
                || _pendingLevels.Count != 0 || _isProcessingQueue
                || _selectionActive || _isApplyingSelection || _gameManager == null
                || (_gameManager.State != RunState.Playing && _gameManager.State != RunState.Paused))
            {
                error = "The editor test scenario requires Play Mode, an active run, no pending/processing production requests, no active selection, and mock upgrades.";
                return false;
            }

            for (int i = 0; i < mockUpgrades.Count; i++)
            {
                if (mockUpgrades[i] == null)
                {
                    error = "The editor test scenario contains a null mock upgrade.";
                    return false;
                }
            }

            _editorTestUpgrades.Clear();
            _previousEditorChoiceIds.Clear();
            _hasPreviousEditorChoiceSet = false;
            for (int i = 0; i < mockUpgrades.Count; i++)
                _editorTestUpgrades.Add(mockUpgrades[i]);

            for (int i = 0; i < queuedLevelUps; i++)
                _pendingLevels.Enqueue(new LevelUpRequest(_collector.CurrentLevel + i + 1, isEditorTest: true));

            error = null;
            ProcessPendingLevels();
            return true;
        }

        public void StopEditorTestScenario()
        {
            if (_selectionActive && _pendingLevels.Count > 0 && _pendingLevels.Peek().IsEditorTest)
                InvalidateSelection(releasePause: true);

            RemovePendingEditorTestRequests();
            _editorTestUpgrades.Clear();
            _previousEditorChoiceIds.Clear();
            _hasPreviousEditorChoiceSet = false;
            _waitingForPanel = false;
            ProcessPendingLevels();
        }

        private void RemovePendingEditorTestRequests()
        {
            int requestCount = _pendingLevels.Count;
            for (int i = 0; i < requestCount; i++)
            {
                LevelUpRequest request = _pendingLevels.Dequeue();
                if (!request.IsEditorTest)
                    _pendingLevels.Enqueue(request);
            }
        }
#endif
    }
}
