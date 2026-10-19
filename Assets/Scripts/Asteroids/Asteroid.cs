using UnityEngine;
using SpaceExplorer.Core.Pooling;

namespace SpaceExplorer.Asteroids
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class Asteroid : PooledObject
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int _maximumHealth = 1;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _minimumSpeed = 2f;
        [SerializeField, Min(0f)] private float _maximumSpeed = 2f;
        [SerializeField] private float _minimumVerticalDrift = 0f;
        [SerializeField] private float _maximumVerticalDrift = 0f;
        [SerializeField, Min(0f)] private float _minimumRotationSpeed = 45f;
        [SerializeField, Min(0f)] private float _maximumRotationSpeed = 90f;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private AsteroidPool _owner;
        private Camera _camera;
        private AsteroidSize _size;
        private int _currentHealth;
        private bool _isInUse;

        public int CurrentHealth => _currentHealth;
        public int MaximumHealth => _maximumHealth;
        public AsteroidSize Size => _size;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<Renderer>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void BeginRun(AsteroidPool owner, Camera camera, AsteroidSize size)
        {
            if (!_isInUse || owner == null || camera == null || !camera.orthographic)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            _size = size;
            _currentHealth = Mathf.Max(1, _maximumHealth);

            float speed = Random.Range(_minimumSpeed, Mathf.Max(_minimumSpeed, _maximumSpeed));
            float drift = Random.Range(_minimumVerticalDrift, Mathf.Max(_minimumVerticalDrift, _maximumVerticalDrift));
            float rotation = Random.Range(_minimumRotationSpeed, Mathf.Max(_minimumRotationSpeed, _maximumRotationSpeed));
            rotation *= Random.value < 0.5f ? -1f : 1f;

            _rigidbody.position = transform.position;
            _rigidbody.linearVelocity = new Vector2(-speed, drift);
            _rigidbody.angularVelocity = rotation;
        }

        public void TakeDamage(int amount)
        {
            if (!_isInUse || amount <= 0)
                return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            if (_currentHealth == 0)
            {
                _owner?.OnAsteroidDestroyed(_size, transform.position);
                ReturnToPool();
            }
        }

        protected override void OnTakenFromPool()
        {
            _owner = null;
            _camera = null;
            _isInUse = true;
            _size = AsteroidSize.Small;
            _currentHealth = Mathf.Max(1, _maximumHealth);
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
            _currentHealth = Mathf.Max(1, _maximumHealth);
            _size = AsteroidSize.Small;
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

            if (_owner == null || _camera == null || _renderer == null || IsOutsideLeftEdge())
                ReturnToPool();
        }

        private bool IsOutsideLeftEdge()
        {
            float cameraLeft = _camera.transform.position.x - _camera.orthographicSize * _camera.aspect;
            return _renderer.bounds.max.x < cameraLeft;
        }

        private void ReturnToPool()
        {
            if (!_isInUse || _owner == null)
                return;

            _owner.Release(this);
        }
    }
}
