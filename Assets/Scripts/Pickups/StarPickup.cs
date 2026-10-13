using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Pickups
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class StarPickup : PooledObject
    {
        [Header("Pickup")]
        [SerializeField, Min(1)] private int _xpValue = 1;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _minimumSpeed = 0.8f;
        [SerializeField, Min(0f)] private float _maximumSpeed = 1.4f;
        [SerializeField] private float _minimumVerticalDrift = -0.35f;
        [SerializeField] private float _maximumVerticalDrift = 0.35f;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private StarPool _owner;
        private Camera _camera;
        private bool _isInUse;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            CircleCollider2D pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            _renderer = GetComponent<Renderer>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        public void BeginDrift(StarPool owner, Camera camera)
        {
            if (!_isInUse || owner == null || camera == null || !camera.orthographic)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            float speed = Random.Range(_minimumSpeed, Mathf.Max(_minimumSpeed, _maximumSpeed));
            float verticalDrift = Random.Range(_minimumVerticalDrift, Mathf.Max(_minimumVerticalDrift, _maximumVerticalDrift));
            _rigidbody.position = transform.position;
            _rigidbody.linearVelocity = new Vector2(-speed, verticalDrift);
            _rigidbody.angularVelocity = 0f;
        }

        protected override void OnTakenFromPool()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            _owner = null;
            _camera = null;
            _isInUse = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInUse || _owner == null || other == null || !other.TryGetComponent(out StarXPCollector collector))
                return;

            StarPool owner = _owner;
            _isInUse = false;
            collector.AddXP(_xpValue);
            owner.Release(this);
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

            _isInUse = false;
            _owner.Release(this);
        }
    }
}
