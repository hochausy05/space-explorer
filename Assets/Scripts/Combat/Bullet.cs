using UnityEngine;
using SpaceExplorer.Core.Pooling;

namespace SpaceExplorer.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public sealed class Bullet : PooledObject
    {
        [SerializeField, Min(0.1f)] private float _speed = 12f;
        [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private BulletPool _owner;
        private Camera _camera;
        private float _remainingLifetime;
        private bool _isInUse;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<Renderer>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        public void Launch(BulletPool owner, Camera camera)
        {
            if (!_isInUse || owner == null || camera == null)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            _remainingLifetime = _maxLifetime;
            _rigidbody.position = transform.position;
            _rigidbody.linearVelocity = Vector2.right * _speed;
        }

        protected override void OnTakenFromPool()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            _owner = null;
            _camera = null;
            _remainingLifetime = _maxLifetime;
            _isInUse = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
            _remainingLifetime = 0f;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!_isInUse)
                return;

            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f || IsOutsideCamera())
                ReturnToPool();
        }

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

        private void ReturnToPool()
        {
            if (!_isInUse || _owner == null)
                return;

            _isInUse = false;
            _owner.Release(this);
        }
    }
}
