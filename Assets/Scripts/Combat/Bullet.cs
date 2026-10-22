using BigInteger = System.Numerics.BigInteger;
using SpaceExplorer.Audio;
using UnityEngine;
using SpaceExplorer.Core.Pooling;
using SpaceExplorer.Asteroids;

namespace SpaceExplorer.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class Bullet : PooledObject
    {
        [SerializeField, Min(0.1f)] private float _speed = 12f;
        [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;
        [SerializeField, Min(1)] private int _damage = 1;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private BulletPool _owner;
        private Camera _camera;
        private int _configuredDamage;
        private BigInteger _damageValue;
        private float _remainingLifetime;
        private bool _isInUse;

        // Lấy component cần dùng và xác nhận collider đã được lưu dạng trigger trong prefab.
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            CircleCollider2D bulletCollider = GetComponent<CircleCollider2D>();
            if (bulletCollider == null)
            {
                FailConfiguration("Bullet prefab is missing its serialized CircleCollider2D component.");
                return;
            }

            if (!bulletCollider.isTrigger)
            {
                FailConfiguration("Bullet prefab CircleCollider2D must be configured as a trigger.");
                return;
            }

            _renderer = GetComponent<Renderer>();
            _configuredDamage = Mathf.Max(1, _damage);
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        // Khởi chạy đạn theo hướng phải với sát thương mặc định của prefab.
        public void Launch(BulletPool owner, Camera camera)
        {
            Launch(owner, camera, Vector2.right);
        }

        // Khởi chạy đạn theo hướng chỉ định với sát thương mặc định.
        public void Launch(BulletPool owner, Camera camera, Vector2 direction)
        {
            Launch(owner, camera, direction, new BigInteger(_configuredDamage));
        }

        // Chuyển sát thương int sang dạng lớn trước khi dùng overload chung.
        public void Launch(BulletPool owner, Camera camera, Vector2 direction, int damage)
        {
            Launch(owner, camera, direction, new BigInteger(damage));
        }

        // Gắn lại pool/camera và đặt sát thương, thời hạn, vận tốc cho viên đạn vừa lấy khỏi pool.
        public void Launch(BulletPool owner, Camera camera, Vector2 direction, BigInteger damage)
        {
            if (!_isInUse || owner == null || camera == null)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            _damageValue = BigInteger.Max(BigInteger.One, damage);
            _remainingLifetime = _maxLifetime;
            _rigidbody.position = transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            _rigidbody.linearVelocity = direction.normalized * _speed;
        }

        // Reset trạng thái chuyển động và sát thương khi lấy đạn khỏi pool.
        protected override void OnTakenFromPool()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            _owner = null;
            _camera = null;
            _remainingLifetime = _maxLifetime;
            _isInUse = true;
            _damageValue = new BigInteger(_configuredDamage);
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        // Xóa vận tốc và liên kết runtime khi trả đạn về pool.
        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
            _remainingLifetime = 0f;
            _damageValue = new BigInteger(_configuredDamage);
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }

        // Giảm thời hạn đạn và thu hồi khi hết giờ hoặc rời camera.
        private void FixedUpdate()
        {
            if (!_isInUse)
                return;

            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f || IsOutsideCamera())
                ReturnToPool();
        }

        // Chỉ Asteroid mới nhận sát thương; đánh dấu đạn đã dùng trước callback để tránh hit lặp.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInUse || _owner == null || other == null)
                return;

            if (!other.TryGetComponent(out Asteroid asteroid))
                return;

            // Đánh dấu đạn đã dùng trước khi gây sát thương để callback kế tiếp bị bỏ qua.
            BulletPool owner = _owner;
            _isInUse = false;
            SfxAudioService.PlayBulletHit();
            asteroid.TakeDamageFromPlayerProjectile(_damageValue);
            owner.Release(this);
        }

        // So sánh bounds của đạn với camera để không giữ đạn ngoài màn hình.
        private bool IsOutsideCamera()
        {
            if (_camera == null || _renderer == null)
                return true;

            Bounds bounds = _renderer.bounds;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector3 cameraPosition = _camera.transform.position;

            return bounds.max.x < cameraPosition.x - halfWidth
                || bounds.min.x > cameraPosition.x + halfWidth
                || bounds.max.y < cameraPosition.y - halfHeight
                || bounds.min.y > cameraPosition.y + halfHeight;
        }

        // Trả đạn một lần về pool đang sở hữu nó.
        private void ReturnToPool()
        {
            if (!_isInUse || _owner == null)
                return;

            _isInUse = false;
            _owner.Release(this);
        }

        // Tắt component và báo lỗi khi prefab thiếu cấu hình bắt buộc.
        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
