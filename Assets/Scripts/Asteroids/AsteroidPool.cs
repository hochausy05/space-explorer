using System;
using SpaceExplorer.Core;
using SpaceExplorer.Core.Pooling;
using SpaceExplorer.Pickups;
using UnityEngine;

namespace SpaceExplorer.Asteroids
{
    public enum AsteroidSize
    {
        Small,
        Large
    }

    public enum AsteroidKind
    {
        Small,
        Large,
        Speed
    }

    public sealed class AsteroidPool : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Asteroid _smallAsteroidPrefab;
        [SerializeField] private Asteroid _largeAsteroidPrefab;
        [SerializeField] private Asteroid _speedAsteroidPrefab;

        [Header("Pool Capacity")]
        [SerializeField, Min(1)] private int _smallCapacity = 24;
        [SerializeField, Min(0)] private int _smallPrewarmCount = 16;
        [SerializeField, Min(1)] private int _largeCapacity = 8;
        [SerializeField, Min(0)] private int _largePrewarmCount = 8;
        [SerializeField, Min(1)] private int _speedCapacity = 8;
        [SerializeField, Min(0)] private int _speedPrewarmCount = 4;

        [Header("Speed Asteroid Rewards")]
        [SerializeField, Min(0)] private int _speedAsteroidScoreReward = 40;
        [SerializeField, Min(0)] private int _speedAsteroidStarDropCount = 8;

        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private StarPool _starPool;

        private PrefabPool<Asteroid> _smallPool;
        private PrefabPool<Asteroid> _largePool;
        private PrefabPool<Asteroid> _speedPool;

        public bool IsReady => _smallPool != null && _largePool != null;
        public bool IsSpeedReady => _speedPool != null;

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

            if (_smallPrewarmCount > _smallCapacity || _largePrewarmCount > _largeCapacity)
            {
                FailConfiguration("AsteroidPool prewarm counts cannot exceed their capacities.");
                return;
            }

            if (_speedAsteroidPrefab != null && _speedPrewarmCount > _speedCapacity)
            {
                Debug.LogError("AsteroidPool Speed prewarm count cannot exceed its capacity; Speed Asteroids will be unavailable.", this);
                _speedAsteroidPrefab = null;
            }

            try
            {
                _smallPool = new PrefabPool<Asteroid>(_smallAsteroidPrefab, _smallCapacity, _smallPrewarmCount, transform);
                _largePool = new PrefabPool<Asteroid>(_largeAsteroidPrefab, _largeCapacity, _largePrewarmCount, transform);
            }
            catch (ArgumentException exception)
            {
                _smallPool?.Dispose();
                _largePool?.Dispose();
                _speedPool?.Dispose();
                _smallPool = null;
                _largePool = null;
                _speedPool = null;
                FailConfiguration($"AsteroidPool configuration is invalid: {exception.Message}");
                return;
            }

            if (_speedAsteroidPrefab != null)
            {
                try
                {
                    _speedPool = new PrefabPool<Asteroid>(_speedAsteroidPrefab, _speedCapacity, _speedPrewarmCount, transform);
                }
                catch (ArgumentException exception)
                {
                    _speedPool?.Dispose();
                    _speedPool = null;
                    Debug.LogError($"Speed Asteroid pool configuration is invalid; Small/Large pools remain available: {exception.Message}", this);
                }
            }
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
            return TrySpawn(kind, position, speedMultiplier, maximumHealth);
        }

        public Asteroid TrySpawnSpeed(Vector3 position, float speedMultiplier, int maximumHealth)
        {
            return TrySpawn(AsteroidKind.Speed, position, speedMultiplier, maximumHealth);
        }

        private Asteroid TrySpawn(AsteroidKind kind, Vector3 position, float speedMultiplier, int maximumHealth)
        {
            PrefabPool<Asteroid> pool = GetPool(kind);
            if (pool == null)
                return null;

            Asteroid asteroid = pool.Get(position, Quaternion.identity);
            if (asteroid == null)
                return null;

            int resolvedMaximumHealth = maximumHealth > 0
                ? maximumHealth
                : GetPrefab(kind).MaximumHealth;
            asteroid.BeginRun(this, _camera, kind, speedMultiplier, resolvedMaximumHealth);
            return asteroid;
        }

        internal void OnAsteroidDestroyed(AsteroidKind kind, Vector3 position)
        {
            GameManager manager = GameManager.Instance;
            int scoreValue = kind == AsteroidKind.Speed
                ? Mathf.Max(0, _speedAsteroidScoreReward)
                : kind == AsteroidKind.Small ? 10 : 30;
            manager?.TryAddScore(scoreValue);

            if (_starPool == null || !_starPool.IsReady)
                return;

            int dropCount = kind == AsteroidKind.Speed
                ? Mathf.Max(0, _speedAsteroidStarDropCount)
                : kind == AsteroidKind.Small ? 1 : UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < dropCount; i++)
                _starPool.TrySpawn(position);
        }

        internal bool Release(Asteroid asteroid)
        {
            return (_smallPool != null && _smallPool.Release(asteroid))
                || (_largePool != null && _largePool.Release(asteroid))
                || (_speedPool != null && _speedPool.Release(asteroid));
        }

        private PrefabPool<Asteroid> GetPool(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return _smallPool;
                case AsteroidKind.Large: return _largePool;
                case AsteroidKind.Speed: return _speedPool;
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
                default: return null;
            }
        }

        private void OnDestroy()
        {
            _smallPool?.Dispose();
            _largePool?.Dispose();
            _speedPool?.Dispose();
            _smallPool = null;
            _largePool = null;
            _speedPool = null;
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
