using SpaceExplorer.Core;
using SpaceExplorer.Upgrades;
using UnityEditor;
using UnityEngine;

namespace SpaceExplorer.EditorTools
{
    internal static class UpgradeSelectionTestHarness
    {
        private static GameManager.PauseRequestToken _otherPauseToken;

        [MenuItem("Tools/Space Explorer/UPG-01/Start Selection Harness")]
        private static void StartSelectionHarness()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode and wire UpgradeManager before starting the UPG-01 selection harness.");
                return;
            }

            UpgradeManager manager = Object.FindFirstObjectByType<UpgradeManager>();
            if (manager == null)
            {
                Debug.LogError("No active UpgradeManager was found. Wire it into Gameplay before using the harness.");
                return;
            }

            IUpgradeOption[] mocks =
            {
                new MockUpgrade("test-one", "TEST — One-use A"),
                new MockUpgrade("test-two", "TEST — One-use B"),
                new MockUpgrade("test-three", "TEST — One-use C"),
                new MockUpgrade("test-one", "TEST — Duplicate (must be filtered)"),
                new MockUpgrade("test-unimplemented", "TEST — Unimplemented (must be filtered)", implemented: false)
            };

            if (!manager.BeginEditorTestScenario(mocks, 4, out string error))
                Debug.LogError($"Could not start the UPG-01 selection harness: {error}");
            else
                Debug.Log("UPG-01 harness queued four requests and registered three editor-only one-use mock upgrades.");
        }

        [MenuItem("Tools/Space Explorer/UPG-01/Finish Run During Selection")]
        private static void FinishRunDuringSelection()
        {
            if (!EditorApplication.isPlaying || GameManager.Instance == null)
            {
                Debug.LogWarning("Enter Gameplay Play Mode before finishing the run from the UPG-01 harness.");
                return;
            }

            GameManager manager = GameManager.Instance;
            var result = new RunResult(manager.CurrentScore, 0, 0f);
            if (!manager.TryFinishRun(result, out string error))
                Debug.LogError($"The harness could not finish the run: {error}");
        }

        [MenuItem("Tools/Space Explorer/UPG-01/Start New Run After Harness")]
        private static void StartNewRunAfterHarness()
        {
            GameManager manager = GameManager.Instance;
            if (!EditorApplication.isPlaying || manager == null || manager.State != RunState.Finished)
            {
                Debug.LogWarning("Finish the current harness run before starting a new one.");
                return;
            }

            if (!manager.TryResetRun(out string resetError))
            {
                Debug.LogError($"The harness could not reset the run: {resetError}");
                return;
            }

            if (!manager.TryBeginRun(out string beginError))
                Debug.LogError($"The harness could not begin a new run: {beginError}");
        }

        [MenuItem("Tools/Space Explorer/UPG-01/Stop Selection Harness")]
        private static void StopSelectionHarness()
        {
            UpgradeManager manager = Object.FindFirstObjectByType<UpgradeManager>();
            if (manager != null)
                manager.StopEditorTestScenario();

            ReleaseOtherPauseOwner();
        }

        [MenuItem("Tools/Space Explorer/UPG-01/Hold Separate Pause Owner")]
        private static void HoldSeparatePauseOwner()
        {
            if (!EditorApplication.isPlaying || GameManager.Instance == null || _otherPauseToken.IsValid)
                return;

            if (!GameManager.Instance.TryAcquirePauseRequest(out _otherPauseToken, out string error))
                Debug.LogError($"Could not acquire the test pause request: {error}");
        }

        [MenuItem("Tools/Space Explorer/UPG-01/Release Separate Pause Owner")]
        private static void ReleaseOtherPauseOwner()
        {
            if (!_otherPauseToken.IsValid)
                return;

            GameManager owner = GameManager.Instance;
            if (owner != null)
                owner.TryReleasePauseRequest(_otherPauseToken, out _);
            _otherPauseToken = default;
        }

        private sealed class MockUpgrade : IUpgradeOption
        {
            private bool _wasApplied;
            private readonly bool _implemented;

            public string Id { get; }
            public string DisplayName { get; }
            public bool HasImplementedEffect => _implemented;

            public MockUpgrade(string id, string displayName, bool implemented = true)
            {
                Id = id;
                DisplayName = displayName;
                _implemented = implemented;
            }

            public bool IsEligible(GameObject player) => !_wasApplied;

            public void Apply(GameObject player)
            {
                _wasApplied = true;
                Debug.Log($"UPG-01 mock effect '{Id}' applied once.", player);
            }
        }
    }
}
