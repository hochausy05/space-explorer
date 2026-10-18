using System;
using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public enum EnergyShieldState
    {
        Unavailable,
        Ready,
        Cooldown
    }

    public sealed class EnergyShieldController : MonoBehaviour
    {
        public const int MaximumLevel = 3;

        [Header("Cooldown by Upgrade Level")]
        [SerializeField, Min(0.01f)] private float _levelOneCooldown = 12f;
        [SerializeField, Min(0.01f)] private float _levelTwoCooldown = 9f;
        [SerializeField, Min(0.01f)] private float _levelThreeCooldown = 6f;

        private int _currentLevel;
        private float _cooldownEndsAt;

        public int CurrentLevel => _currentLevel;
        public bool CanUpgrade => isActiveAndEnabled && _currentLevel < MaximumLevel;
        public event Action StateChanged;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownEndsAt - Time.time);
        public EnergyShieldState State => _currentLevel == 0
            ? EnergyShieldState.Unavailable
            : CooldownRemaining > 0f ? EnergyShieldState.Cooldown : EnergyShieldState.Ready;

        private void Awake()
        {
            GameManager.RunStarted += ResetForNewRun;
        }

        private void OnDestroy()
        {
            GameManager.RunStarted -= ResetForNewRun;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
                return false;

            _currentLevel++;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Consumes one ready shield charge; the current cycle duration is fixed when the hit is absorbed.</summary>
        public bool TryAbsorbAsteroidCollision()
        {
            if (State != EnergyShieldState.Ready)
                return false;

            _cooldownEndsAt = Time.time + GetCooldownForLevel(_currentLevel);
            StateChanged?.Invoke();
            return true;
        }

        private float GetCooldownForLevel(int level)
        {
            float configuredCooldown = level switch
            {
                1 => _levelOneCooldown,
                2 => _levelTwoCooldown,
                _ => _levelThreeCooldown
            };

            return Mathf.Max(0.01f, configuredCooldown);
        }

        private void ResetForNewRun()
        {
            _currentLevel = 0;
            _cooldownEndsAt = 0f;
            StateChanged?.Invoke();
        }
    }
}
