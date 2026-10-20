using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Asteroids
{
    public sealed class AsteroidSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AsteroidPool _pool;
        [SerializeField] private Camera _camera;

        [Header("Spawn Settings")]
        [SerializeField, Min(0.75f)] private float _spawnInterval = 1.8f;
        [SerializeField, Min(0.01f)] private float _initialAsteroidSpeedMultiplier = 0.8f;
        [SerializeField, Range(0.01f, 1f)] private float _initialSpawnFrequencyMultiplier = 0.75f;
        [SerializeField, Min(0.01f)] private float _difficultyStepSeconds = 30f;
        [SerializeField, Min(0f)] private float _speedMultiplierIncreasePerStep = 0.05f;
        [SerializeField, Min(0f)] private float _frequencyMultiplierIncreasePerStep = 0.08f;
        [SerializeField, Min(1f)] private float _maximumSpeedMultiplier = 1.6f;
        [SerializeField, Min(1f)] private float _maximumFrequencyMultiplier = 2f;
        [SerializeField, Range(0f, 1f)] private float _largeAsteroidProbability = 0.2f;
        [SerializeField, Min(0f)] private float _verticalInset = 0.8f;
        [SerializeField, Min(0f)] private float _rightEdgeOffset = 0.5f;

        [Header("Speed Variant Test")]
        [SerializeField, Min(1f)] private float _speedVariantMinimumMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float _speedVariantMaximumMultiplier = 2f;

        private float _timeUntilNextSpawn;
        private double _activeGameplayElapsed;
        private bool _runTimerInitialized;
        private bool _runStartedSubscribed;
        private bool _configurationErrorReported;
        private bool _speedPoolWarningReported;

        private void Awake()
        {
            if (_pool == null)
            {
                FailConfiguration("AsteroidSpawner needs an AsteroidPool reference.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;

            if (!IsValidCamera(_camera))
            {
                FailConfiguration("AsteroidSpawner needs an axis-aligned orthographic gameplay camera.");
                return;
            }

            if (_spawnInterval <= 0f)
            {
                FailConfiguration("AsteroidSpawner interval must be greater than zero.");
                return;
            }
        }

        private void Start()
        {
            // Start runs after every scene object's Awake, including AsteroidPool.Awake.
            if (_pool != null && !_pool.IsReady)
            {
                FailConfiguration("AsteroidSpawner cannot run because AsteroidPool is not ready.");
                return;
            }

            if (GameManager.Instance == null)
            {
                FailConfiguration("AsteroidSpawner requires a GameManager in the Gameplay scene.");
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager != null && manager.State == RunState.Playing && !_runTimerInitialized)
                ResetDifficultyForNewRun();
        }

        private void OnEnable()
        {
            if (!_runStartedSubscribed)
            {
                GameManager.RunStarted += ResetDifficultyForNewRun;
                _runStartedSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (_runStartedSubscribed)
            {
                GameManager.RunStarted -= ResetDifficultyForNewRun;
                _runStartedSubscribed = false;
            }
        }

        private void Update()
        {
            if (_pool == null || !IsValidCamera(_camera))
            {
                FailConfiguration("AsteroidSpawner lost its pool or camera reference.");
                return;
            }

            if (!IsRunActive())
                return;

            _activeGameplayElapsed += Time.deltaTime;
            _timeUntilNextSpawn -= Time.deltaTime;
            if (_timeUntilNextSpawn > 0f)
                return;

            // Skip catch-up bursts after a hitch; pool capacity remains the active-object limit.
            _timeUntilNextSpawn = GetCurrentSpawnInterval();
            SpawnOne();
        }

        private bool IsRunActive()
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.State == RunState.Playing;
        }

        private void SpawnOne()
        {
            AsteroidSize size = Random.value < Mathf.Clamp01(_largeAsteroidProbability)
                ? AsteroidSize.Large
                : AsteroidSize.Small;

            // A full pool is an expected limit; the next interval will retry without logging each time.
            Vector3 spawnPosition = GetSpawnPosition();
            int maximumHealth = AsteroidEvolution.GetMaximumHealth(size, _activeGameplayElapsed);
            _pool.TrySpawn(size, spawnPosition, GetCurrentAsteroidSpeedMultiplier(), maximumHealth);
        }

        [ContextMenu("Test/Spawn Speed Asteroid")]
        private void SpawnSpeedAsteroidForTesting()
        {
            if (!Application.isPlaying || !IsRunActive() || _pool == null || !IsValidCamera(_camera))
                return;

            float minimum = Mathf.Min(_speedVariantMinimumMultiplier, _speedVariantMaximumMultiplier);
            float maximum = Mathf.Max(_speedVariantMinimumMultiplier, _speedVariantMaximumMultiplier);
            float variantMultiplier = Random.Range(minimum, maximum);
            int maximumHealth = AsteroidEvolution.GetMaximumHealth(AsteroidSize.Small, _activeGameplayElapsed);
            Asteroid spawned = _pool.TrySpawnSpeed(
                GetSpawnPosition(),
                GetCurrentAsteroidSpeedMultiplier() * variantMultiplier,
                maximumHealth);

            if (spawned == null && !_speedPoolWarningReported)
            {
                Debug.LogWarning("Speed Asteroid test spawn failed; assign a valid Speed Asteroid prefab and ensure its pool has capacity.", this);
                _speedPoolWarningReported = true;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float inset = Mathf.Min(_verticalInset, halfHeight * 0.9f);
            float minimumY = _camera.transform.position.y - halfHeight + inset;
            float maximumY = _camera.transform.position.y + halfHeight - inset;
            float spawnY = Random.Range(minimumY, maximumY);
            Vector3 cameraPosition = _camera.transform.position;
            return new Vector3(cameraPosition.x + halfWidth + _rightEdgeOffset, spawnY, _pool.transform.position.z);
        }

        private void ResetDifficultyForNewRun()
        {
            _activeGameplayElapsed = 0d;
            _timeUntilNextSpawn = GetCurrentSpawnInterval();
            _runTimerInitialized = true;
        }

        private int GetDifficultyStep()
        {
            if (_difficultyStepSeconds <= 0f)
                return 0;

            double steps = System.Math.Floor(_activeGameplayElapsed / _difficultyStepSeconds);
            double speedSteps = _speedMultiplierIncreasePerStep > 0f
                ? (_maximumSpeedMultiplier - 1f) / _speedMultiplierIncreasePerStep
                : 0d;
            double frequencySteps = _frequencyMultiplierIncreasePerStep > 0f
                ? (_maximumFrequencyMultiplier - 1f) / _frequencyMultiplierIncreasePerStep
                : 0d;
            double maximumSteps = System.Math.Ceiling(System.Math.Max(speedSteps, frequencySteps));
            return (int)System.Math.Min(System.Math.Min(steps, maximumSteps), int.MaxValue);
        }

        private float GetCurrentAsteroidSpeedMultiplier()
        {
            return _initialAsteroidSpeedMultiplier * Mathf.Min(
                1f + GetDifficultyStep() * _speedMultiplierIncreasePerStep,
                _maximumSpeedMultiplier);
        }

        private float GetCurrentSpawnInterval()
        {
            float frequencyMultiplier = _initialSpawnFrequencyMultiplier * Mathf.Min(
                1f + GetDifficultyStep() * _frequencyMultiplierIncreasePerStep,
                _maximumFrequencyMultiplier);
            return _spawnInterval / Mathf.Max(0.01f, frequencyMultiplier);
        }

        private static bool IsValidCamera(Camera camera)
        {
            if (camera == null || !camera.orthographic || camera.aspect <= 0f)
                return false;

            Vector3 angles = camera.transform.eulerAngles;
            return Mathf.Abs(Mathf.DeltaAngle(0f, angles.x)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.y)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.z)) < 0.01f;
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
