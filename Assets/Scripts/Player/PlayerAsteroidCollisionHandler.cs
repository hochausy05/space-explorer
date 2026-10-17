using SpaceExplorer.Asteroids;
using SpaceExplorer.Combat;
using SpaceExplorer.Core;
using SpaceExplorer.Pickups;
using SpaceExplorer.Upgrades;
using UnityEngine;

namespace SpaceExplorer.Player
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerAsteroidCollisionHandler : MonoBehaviour
    {
        [SerializeField] private StarXPCollector _progression;

        private Collider2D _playerCollider;
        private PlayerController _playerController;
        private WeaponController _weaponController;
        private EnergyShieldController _energyShield;
        private double _runStartedAt;
        private double _lastCollisionFixedTime = double.NegativeInfinity;
        private EntityId _lastCollisionColliderId = EntityId.None;
        private bool _hasRunStartTime;
        private bool _gameOverStarted;
        private bool _configurationErrorReported;

        private void Awake()
        {
            _playerCollider = GetComponent<Collider2D>();
            _playerController = GetComponent<PlayerController>();
            _weaponController = GetComponent<WeaponController>();
            _energyShield = GetComponent<EnergyShieldController>();

            if (_progression == null)
                _progression = GetComponent<StarXPCollector>();

            if (_playerCollider == null || !_playerCollider.isTrigger)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's existing trigger Collider2D.");
                return;
            }

            if (_progression == null)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's StarXPCollector to capture RunResult.");
                return;
            }

            GameManager.RunStarted += HandleRunStarted;
            GameManager manager = GameManager.Instance;
            if (manager != null && (manager.State == RunState.Playing || manager.State == RunState.Paused))
                HandleRunStarted();
        }

        private void OnDestroy()
        {
            GameManager.RunStarted -= HandleRunStarted;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_gameOverStarted || !other.TryGetComponent(out Asteroid _))
                return;

            GameManager manager = GameManager.Instance;
            if (manager == null || (manager.State != RunState.Playing && manager.State != RunState.Paused))
                return;

            EntityId colliderId = other.GetEntityId();
            double fixedTime = Time.fixedTimeAsDouble;
            if (_lastCollisionColliderId.Equals(colliderId) && _lastCollisionFixedTime == fixedTime)
                return;

            _lastCollisionColliderId = colliderId;
            _lastCollisionFixedTime = fixedTime;

            if (_energyShield != null && _energyShield.TryAbsorbAsteroidCollision())
                return;

            FinishRun(manager);
        }

        private void FinishRun(GameManager manager)
        {
            if (!_hasRunStartTime)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler did not receive the new-run event needed to capture survival time.");
                return;
            }

            _gameOverStarted = true;
            if (_playerController != null)
                _playerController.enabled = false;
            if (_weaponController != null)
                _weaponController.enabled = false;

            float survivalTime = Mathf.Max(0f, (float)(Time.timeAsDouble - _runStartedAt));
            var result = new RunResult(manager.CurrentScore, _progression.CurrentLevel, survivalTime);

            if (!manager.TryFinishRun(result, out string finishError))
            {
                Debug.LogError($"Player-Asteroid collision could not finish the run: {finishError}", this);
                return;
            }

            if (!SceneLoader.TryLoad(SceneLoader.EndGame, out string sceneError))
                Debug.LogError(sceneError, this);
        }

        private void HandleRunStarted()
        {
            _gameOverStarted = false;
            _runStartedAt = Time.timeAsDouble;
            _lastCollisionColliderId = EntityId.None;
            _lastCollisionFixedTime = double.NegativeInfinity;
            _hasRunStartTime = true;
        }

        private void FailConfiguration(string message)
        {
            if (!_configurationErrorReported)
            {
                Debug.LogError(message, this);
                _configurationErrorReported = true;
            }

            enabled = false;
        }
    }
}
