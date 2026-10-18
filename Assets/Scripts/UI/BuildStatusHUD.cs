using System;
using System.Globalization;
using SpaceExplorer.Core;
using SpaceExplorer.Combat;
using SpaceExplorer.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    /// <summary>Displays current Player build values without owning or changing gameplay state.</summary>
    public sealed class BuildStatusHUD : MonoBehaviour
    {
        private const float ShieldRefreshInterval = 0.1f;

        [Header("Player Source")]
        [SerializeField] private GameObject _player;

        [Header("Stat Values")]
        [SerializeField] private Text _droneValue;
        [SerializeField] private Text _shieldValue;
        [SerializeField] private Text _damageValue;
        [SerializeField] private Text _fireRateValue;
        [SerializeField] private Text _multiShotValue;

        private DroneWingController _droneWing;
        private EnergyShieldController _energyShield;
        private WeaponController _weapon;
        private float _nextShieldRefresh;
        private EnergyShieldState _lastShieldState;
        private bool _configurationErrorReported;

        private void OnEnable()
        {
            if (!ResolveReferences())
            {
                ReportConfigurationError();
                enabled = false;
                return;
            }

            _droneWing.StatsChanged += RefreshDrone;
            _energyShield.StateChanged += RefreshShield;
            _weapon.StatsChanged += RefreshWeaponStats;
            GameManager.RunStarted += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_droneWing != null)
                _droneWing.StatsChanged -= RefreshDrone;
            if (_energyShield != null)
                _energyShield.StateChanged -= RefreshShield;
            if (_weapon != null)
                _weapon.StatsChanged -= RefreshWeaponStats;
            GameManager.RunStarted -= RefreshAll;
        }

        private void Update()
        {
            if (_energyShield == null || Time.unscaledTime < _nextShieldRefresh)
                return;

            EnergyShieldState currentState = _energyShield.State;
            if (currentState != EnergyShieldState.Cooldown && currentState == _lastShieldState)
                return;

            _nextShieldRefresh = Time.unscaledTime + ShieldRefreshInterval;
            RefreshShield();
        }

        private bool ResolveReferences()
        {
            if (_player == null || _droneValue == null || _shieldValue == null || _damageValue == null
                || _fireRateValue == null || _multiShotValue == null)
                return false;

            _player.TryGetComponent(out _droneWing);
            _player.TryGetComponent(out _energyShield);
            _player.TryGetComponent(out _weapon);
            return _droneWing != null && _energyShield != null && _weapon != null;
        }

        private void RefreshAll()
        {
            RefreshDrone();
            RefreshShield();
            RefreshWeaponStats();
        }

        private void RefreshDrone()
        {
            if (_droneWing != null && _droneValue != null)
                _droneValue.text = $"{_droneWing.DroneCount}/{DroneWingController.MaximumDroneCount}";
        }

        private void RefreshShield()
        {
            if (_energyShield == null || _shieldValue == null)
                return;

            switch (_energyShield.State)
            {
                case EnergyShieldState.Ready:
                    _shieldValue.text = "READY";
                    break;
                case EnergyShieldState.Cooldown:
                    _shieldValue.text = _energyShield.CooldownRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s";
                    break;
                default:
                    _shieldValue.text = "—";
                    break;
            }

            _lastShieldState = _energyShield.State;
        }

        private void RefreshWeaponStats()
        {
            if (_weapon == null)
                return;

            if (_damageValue != null)
                _damageValue.text = _weapon.DamagePerProjectile.ToString(CultureInfo.InvariantCulture);
            if (_fireRateValue != null)
                _fireRateValue.text = _weapon.EffectiveFireInterval.ToString("0.00", CultureInfo.InvariantCulture) + "s";
            if (_multiShotValue != null)
                _multiShotValue.text = _weapon.ProjectilesPerVolley.ToString(CultureInfo.InvariantCulture);
        }

        private void ReportConfigurationError()
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError("BuildStatusHUD requires the Player GameObject and all five Legacy UI Text value references; the Player must have DroneWingController, EnergyShieldController, and WeaponController.", this);
            _configurationErrorReported = true;
        }
    }
}
