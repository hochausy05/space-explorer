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

            // Lưu ba chỉ số đã chọn để lấy một bộ lựa chọn khác nếu cần.
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

            // Gắn cấp yêu cầu với nguồn production hoặc kịch bản kiểm thử Editor.
            public LevelUpRequest(int level, bool isEditorTest)
            {
                Level = level;
                IsEditorTest = isEditorTest;
            }
        }

        // Đăng ký sự kiện lên cấp và tiếp tục xử lý hàng đợi còn giữ lại.
        private void OnEnable()
        {
            if (_collector == null)
            {
                FailConfiguration("UpgradeManager requires the Gameplay StarXPCollector reference.");
                return;
            }

            _collector.LevelUp += HandleLevelUp;
            SubscribeRunStarted();

            // Giữ yêu cầu ở đầu hàng đợi đến khi panel nhận thành công để có thể thử lại.
            _waitingForPanel = false;
            ProcessPendingLevels();
        }

        // Hủy đăng ký lên cấp, vô hiệu callback cũ và trả pause token do manager sở hữu.
        private void OnDisable()
        {
            if (_collector != null)
                _collector.LevelUp -= HandleLevelUp;

            // Vô hiệu callback của vòng đời cũ nhưng giữ nguyên các yêu cầu đang chờ.
            _runVersion++;
            _isApplyingSelection = false;
            _waitingForPanel = false;
            InvalidateSelection(releasePause: true);
            UnbindGameManager();
        }

        // Hủy mọi đăng ký sự kiện còn lại trước khi UpgradeManager bị hủy.
        private void OnDestroy()
        {
            if (_runStartedSubscribed)
            {
                GameManager.RunStarted -= HandleRunStarted;
                _runStartedSubscribed = false;
            }

            UnbindGameManager();
        }

        // Theo dõi GameManager và thử lại yêu cầu khi panel được cấu hình lại.
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

        // Xếp yêu cầu lên cấp vào hàng đợi nếu run vẫn còn hoạt động.
        private void HandleLevelUp(int level)
        {
            TryBindGameManager();
            if (_gameManager != null && (_gameManager.State == RunState.Finished || _gameManager.State == RunState.Ready))
                return;

            _pendingLevels.Enqueue(new LevelUpRequest(level, isEditorTest: false));
            ProcessPendingLevels();
        }

        // Xử lý lần lượt yêu cầu lên cấp; chỉ pause khi có lựa chọn hợp lệ và panel dùng được.
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
                        // Không tạo lựa chọn giả hoặc panel rỗng; chỉ bỏ yêu cầu này rồi xử lý yêu cầu kế.
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

                    // Nếu Show thất bại, trả pause ngay và giữ yêu cầu đầu hàng đợi để thử lại.
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

        // Tạo lựa chọn hợp lệ từ danh sách đăng ký hoặc dữ liệu giả lập trong Editor.
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

        // Lọc hiệu ứng chưa cài, ID trùng và điều kiện nhận trước khi thêm nâng cấp.
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

        // Chọn ngẫu nhiên tối đa ba nâng cấp hợp lệ và tránh lặp nguyên bộ ở lượt sau.
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
                // Chọn đều giữa các bộ thay thế; danh sách hữu hạn nên không thể lặp thử vô hạn.
                SelectAlternativeChoiceSet(previousChoiceIds);
                return;
            }

            AddShuffledChoices(0, 1, 2);
        }

        // Xáo trộn danh sách bằng Fisher–Yates để các lựa chọn có cơ hội đồng đều.
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

        // Kiểm tra ba mục đầu sau xáo trộn có trùng bộ lựa chọn trước đó hay không.
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

        // Chọn ngẫu nhiên một bộ ba khác mà không dùng vòng thử lại vô hạn.
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

            // Khi có hơn ba nâng cấp hợp lệ, luôn tồn tại ít nhất một bộ thay thế.
            if (_alternativeChoiceSets.Count == 0)
            {
                AddShuffledChoices(0, 1, 2);
                return;
            }

            ChoiceIndices selected = _alternativeChoiceSets[UnityEngine.Random.Range(0, _alternativeChoiceSets.Count)];
            AddShuffledChoices(selected.First, selected.Second, selected.Third);
        }

        // So sánh ba ID theo tập hợp, không phụ thuộc thứ tự nút hiển thị.
        private bool IsPreviousChoiceSet(HashSet<string> previousChoiceIds, int first, int second, int third)
        {
            return previousChoiceIds.Contains(_eligibleChoices[first].Id)
                && previousChoiceIds.Contains(_eligibleChoices[second].Id)
                && previousChoiceIds.Contains(_eligibleChoices[third].Id);
        }

        // Thêm ba nâng cấp theo thứ tự đã xáo trộn vào danh sách hiển thị.
        private void AddShuffledChoices(int first, int second, int third)
        {
            _choices.Add(_eligibleChoices[first]);
            _choices.Add(_eligibleChoices[second]);
            _choices.Add(_eligibleChoices[third]);
        }

        // Lưu ID vừa hiển thị để tránh lặp nguyên bộ ở lần chọn kế tiếp.
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

        // Xóa lịch sử bộ lựa chọn khi run bắt đầu hoặc kết thúc.
        private void ClearPreviousChoiceSets()
        {
            _previousProductionChoiceIds.Clear();
            _previousEditorChoiceIds.Clear();
            _hasPreviousProductionChoiceSet = false;
            _hasPreviousEditorChoiceSet = false;
        }

        // Token chặn callback cũ; lấy yêu cầu khỏi hàng đợi trước khi áp dụng hiệu ứng một lần.
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
            _pendingLevels.Dequeue(); // Bỏ yêu cầu trước khi gọi effect vì effect có thể tắt component.
            ClearEditorMocksWhenNoTestRequests();
            _selectionPanel.Hide();

            try
            {
                bool effectImplemented = selectedUpgrade.HasImplementedEffect;
                bool stillCurrent = IsSelectionApplicationCurrent(runVersion);
                bool eligible = effectImplemented && stillCurrent && selectedUpgrade.IsEligible(_collector.gameObject);

                // Kiểm tra điều kiện nhận có thể đổi trạng thái ngay, nên cần xác nhận run vẫn hợp lệ.
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

        // Xác nhận callback còn thuộc run hiện tại và gameplay vẫn pause để chọn.
        private bool IsSelectionApplicationCurrent(int runVersion)
        {
            return runVersion == _runVersion && isActiveAndEnabled && _gameManager != null
                && _gameManager.State == RunState.Paused;
        }

        // Dọn hàng đợi và lựa chọn khi GameManager kết thúc run.
        private void HandleRunFinished(RunResult result)
        {
            ResetForFinishedRun();
        }

        // Vô hiệu callback và xóa lựa chọn sau khi run hoàn tất.
        private void ResetForFinishedRun()
        {
            _runVersion++;
            ClearPreviousChoiceSets();
            ClearPendingLevels();
            _isApplyingSelection = false;
            _hasPauseRequest = false; // GameManager thu hồi mọi pause request trước khi phát RunFinished.
            _pauseToken = default;
            InvalidateSelection(releasePause: false);
#if UNITY_EDITOR
            _editorTestUpgrades.Clear();
#endif
        }

        // Reset hàng đợi và lịch sử lựa chọn cho run mới.
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

        // Đóng lựa chọn hiện tại và tăng token để callback cũ mất hiệu lực.
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

        // Lên lịch thử lại sau một khoảng nghỉ để tránh lặp liên tục.
        private void WaitForPanelRepair()
        {
            _waitingForPanel = true;
            _nextPanelRetryTime = Time.unscaledTime + PanelRetryInterval;
        }

        // Xóa các yêu cầu cấp đang chờ và trạng thái đợi panel.
        private void ClearPendingLevels()
        {
            _pendingLevels.Clear();
            _waitingForPanel = false;
        }

        // Xóa mock Editor khi hàng đợi không còn yêu cầu kiểm thử.
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

        // Chỉ trả pause token mà UpgradeManager đã tự nhận.
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

        // Báo lỗi cấu hình rồi tắt manager để tránh xử lý lựa chọn không an toàn.
        private void FailConfiguration(string message)
        {
            ReportConfigurationError(message);
            enabled = false;
        }

        // Ghi lỗi cấu hình tối đa một lần để tránh log lặp.
        private void ReportConfigurationError(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }

        // Đăng ký một lần sự kiện bắt đầu run để xóa trạng thái cũ.
        private void SubscribeRunStarted()
        {
            if (_runStartedSubscribed)
                return;

            // Giữ đăng ký vòng đời qua lúc disable để run mới vẫn xóa các yêu cầu cũ.
            GameManager.RunStarted += HandleRunStarted;
            _runStartedSubscribed = true;
        }

        // Liên kết GameManager đang hoạt động và dọn trạng thái của manager cũ.
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

        // Hủy đăng ký RunFinished khỏi GameManager đang được theo dõi.
        private void UnbindGameManager()
        {
            GameManager previous = _gameManager;
            _gameManager = null;
            _gameManagerBindingVersion++;
            if (previous != null)
                previous.RunFinished -= HandleRunFinished;
        }

#if UNITY_EDITOR
        // Tạo yêu cầu kiểm thử Editor khi không có yêu cầu gameplay đang chờ.
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

        // Xóa yêu cầu giả lập còn lại rồi tiếp tục xử lý hàng đợi gameplay.
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

        // Lọc yêu cầu Editor khỏi hàng đợi nhưng giữ nguyên yêu cầu gameplay.
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
