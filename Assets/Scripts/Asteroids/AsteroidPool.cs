using System;
using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Asteroids
{
    public enum AsteroidSize
    {
        Small,
        Large
    }

    public sealed class AsteroidPool : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Asteroid _smallAsteroidPrefab;
        [SerializeField] private Asteroid _largeAsteroidPrefab;

        [Header("Pool Capacity")]
        [SerializeField, Min(1)] private int _smallCapacity = 24;
        [SerializeField, Min(0)] private int _smallPrewarmCount = 16;
        [SerializeField, Min(1)] private int _largeCapacity = 8;
        [SerializeField, Min(0)] private int _largePrewarmCount = 8;

        [Header("References")]
        [SerializeField] private Camera _camera;

        private PrefabPool<Asteroid> _smallPool;
        private PrefabPool<Asteroid> _largePool;

        public bool IsReady => _smallPool != null && _largePool != null;

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

            if (_smallPrewarmCount > _smallCapacity || _largePrewarmCount > _largeCapacity)
            {
                FailConfiguration("AsteroidPool prewarm counts cannot exceed their capacities.");
                return;
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
                _smallPool = null;
                _largePool = null;
                FailConfiguration($"AsteroidPool configuration is invalid: {exception.Message}");
            }
        }

        public Asteroid TrySpawn(AsteroidSize size, Vector3 position)
        {
            if (!IsReady)
                return null;

            PrefabPool<Asteroid> pool = size == AsteroidSize.Small ? _smallPool : _largePool;
            Asteroid asteroid = pool.Get(position, Quaternion.identity);
            if (asteroid == null)
                return null;

            asteroid.BeginRun(this, _camera);
            return asteroid;
        }

        internal bool Release(Asteroid asteroid)
        {
            return (_smallPool != null && _smallPool.Release(asteroid))
                || (_largePool != null && _largePool.Release(asteroid));
        }

        private void OnDestroy()
        {
            _smallPool?.Dispose();
            _largePool?.Dispose();
            _smallPool = null;
            _largePool = null;
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
