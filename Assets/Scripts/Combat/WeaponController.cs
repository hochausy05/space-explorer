using System;
using BigInteger = System.Numerics.BigInteger;
using SpaceExplorer.Audio;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceExplorer.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        private const int BaseProjectileCount = 1;
        private const int MaximumProjectileCount = 5;
        private const int BaseProjectileDamage = 1;
        private const float RapidFireReduction = 0.04f;
        private const float MinimumFireInterval = 0.14f;
        private const float RapidFireComparisonTolerance = 0.0001f;

        [Header("Weapon")]
        [SerializeField, Min(0.05f)] private float _fireInterval = 0.35f;
        [SerializeField, Min(0.01f)] private float _baseFireIntervalMultiplier = 1.1f;
        [SerializeField, Min(0f)] private float _muzzleOffset = 0.65f;
        [SerializeField, Min(0f)] private float _multiShotAngleStepDegrees = 15f;

        [Header("References")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private BulletPool _bulletPool;

        private InputAction _fireAction;
        private double _nextFireTime;
        private int _projectilesPerVolley = BaseProjectileCount;
        private BigInteger _damagePerProjectile = BaseProjectileDamage;
        private int _rapidFireUpgradeCount;
        private bool _runStartedSubscribed;

        public event Action StatsChanged;

        public int ProjectilesPerVolley => _projectilesPerVolley;
        public BigInteger DamagePerProjectile => _damagePerProjectile;
        public BigInteger DamageUpgradeLevel => _damagePerProjectile - BaseProjectileDamage;
        public float BaseFireInterval => _fireInterval * _baseFireIntervalMultiplier;
        public BulletPool SharedBulletPool => _bulletPool;
        public bool IsFireHeld => _fireAction != null && _fireAction.IsPressed();
        public float EffectiveFireInterval => _rapidFireUpgradeCount == 0
            ? BaseFireInterval
            : Mathf.Max(BaseFireInterval - RapidFireReduction * _rapidFireUpgradeCount, MinimumFireInterval);
        public bool CanUpgradeMultiShot => _projectilesPerVolley < MaximumProjectileCount;
        public bool CanUpgradeRapidFire
        {
            get
            {
                float currentInterval = EffectiveFireInterval;
                float nextInterval = Mathf.Max(
                    BaseFireInterval - RapidFireReduction * (_rapidFireUpgradeCount + 1),
                    MinimumFireInterval);
                return nextInterval < currentInterval - RapidFireComparisonTolerance;
            }
        }
        public bool CanUpgradeDamage => true;

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
            if (!_runStartedSubscribed)
            {
                GameManager.RunStarted += ResetRunUpgrades;
                _runStartedSubscribed = true;
            }

            _fireAction?.Enable();
        }

        private void OnDisable()
        {
            _fireAction?.Disable();
        }

        private void OnDestroy()
        {
            if (_runStartedSubscribed)
            {
                GameManager.RunStarted -= ResetRunUpgrades;
                _runStartedSubscribed = false;
            }
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != RunState.Playing
                || _fireAction == null || _bulletPool == null || !_bulletPool.IsReady || !_fireAction.IsPressed())
                return;

            double now = Time.timeAsDouble;
            if (now < _nextFireTime)
                return;

            _nextFireTime = now + EffectiveFireInterval;
            Vector3 muzzlePosition = transform.position + Vector3.right * _muzzleOffset;
            float centerIndex = (_projectilesPerVolley - 1) * 0.5f;
            bool spawnedAnyProjectile = false;
            for (int i = 0; i < _projectilesPerVolley; i++)
            {
                float angle = (i - centerIndex) * _multiShotAngleStepDegrees;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                spawnedAnyProjectile |= _bulletPool.TrySpawn(muzzlePosition, direction, _damagePerProjectile);
            }

            if (spawnedAnyProjectile)
                SfxAudioService.PlayShoot();
        }

        public bool TryApplyMultiShot()
        {
            if (!CanUpgradeMultiShot)
                return false;

            _projectilesPerVolley++;
            StatsChanged?.Invoke();
            return true;
        }

        public bool TryApplyRapidFire()
        {
            if (!CanUpgradeRapidFire)
                return false;

            _rapidFireUpgradeCount++;
            StatsChanged?.Invoke();
            return true;
        }

        public bool TryApplyDamageUp()
        {
            if (!CanUpgradeDamage)
                return false;

            _damagePerProjectile += BigInteger.One;
            StatsChanged?.Invoke();
            return true;
        }

        private void ResetRunUpgrades()
        {
            _projectilesPerVolley = BaseProjectileCount;
            _damagePerProjectile = BaseProjectileDamage;
            _rapidFireUpgradeCount = 0;
            _nextFireTime = 0d;
            StatsChanged?.Invoke();
        }

        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
