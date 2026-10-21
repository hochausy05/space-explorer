using BigInteger = System.Numerics.BigInteger;
using SpaceExplorer.Audio;
using UnityEngine;
using SpaceExplorer.Core.Pooling;

namespace SpaceExplorer.Asteroids
{
    public enum ErraticMovementPattern
    {
        SmoothWave,
        SmoothZigzag
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class Asteroid : PooledObject
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int _maximumHealth = 1;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _minimumSpeed = 2f;
        [SerializeField, Min(0f)] private float _maximumSpeed = 2f;
        [SerializeField] private float _minimumVerticalDrift;
        [SerializeField] private float _maximumVerticalDrift;
        [SerializeField, Min(0f)] private float _minimumRotationSpeed = 45f;
        [SerializeField, Min(0f)] private float _maximumRotationSpeed = 90f;

        [Header("Homing")]
        [SerializeField, Min(0f)] private float _homingDurationSeconds = 3f;
        [SerializeField, Min(0f)] private float _homingTurnRateDegreesPerSecond = 75f;

        [Header("Erratic")]
        [SerializeField] private ErraticMovementPattern _erraticMovementPattern = ErraticMovementPattern.SmoothWave;
        [SerializeField, Min(0f)] private float _erraticWaveAmplitude = 0.7f;
        [SerializeField, Min(0f)] private float _erraticWaveFrequency = 1.2f;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private Collider2D _collider;
        private AsteroidPool _owner;
        private Camera _camera;
        private Transform _playerTarget;
        private AsteroidSize _size;
        private AsteroidKind _kind;
        private Vector2 _movementDirection;
        private float _movementSpeed;
        private float _difficultySpeedMultiplier;
        private RigidbodyConstraints2D _originalConstraints;
        private float _variantElapsed;
        private float _fragmentDirectionDegrees;
        private int _currentHealth;
        private int _currentMaximumHealth;
        private bool _isInUse;
        private bool _hasEnteredCamera;

        public int CurrentHealth => _currentHealth;
        public int MaximumHealth => _currentMaximumHealth > 0 ? _currentMaximumHealth : Mathf.Max(1, _maximumHealth);
        public AsteroidSize Size => _size;
        public AsteroidKind Kind => _kind;
        internal float FragmentSeparation => _collider != null ? Mathf.Max(0.2f, _collider.bounds.extents.y * 1.2f) : 0.5f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider2D>();
            _originalConstraints = _rigidbody.constraints;
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        internal float RollBaseSpeed()
        {
            return Random.Range(_minimumSpeed, Mathf.Max(_minimumSpeed, _maximumSpeed));
        }

        public void BeginRun(AsteroidPool owner, Camera camera, AsteroidSize size)
        {
            AsteroidKind kind = size == AsteroidSize.Small ? AsteroidKind.Small : AsteroidKind.Large;
            BeginRun(owner, camera, kind, 1f, _maximumHealth);
        }

        public void BeginRun(AsteroidPool owner, Camera camera, AsteroidSize size, float speedMultiplier)
        {
            AsteroidKind kind = size == AsteroidSize.Small ? AsteroidKind.Small : AsteroidKind.Large;
            BeginRun(owner, camera, kind, speedMultiplier, _maximumHealth);
        }

        public void BeginRun(AsteroidPool owner, Camera camera, AsteroidSize size, float speedMultiplier, int maximumHealth)
        {
            AsteroidKind kind = size == AsteroidSize.Small ? AsteroidKind.Small : AsteroidKind.Large;
            BeginRun(owner, camera, kind, speedMultiplier, maximumHealth);
        }

        internal void BeginRun(
            AsteroidPool owner,
            Camera camera,
            AsteroidKind kind,
            float speedMultiplier,
            int maximumHealth,
            float baseSpeed = -1f,
            Transform playerTarget = null)
        {
            if (!_isInUse || owner == null || camera == null || !camera.orthographic)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            _playerTarget = playerTarget;
            _kind = kind;
            _size = kind == AsteroidKind.Large || kind == AsteroidKind.Splitter ? AsteroidSize.Large : AsteroidSize.Small;
            _currentMaximumHealth = kind == AsteroidKind.Fragment ? 1 : Mathf.Max(1, maximumHealth);
            _currentHealth = _currentMaximumHealth;
            _variantElapsed = 0f;
            _hasEnteredCamera = false;

            float resolvedBaseSpeed = baseSpeed >= 0f ? baseSpeed : RollBaseSpeed();
            _movementSpeed = resolvedBaseSpeed * Mathf.Max(0f, speedMultiplier);
            _difficultySpeedMultiplier = Mathf.Max(0f, speedMultiplier);

            if (kind == AsteroidKind.Fragment)
                _movementDirection = DirectionFromDegrees(_fragmentDirectionDegrees);
            else
            {
                _movementDirection = Vector2.left;
            }

            float drift = kind == AsteroidKind.Speed || kind == AsteroidKind.Homing
                || kind == AsteroidKind.Erratic || kind == AsteroidKind.Fragment
                ? 0f
                : Random.Range(_minimumVerticalDrift, Mathf.Max(_minimumVerticalDrift, _maximumVerticalDrift));
            float rotation = kind == AsteroidKind.Speed
                ? 0f
                : Random.Range(_minimumRotationSpeed, Mathf.Max(_minimumRotationSpeed, _maximumRotationSpeed))
                  * (Random.value < 0.5f ? -1f : 1f);

            _rigidbody.constraints = kind == AsteroidKind.Speed
                ? _originalConstraints | RigidbodyConstraints2D.FreezeRotation
                : _originalConstraints;
            _rigidbody.position = transform.position;
            _rigidbody.linearVelocity = kind == AsteroidKind.Erratic
                ? new Vector2(-_movementSpeed, 0f)
                : _movementDirection * _movementSpeed + Vector2.up * drift;
            _rigidbody.angularVelocity = rotation;
        }

        internal void SetFragmentDirection(float degrees)
        {
            _fragmentDirectionDegrees = degrees;
        }

        public void TakeDamage(int amount)
        {
            ApplyDamage(new BigInteger(amount), causedByPlayerProjectile: false);
        }

        public void TakeDamage(BigInteger amount)
        {
            ApplyDamage(amount, causedByPlayerProjectile: false);
        }

        public void TakeDamageFromPlayerProjectile(BigInteger amount)
        {
            ApplyDamage(amount, causedByPlayerProjectile: true);
        }

        private void ApplyDamage(BigInteger amount, bool causedByPlayerProjectile)
        {
            if (!_isInUse || amount <= BigInteger.Zero)
                return;

            _currentHealth = amount >= _currentHealth ? 0 : _currentHealth - (int)amount;
            if (_currentHealth != 0)
                return;

            AsteroidPool owner = _owner;
            AsteroidKind destroyedKind = _kind;
            Vector3 destroyedPosition = transform.position;
            _isInUse = false;
            if (causedByPlayerProjectile)
                SfxAudioService.PlayAsteroidBreak();
            owner?.OnAsteroidDestroyed(destroyedKind, destroyedPosition, causedByPlayerProjectile,
                destroyedKind == AsteroidKind.Splitter ? FragmentSeparation : 0f, _difficultySpeedMultiplier);
            owner?.Release(this);
        }

        protected override void OnTakenFromPool()
        {
            _owner = null;
            _camera = null;
            _playerTarget = null;
            _isInUse = true;
            _size = AsteroidSize.Small;
            _kind = AsteroidKind.Small;
            _currentMaximumHealth = Mathf.Max(1, _maximumHealth);
            _currentHealth = _currentMaximumHealth;
            _movementDirection = Vector2.left;
            _movementSpeed = 0f;
            _difficultySpeedMultiplier = 0f;
            _variantElapsed = 0f;
            _fragmentDirectionDegrees = 0f;
            _hasEnteredCamera = false;
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_collider == null)
                _collider = GetComponent<Collider2D>();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
            _playerTarget = null;
            _currentMaximumHealth = Mathf.Max(1, _maximumHealth);
            _currentHealth = _currentMaximumHealth;
            _size = AsteroidSize.Small;
            _kind = AsteroidKind.Small;
            _movementDirection = Vector2.left;
            _movementSpeed = 0f;
            _difficultySpeedMultiplier = 0f;
            _variantElapsed = 0f;
            _fragmentDirectionDegrees = 0f;
            _hasEnteredCamera = false;
            if (_rigidbody != null)
            {
                _rigidbody.constraints = _originalConstraints;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!_isInUse)
                return;

            float step = Time.fixedDeltaTime;
            if (_kind == AsteroidKind.Homing)
                UpdateHomingMovement(step);
            else if (_kind == AsteroidKind.Erratic)
                UpdateErraticMovement(step);
            else if (_kind == AsteroidKind.Speed)
            {
                _rigidbody.linearVelocity = Vector2.left * _movementSpeed;
                _rigidbody.angularVelocity = 0f;
            }
            else if (_kind == AsteroidKind.Fragment)
            {
                _rigidbody.linearVelocity = _movementDirection * _movementSpeed;
                _rigidbody.angularVelocity = 0f;
            }

            if (_owner == null || _camera == null || _renderer == null || ShouldReturnToPool())
                ReturnToPool();
        }

        private void UpdateHomingMovement(float deltaTime)
        {
            if (_variantElapsed < _homingDurationSeconds && _playerTarget != null)
            {
                Vector2 toPlayer = (Vector2)_playerTarget.position - _rigidbody.position;
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    float currentAngle = Mathf.Atan2(_movementDirection.y, _movementDirection.x) * Mathf.Rad2Deg;
                    float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                    float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle,
                        Mathf.Max(0f, _homingTurnRateDegreesPerSecond) * deltaTime);
                    _movementDirection = DirectionFromDegrees(nextAngle);
                }
            }

            _variantElapsed += deltaTime;
            _rigidbody.linearVelocity = _movementDirection * _movementSpeed;
            _rigidbody.angularVelocity = 0f;
        }

        private void UpdateErraticMovement(float deltaTime)
        {
            _variantElapsed += deltaTime;
            float frequency = Mathf.Max(0f, _erraticWaveFrequency);
            float phase = Mathf.Repeat(_variantElapsed * frequency, 1f);
            float verticalSpeed;
            if (_erraticMovementPattern == ErraticMovementPattern.SmoothZigzag)
            {
                float segmentProgress = phase < 0.5f ? phase * 2f : (phase - 0.5f) * 2f;
                float derivative = 24f * segmentProgress * (1f - segmentProgress);
                if (phase >= 0.5f)
                    derivative = -derivative;
                verticalSpeed = _erraticWaveAmplitude * frequency * derivative;
            }
            else
            {
                verticalSpeed = _erraticWaveAmplitude * frequency * (2f * Mathf.PI)
                    * Mathf.Cos(phase * (2f * Mathf.PI));
            }

            _rigidbody.linearVelocity = new Vector2(-_movementSpeed, verticalSpeed);
            _rigidbody.angularVelocity = 0f;
        }

        private bool ShouldReturnToPool()
        {
            Bounds bounds = _renderer.bounds;
            Vector3 cameraPosition = _camera.transform.position;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float left = cameraPosition.x - halfWidth;
            float right = cameraPosition.x + halfWidth;
            float bottom = cameraPosition.y - halfHeight;
            float top = cameraPosition.y + halfHeight;

            bool special = IsSpecial(_kind) || _kind == AsteroidKind.Fragment;
            if (!special)
                return bounds.max.x < left;

            if (bounds.max.x >= left && bounds.min.x <= right && bounds.max.y >= bottom && bounds.min.y <= top)
                _hasEnteredCamera = true;

            return _hasEnteredCamera && (bounds.max.x < left || bounds.min.x > right
                || bounds.max.y < bottom || bounds.min.y > top);
        }

        private void ReturnToPool()
        {
            if (!_isInUse || _owner == null)
                return;

            _owner.Release(this);
        }

        private static bool IsSpecial(AsteroidKind kind)
        {
            return kind == AsteroidKind.Speed || kind == AsteroidKind.Homing
                || kind == AsteroidKind.Erratic || kind == AsteroidKind.Splitter;
        }

        private static Vector2 DirectionFromDegrees(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }
    }
}
