using System;
using SpaceExplorer.Core;
using SpaceExplorer.Core.Pooling;
using SpaceExplorer.Effects;
using SpaceExplorer.Pickups;
using UnityEngine;

namespace SpaceExplorer.Asteroids
{
    public enum AsteroidSize { Small, Large }

    public enum AsteroidKind
    {
        Small,
        Large,
        Speed,
        Homing,
        Erratic,
        Splitter,
        Fragment
    }

    public sealed class AsteroidPool : MonoBehaviour
    {
        [Header("Standard Prefabs")]
        [SerializeField] private Asteroid _smallAsteroidPrefab;
        [SerializeField] private Asteroid _largeAsteroidPrefab;

        [Header("Special Prefabs")]
        [SerializeField] private Asteroid _speedAsteroidPrefab;
        [SerializeField] private Asteroid _homingAsteroidPrefab;
        [SerializeField] private Asteroid _erraticAsteroidPrefab;
        [SerializeField] private Asteroid _splitterAsteroidPrefab;
        [SerializeField] private Asteroid _splitterFragmentPrefab;

        [Header("Standard Pool Capacity")]
        [SerializeField, Min(1)] private int _smallCapacity = 24;
        [SerializeField, Min(0)] private int _smallPrewarmCount = 16;
        [SerializeField, Min(1)] private int _largeCapacity = 8;
        [SerializeField, Min(0)] private int _largePrewarmCount = 8;

        [Header("Special Pool Initial Capacity (grows as needed)")]
        [SerializeField, Min(1)] private int _specialInitialCapacity = 8;
        [SerializeField, Min(0)] private int _specialPrewarmCount;

        [Header("Destruction Rewards")]
        [SerializeField, Min(0)] private int _speedAsteroidScoreReward = 40;
        [SerializeField, Min(0)] private int _speedAsteroidStarDropCount = 8;
        [SerializeField, Min(0)] private int _homingAsteroidScoreReward = 50;
        [SerializeField, Min(0)] private int _homingAsteroidStarDropCount = 9;
        [SerializeField, Min(0)] private int _erraticAsteroidScoreReward = 45;
        [SerializeField, Min(0)] private int _erraticAsteroidStarDropCount = 8;
        [SerializeField, Min(0)] private int _splitterAsteroidScoreReward = 60;
        [SerializeField, Min(0)] private int _splitterAsteroidStarDropCount = 10;

        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private StarPool _starPool;
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private AsteroidExplosionPool _explosionPool;

        private PrefabPool<Asteroid> _smallPool;
        private PrefabPool<Asteroid> _largePool;
        private PrefabPool<Asteroid> _speedPool;
        private PrefabPool<Asteroid> _homingPool;
        private PrefabPool<Asteroid> _erraticPool;
        private PrefabPool<Asteroid> _splitterPool;
        private PrefabPool<Asteroid> _fragmentPool;
        private bool _configurationErrorReported;
        private bool _starCapacityWarningReported;
        private bool _explosionPoolWarningReported;

        public bool IsReady => _smallPool != null && _largePool != null;
        public bool AreAllSpecialPoolsReady => _speedPool != null && _homingPool != null
            && _erraticPool != null && _splitterPool != null && _fragmentPool != null
            && _starPool != null && _starPool.IsReady && _playerTarget != null;

        private void Awake()
        {
            if (_smallAsteroidPrefab == null || _largeAsteroidPrefab == null)
            {
                FailConfiguration("AsteroidPool needs both Small and Large asteroid prefabs assigned.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || !_camera.orthographic)
            {
                FailConfiguration("AsteroidPool needs an orthographic gameplay camera assigned.");
                return;
            }

            if (_starPool == null)
            {
                FailConfiguration("AsteroidPool needs a StarPool assigned so destroyed asteroids can drop Stars.");
                return;
            }

            if (_smallPrewarmCount > _smallCapacity || _largePrewarmCount > _largeCapacity
                || _specialPrewarmCount > _specialInitialCapacity)
            {
                FailConfiguration("AsteroidPool prewarm counts cannot exceed their configured initial capacities.");
                return;
            }

            try
            {
                _smallPool = new PrefabPool<Asteroid>(_smallAsteroidPrefab, _smallCapacity, _smallPrewarmCount, transform);
                _largePool = new PrefabPool<Asteroid>(_largeAsteroidPrefab, _largeCapacity, _largePrewarmCount, transform);
            }
            catch (ArgumentException exception)
            {
                DisposePools();
                FailConfiguration($"AsteroidPool configuration is invalid: {exception.Message}");
                return;
            }

            _speedPool = CreateSpecialPool(_speedAsteroidPrefab, "Speed");
            _homingPool = CreateSpecialPool(_homingAsteroidPrefab, "Homing");
            _erraticPool = CreateSpecialPool(_erraticAsteroidPrefab, "Erratic");
            _splitterPool = CreateSpecialPool(_splitterAsteroidPrefab, "Splitter");
            _fragmentPool = CreateSpecialPool(_splitterFragmentPrefab, "Splitter fragment");
        }

        public Asteroid TrySpawn(AsteroidSize size, Vector3 position)
        {
            return TrySpawn(size, position, 1f);
        }

        public Asteroid TrySpawn(AsteroidSize size, Vector3 position, float speedMultiplier)
        {
            return TrySpawn(size, position, speedMultiplier, 0);
        }

        public Asteroid TrySpawn(AsteroidSize size, Vector3 position, float speedMultiplier, int maximumHealth)
        {
            AsteroidKind kind = size == AsteroidSize.Small ? AsteroidKind.Small : AsteroidKind.Large;
            return TrySpawnKind(kind, position, speedMultiplier, maximumHealth, useStandardSpeed: true);
        }

        // Compatibility overload for the original Speed test spawn, where the caller supplies the full multiplier.
        public Asteroid TrySpawnSpeed(Vector3 position, float speedMultiplier, int maximumHealth)
        {
            return TrySpawnKind(AsteroidKind.Speed, position, speedMultiplier, maximumHealth, useStandardSpeed: false);
        }

        public Asteroid TrySpawnSpeed(Vector3 position, float difficultySpeedMultiplier, float variantMultiplier, int maximumHealth)
        {
            return TrySpawnKind(AsteroidKind.Speed, position,
                Mathf.Max(0f, difficultySpeedMultiplier) * Mathf.Clamp(variantMultiplier, 1.5f, 2f),
                maximumHealth, useStandardSpeed: false);
        }

        public Asteroid TrySpawnSpecial(AsteroidKind kind, Vector3 position, float difficultySpeedMultiplier, int maximumHealth)
        {
            if (kind != AsteroidKind.Homing && kind != AsteroidKind.Erratic && kind != AsteroidKind.Splitter)
                return null;

            return TrySpawnKind(kind, position, difficultySpeedMultiplier, maximumHealth, useStandardSpeed: false);
        }

        internal bool TrySpawnFragments(Vector3 parentPosition, float separation, float difficultySpeedMultiplier)
        {
            if (_fragmentPool == null)
                return false;

            float offset = Mathf.Max(0.25f, separation);
            Vector3 firstPosition = parentPosition + Vector3.up * offset;
            Vector3 secondPosition = parentPosition + Vector3.down * offset;
            Asteroid first = TrySpawnFragment(firstPosition, 155f, difficultySpeedMultiplier);
            Asteroid second = TrySpawnFragment(secondPosition, 205f, difficultySpeedMultiplier);
            return first != null && second != null;
        }

        internal void OnAsteroidDestroyed(AsteroidKind kind, Vector3 position, bool causedByPlayerProjectile,
            float fragmentSeparation, float difficultySpeedMultiplier)
        {
            if (kind == AsteroidKind.Fragment)
            {
                PlayDestructionExplosion(kind, position);
                return;
            }

            int scoreReward = GetScoreReward(kind);
            int starReward = GetStarReward(kind);
            GameManager.Instance?.TryAddScore(scoreReward);

            int spawnedCount = 0;
            bool allStarsSpawned = starReward <= 0
                || (_starPool != null && _starPool.TrySpawnBatch(position, starReward, out spawnedCount));
            if (!allStarsSpawned && !_starCapacityWarningReported)
            {
                Debug.LogError($"Asteroid reward requested {starReward} Stars but only {spawnedCount} could be spawned. Check StarPool configuration.", this);
                _starCapacityWarningReported = true;
            }

            PlayDestructionExplosion(kind, position);

            if (kind == AsteroidKind.Splitter && causedByPlayerProjectile
                && !TrySpawnFragments(position, fragmentSeparation, difficultySpeedMultiplier)
                && !_fragmentPoolWarningReported)
            {
                Debug.LogError("Splitter was destroyed by a player projectile but both fragments could not be spawned. Check the Splitter Fragment prefab configuration.", this);
                _fragmentPoolWarningReported = true;
            }
        }

        private void PlayDestructionExplosion(AsteroidKind kind, Vector3 position)
        {
            if (_explosionPool != null)
            {
                _explosionPool.Play(kind, position);
                return;
            }

            if (_explosionPoolWarningReported)
                return;

            Debug.LogWarning("AsteroidPool has no AsteroidExplosionPool assigned; asteroid destruction visuals are disabled.", this);
            _explosionPoolWarningReported = true;
        }

        internal bool Release(Asteroid asteroid)
        {
            return (_smallPool != null && _smallPool.Release(asteroid))
                || (_largePool != null && _largePool.Release(asteroid))
                || (_speedPool != null && _speedPool.Release(asteroid))
                || (_homingPool != null && _homingPool.Release(asteroid))
                || (_erraticPool != null && _erraticPool.Release(asteroid))
                || (_splitterPool != null && _splitterPool.Release(asteroid))
                || (_fragmentPool != null && _fragmentPool.Release(asteroid));
        }

        private bool _fragmentPoolWarningReported;

        private Asteroid TrySpawnFragment(Vector3 position, float directionDegrees, float difficultySpeedMultiplier)
        {
            Asteroid fragment = GetFromPool(_fragmentPool, position);
            if (fragment == null)
                return null;

            fragment.SetFragmentDirection(directionDegrees);
            float speed = _largeAsteroidPrefab.RollBaseSpeed();
            fragment.BeginRun(this, _camera, AsteroidKind.Fragment, difficultySpeedMultiplier * 1.4f, 1, speed);
            return fragment;
        }

        private Asteroid TrySpawnKind(AsteroidKind kind, Vector3 position, float speedMultiplier, int maximumHealth, bool useStandardSpeed)
        {
            PrefabPool<Asteroid> pool = GetPool(kind);
            Asteroid asteroid = GetFromPool(pool, position);
            if (asteroid == null)
                return null;

            float baseSpeed = -1f;
            Transform target = null;
            if (!useStandardSpeed)
            {
                Asteroid speedSource = kind == AsteroidKind.Large || kind == AsteroidKind.Splitter
                    ? _largeAsteroidPrefab
                    : _smallAsteroidPrefab;
                baseSpeed = speedSource.RollBaseSpeed();
                if (kind == AsteroidKind.Homing)
                    target = _playerTarget;
            }

            int resolvedHealth = maximumHealth > 0 ? maximumHealth : GetPrefab(kind).MaximumHealth;
            asteroid.BeginRun(this, _camera, kind, speedMultiplier, resolvedHealth, baseSpeed, target);
            return asteroid;
        }

        private Asteroid GetFromPool(PrefabPool<Asteroid> pool, Vector3 position)
        {
            if (pool == null)
                return null;

            return pool.Get(position, Quaternion.identity);
        }

        private PrefabPool<Asteroid> CreateSpecialPool(Asteroid prefab, string label)
        {
            if (prefab == null)
                return null;

            try
            {
                return new PrefabPool<Asteroid>(prefab, _specialInitialCapacity, _specialPrewarmCount, transform,
                    expandWhenExhausted: true);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"AsteroidPool {label} pool configuration is invalid: {exception.Message}", this);
                return null;
            }
        }

        private int GetScoreReward(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return 10;
                case AsteroidKind.Large: return 30;
                case AsteroidKind.Speed: return Mathf.Max(0, _speedAsteroidScoreReward);
                case AsteroidKind.Homing: return Mathf.Max(0, _homingAsteroidScoreReward);
                case AsteroidKind.Erratic: return Mathf.Max(0, _erraticAsteroidScoreReward);
                case AsteroidKind.Splitter: return Mathf.Max(0, _splitterAsteroidScoreReward);
                default: return 0;
            }
        }

        private int GetStarReward(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return 1;
                case AsteroidKind.Large: return UnityEngine.Random.Range(2, 4);
                case AsteroidKind.Speed: return Mathf.Max(0, _speedAsteroidStarDropCount);
                case AsteroidKind.Homing: return Mathf.Max(0, _homingAsteroidStarDropCount);
                case AsteroidKind.Erratic: return Mathf.Max(0, _erraticAsteroidStarDropCount);
                case AsteroidKind.Splitter: return Mathf.Max(0, _splitterAsteroidStarDropCount);
                default: return 0;
            }
        }

        private PrefabPool<Asteroid> GetPool(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return _smallPool;
                case AsteroidKind.Large: return _largePool;
                case AsteroidKind.Speed: return _speedPool;
                case AsteroidKind.Homing: return _homingPool;
                case AsteroidKind.Erratic: return _erraticPool;
                case AsteroidKind.Splitter: return _splitterPool;
                case AsteroidKind.Fragment: return _fragmentPool;
                default: return null;
            }
        }

        private Asteroid GetPrefab(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return _smallAsteroidPrefab;
                case AsteroidKind.Large: return _largeAsteroidPrefab;
                case AsteroidKind.Speed: return _speedAsteroidPrefab;
                case AsteroidKind.Homing: return _homingAsteroidPrefab;
                case AsteroidKind.Erratic: return _erraticAsteroidPrefab;
                case AsteroidKind.Splitter: return _splitterAsteroidPrefab;
                case AsteroidKind.Fragment: return _splitterFragmentPrefab;
                default: return null;
            }
        }

        private void OnDestroy()
        {
            DisposePools();
        }

        private void DisposePools()
        {
            _smallPool?.Dispose();
            _largePool?.Dispose();
            _speedPool?.Dispose();
            _homingPool?.Dispose();
            _erraticPool?.Dispose();
            _splitterPool?.Dispose();
            _fragmentPool?.Dispose();
            _smallPool = _largePool = _speedPool = _homingPool = _erraticPool = _splitterPool = _fragmentPool = null;
        }

        private void FailConfiguration(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
            enabled = false;
        }
    }
}
