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

        // Kiểm tra pool hiệu ứng đã sẵn sàng nhận yêu cầu phát nổ.
        public bool IsReady => _pool != null;

        // Kiểm tra prefab và tạo pool hiệu ứng dùng chung cho các loại Asteroid.
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
                // Đặt root của pool ở cấp scene để transform của host không kéo lệch hiệu ứng.
                _pool = new PrefabPool<PooledAsteroidExplosion>(
                    _explosionPrefab, _initialCapacity, _prewarmCount, expandWhenExhausted: true);
            }
            catch (ArgumentException exception)
            {
                FailConfiguration($"AsteroidExplosionPool configuration is invalid: {exception.Message}");
            }
        }

        // Nhận hiệu ứng từ pool, đặt vị trí và kích cỡ theo loại Asteroid.
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

        // Nhận yêu cầu trả hiệu ứng từ object đã chạy xong.
        internal void Release(PooledAsteroidExplosion effect)
        {
            _pool?.Release(effect);
        }

        // Chọn hệ số scale riêng cho từng loại Asteroid.
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

        // Giải phóng pool khi scene hoặc chủ hiệu ứng bị hủy.
        private void OnDestroy()
        {
            _pool?.Dispose();
            _pool = null;
        }

        // Dừng hoạt động và chỉ ghi lỗi cấu hình một lần.
        private void FailConfiguration(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
        }
    }
}
