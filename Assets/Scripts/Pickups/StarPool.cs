using System;
using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Pickups
{
    public sealed class StarPool : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private StarPickup _starPrefab;

        [Header("Pool Capacity")]
        [SerializeField, Min(1)] private int _capacity = 64;
        [SerializeField, Min(0)] private int _prewarmCount = 64;

        [Header("References")]
        [SerializeField] private Camera _camera;

        private PrefabPool<StarPickup> _pool;

        public bool IsReady => _pool != null;

        private void Awake()
        {
            if (_starPrefab == null)
            {
                FailConfiguration("StarPool needs a Star prefab assigned.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || !_camera.orthographic)
            {
                FailConfiguration("StarPool needs an orthographic gameplay camera assigned.");
                return;
            }

            if (_prewarmCount > _capacity)
            {
                FailConfiguration("StarPool prewarm count cannot exceed its capacity.");
                return;
            }

            try
            {
                _pool = new PrefabPool<StarPickup>(_starPrefab, _capacity, _prewarmCount, transform);
            }
            catch (ArgumentException exception)
            {
                FailConfiguration($"StarPool configuration is invalid: {exception.Message}");
            }
        }

        public bool TrySpawn(Vector3 position)
        {
            if (_pool == null || _camera == null)
                return false;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.2f;
            StarPickup star = _pool.Get(position + new Vector3(offset.x, offset.y, 0f), Quaternion.identity);
            if (star == null)
                return false;

            star.BeginDrift(this, _camera);
            return true;
        }

        internal bool Release(StarPickup star)
        {
            return _pool != null && _pool.Release(star);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
            _pool = null;
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
