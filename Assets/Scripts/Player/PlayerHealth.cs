using System;
using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        private const int MaximumHitPoints = 10;

        private int _currentHP;

        public int CurrentHP => _currentHP;
        public int MaximumHP => MaximumHitPoints;
        public int MaxHP => MaximumHP;

        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            _currentHP = MaximumHP;
            GameManager.RunStarted += ResetForNewRun;
        }

        private void OnDestroy()
        {
            GameManager.RunStarted -= ResetForNewRun;
        }

        public void ResetForNewRun()
        {
            _currentHP = MaximumHP;
            HealthChanged?.Invoke(_currentHP, MaximumHP);
        }

        /// <returns>True when this damage reduced Player HP to zero.</returns>
        public bool ApplyDamage(int amount)
        {
            if (amount <= 0 || _currentHP <= 0)
                return _currentHP <= 0;

            int nextHP = Mathf.Max(0, _currentHP - amount);
            if (nextHP == _currentHP)
                return false;

            _currentHP = nextHP;
            HealthChanged?.Invoke(_currentHP, MaximumHP);
            return _currentHP == 0;
        }
    }
}
