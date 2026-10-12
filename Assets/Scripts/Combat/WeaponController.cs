using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceExplorer.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField, Min(0.05f)] private float _fireInterval = 0.35f;
        [SerializeField, Min(0f)] private float _muzzleOffset = 0.65f;

        [Header("References")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private BulletPool _bulletPool;

        private InputAction _fireAction;
        private double _nextFireTime;

        private void Awake()
        {
            if (_inputActions == null)
            {
                FailConfiguration("WeaponController needs the project's InputActionAsset assigned.");
                return;
            }

            _fireAction = _inputActions.FindAction("Player/Attack", false);
            if (_fireAction == null)
            {
                FailConfiguration("InputActionAsset is missing the 'Player/Attack' action.");
                return;
            }

            if (_bulletPool == null)
                FailConfiguration("WeaponController needs a BulletPool assigned.");
        }

        private void OnEnable()
        {
            _fireAction?.Enable();
        }

        private void OnDisable()
        {
            _fireAction?.Disable();
        }

        private void Update()
        {
            if (_fireAction == null || _bulletPool == null || !_bulletPool.IsReady || !_fireAction.IsPressed())
                return;

            double now = Time.timeAsDouble;
            if (now < _nextFireTime)
                return;

            _nextFireTime = now + _fireInterval;
            Vector3 muzzlePosition = transform.position + Vector3.right * _muzzleOffset;
            _bulletPool.TrySpawn(muzzlePosition);
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
