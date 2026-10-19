using SpaceExplorer.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceExplorer.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _moveSpeed = 7f;
        [SerializeField, Min(0.01f)] private float _acceleration = 22f;
        [SerializeField, Min(0.01f)] private float _deceleration = 27f;
        [SerializeField, Range(0f, 1f)] private float _movementMultiplierWhileFiring = 0.9f;

        [Header("References")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Camera _camera;

        private Rigidbody2D _rigidbody;
        private Collider2D _playerCollider;
        private WeaponController _weaponController;
        private InputAction _moveAction;
        private Vector2 _moveInput;
        private Vector2 _velocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _playerCollider = GetComponent<Collider2D>();
            _weaponController = GetComponent<WeaponController>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;

            if (_inputActions == null)
            {
                FailConfiguration("Player movement needs the project's InputActionAsset assigned.");
                return;
            }

            _moveAction = _inputActions.FindAction("Player/Move", false);
            if (_moveAction == null)
            {
                FailConfiguration("InputActionAsset is missing the 'Player/Move' action.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
            {
                FailConfiguration("Player movement needs an assigned camera or an active MainCamera-tagged camera.");
                return;
            }
            if (!_camera.orthographic || Mathf.Abs(Mathf.DeltaAngle(0f, _camera.transform.eulerAngles.z)) > 0.01f)
            {
                FailConfiguration("Player movement bounds require an axis-aligned orthographic 2D camera.");
                return;
            }

            if (_playerCollider == null || !_playerCollider.enabled)
                FailConfiguration("Player movement needs the Player's enabled Collider2D to calculate movement bounds.");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _moveInput = Vector2.zero;
            _velocity = Vector2.zero;
        }

        private void Update()
        {
            if (_moveAction == null)
                return;

            _moveInput = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || _camera == null || _playerCollider == null || !_playerCollider.enabled)
            {
                FailConfiguration("A required player movement reference was lost at runtime.");
                return;
            }

            if (!TryGetMovementBounds(out Vector2 minimum, out Vector2 maximum))
                return;

            float deltaTime = Time.fixedDeltaTime;
            float movementSpeed = _moveSpeed;
            if (_weaponController != null && _weaponController.IsFireHeld)
                movementSpeed *= _movementMultiplierWhileFiring;
            Vector2 targetVelocity = _moveInput * movementSpeed;
            float rate = _moveInput.sqrMagnitude > 0f ? _acceleration : _deceleration;
            _velocity = Vector2.MoveTowards(_velocity, targetVelocity, rate * deltaTime);

            Vector2 desiredPosition = _rigidbody.position + _velocity * deltaTime;
            Vector2 boundedPosition = new Vector2(
                Mathf.Clamp(desiredPosition.x, minimum.x, maximum.x),
                Mathf.Clamp(desiredPosition.y, minimum.y, maximum.y));

            if (boundedPosition.x != desiredPosition.x)
                _velocity.x = 0f;
            if (boundedPosition.y != desiredPosition.y)
                _velocity.y = 0f;

            _rigidbody.MovePosition(boundedPosition);
        }

        private bool TryGetMovementBounds(out Vector2 minimum, out Vector2 maximum)
        {
            Bounds colliderBounds = _playerCollider.bounds;
            Vector2 colliderOffset = (Vector2)colliderBounds.center - _rigidbody.position;
            Vector3 extents = colliderBounds.extents;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector3 cameraPosition = _camera.transform.position;

            // Recalculate every physics step so camera or viewport size changes are reflected.
            float minimumX = cameraPosition.x - halfWidth + extents.x - colliderOffset.x;
            float maximumX = cameraPosition.x + halfWidth - extents.x - colliderOffset.x;
            float minimumY = cameraPosition.y - halfHeight + extents.y - colliderOffset.y;
            float maximumY = cameraPosition.y + halfHeight - extents.y - colliderOffset.y;

            // If the viewport is smaller than the collider, center the collider on that axis.
            if (minimumX > maximumX)
                minimumX = maximumX = cameraPosition.x - colliderOffset.x;
            if (minimumY > maximumY)
                minimumY = maximumY = cameraPosition.y - colliderOffset.y;

            minimum = new Vector2(minimumX, minimumY);
            maximum = new Vector2(maximumX, maximumY);
            return true;
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
