using System;
using SpaceExplorer.Asteroids;
using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Effects
{
    public sealed class AsteroidExplosionPool : MonoBehaviour
    {
        [Header("Effect Prefab and Pool")]
        [SerializeField] private PooledAsteroidExplosion _explosionPrefab;
        [SerializeField, Min(1)] private int _initialCapacity = 24;
        [SerializeField, Min(0)] private int _prewarmCount = 8;

        [Header("Scale Multipliers")]
        [SerializeField, Min(0f)] private float _smallScale = 0.8f;
        [SerializeField, Min(0f)] private float _largeScale = 1.1f;
        [SerializeField, Min(0f)] private float _speedScale = 1f;
        [SerializeField, Min(0f)] private float _homingScale = 1.2f;
        [SerializeField, Min(0f)] private float _erraticScale = 1.2f;
        [SerializeField, Min(0f)] private float _splitterScale = 1.3f;
        [SerializeField, Min(0f)] private float _fragmentScale = 0.6f;

        private PrefabPool<PooledAsteroidExplosion> _pool;
        private bool _configurationErrorReported;

        public bool IsReady => _pool != null;

        private void Awake()
        {
            if (_explosionPrefab == null)
            {
                FailConfiguration("AsteroidExplosionPool needs the AsteroidExplosion prefab assigned.");
                return;
            }

            if (!_explosionPrefab.TryValidate(out string validationError))
            {
                FailConfiguration($"AsteroidExplosionPool prefab is invalid: {validationError}");
                return;
            }

            if (_prewarmCount > _initialCapacity)
            {
                FailConfiguration("AsteroidExplosionPool prewarm count cannot exceed its initial capacity.");
                return;
            }

            try
            {
                // Leave the effect pool root at the scene root so pool-host transforms cannot move effects.
                _pool = new PrefabPool<PooledAsteroidExplosion>(
                    _explosionPrefab, _initialCapacity, _prewarmCount, expandWhenExhausted: true);
            }
            catch (ArgumentException exception)
            {
                FailConfiguration($"AsteroidExplosionPool configuration is invalid: {exception.Message}");
            }
        }

        public bool Play(AsteroidKind kind, Vector3 worldPosition)
        {
            if (_pool == null)
                return false;

            PooledAsteroidExplosion effect = _pool.Get(worldPosition, Quaternion.identity);
            if (effect == null)
                return false;

            if (effect.BeginPlayback(this, GetScaleMultiplier(kind)))
                return true;

            FailConfiguration("AsteroidExplosionPool could not start the prefab's default animation state.");
            _pool.Release(effect);
            return false;
        }

        internal void Release(PooledAsteroidExplosion effect)
        {
            _pool?.Release(effect);
        }

        private float GetScaleMultiplier(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Small: return _smallScale;
                case AsteroidKind.Large: return _largeScale;
                case AsteroidKind.Speed: return _speedScale;
                case AsteroidKind.Homing: return _homingScale;
                case AsteroidKind.Erratic: return _erraticScale;
                case AsteroidKind.Splitter: return _splitterScale;
                case AsteroidKind.Fragment: return _fragmentScale;
                default: return 1f;
            }
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
            _pool = null;
        }

        private void FailConfiguration(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }
    }
}
