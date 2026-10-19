using System;
using BigInteger = System.Numerics.BigInteger;
using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Combat
{
    public sealed class BulletPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private Bullet _bulletPrefab;
        [SerializeField, Min(1)] private int _capacity = 64;
        [SerializeField, Min(0)] private int _prewarmCount = 12;

        [Header("References")]
        [SerializeField] private Camera _camera;

        private PrefabPool<Bullet> _pool;

        public bool IsReady => _pool != null;

        private void Awake()
        {
            if (_bulletPrefab == null)
            {
                FailConfiguration("BulletPool needs a Bullet prefab assigned.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || !_camera.orthographic)
            {
                FailConfiguration("BulletPool needs an orthographic gameplay camera assigned.");
                return;
            }

            if (_prewarmCount > _capacity)
            {
                FailConfiguration("BulletPool prewarm count cannot exceed its capacity.");
                return;
            }

            try
            {
                _pool = new PrefabPool<Bullet>(_bulletPrefab, _capacity, _prewarmCount, transform);
            }
            catch (ArgumentException exception)
            {
                FailConfiguration($"BulletPool configuration is invalid: {exception.Message}");
            }
        }

        public bool TrySpawn(Vector3 position)
        {
            return TrySpawn(position, Vector2.right, BigInteger.Zero, hasDamageOverride: false);
        }

        public bool TrySpawn(Vector3 position, Vector2 direction, int damage)
        {
            if (damage < 1)
                return false;

            return TrySpawn(position, direction, new BigInteger(damage));
        }

        public bool TrySpawn(Vector3 position, Vector2 direction, BigInteger damage)
        {
            if (damage < BigInteger.One)
                return false;

            return TrySpawn(position, direction, damage, hasDamageOverride: true);
        }

        private bool TrySpawn(Vector3 position, Vector2 direction, BigInteger damage, bool hasDamageOverride)
        {
            if (_pool == null || _camera == null)
                return false;

            Bullet bullet = _pool.Get(position, Quaternion.identity);
            if (bullet == null)
                return false;

            if (hasDamageOverride)
                bullet.Launch(this, _camera, direction, damage);
            else
                bullet.Launch(this, _camera, direction);
            return true;
        }

        internal bool Release(Bullet bullet)
        {
            return _pool != null && _pool.Release(bullet);
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
