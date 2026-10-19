using SpaceExplorer.Asteroids;
using SpaceExplorer.Combat;
using SpaceExplorer.Core;
using SpaceExplorer.Pickups;
using SpaceExplorer.Upgrades;
using UnityEngine;

namespace SpaceExplorer.Player
{
    [RequireComponent(typeof(Collider2D), typeof(PlayerHealth))]
    public sealed class PlayerAsteroidCollisionHandler : MonoBehaviour
    {
        [SerializeField] private StarXPCollector _progression;
        [SerializeField, Min(0)] private int _smallAsteroidDamage = 1;
        [SerializeField, Min(0)] private int _largeAsteroidDamage = 3;
        [SerializeField, Min(0)] private int _smallAsteroidScorePenalty = 5;
        [SerializeField, Min(0)] private int _largeAsteroidScorePenalty = 15;

        private Collider2D _playerCollider;
        private PlayerHealth _playerHealth;
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
            _playerHealth = GetComponent<PlayerHealth>();
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

            if (_playerHealth == null)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's PlayerHealth component.");
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
            if (_gameOverStarted || !other.TryGetComponent(out Asteroid asteroid))
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

            int scorePenalty = asteroid.Size == AsteroidSize.Small
                ? Mathf.Max(0, _smallAsteroidScorePenalty)
                : Mathf.Max(0, _largeAsteroidScorePenalty);

            if (!manager.TryApplyScorePenalty(scorePenalty, out string penaltyError))
            {
                Debug.LogError($"Player-Asteroid collision could not apply the Score penalty: {penaltyError}", this);
                return;
            }

            if (_energyShield != null && _energyShield.TryAbsorbAsteroidCollision())
                return;

            int damage = asteroid.Size == AsteroidSize.Small
                ? Mathf.Max(0, _smallAsteroidDamage)
                : Mathf.Max(0, _largeAsteroidDamage);

            if (_playerHealth.ApplyDamage(damage))
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
