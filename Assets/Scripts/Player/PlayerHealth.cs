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
        // Giữ tên MaxHP tương thích với các thành phần HUD đang dùng.
        public int MaxHP => MaximumHP;

        public event Action<int, int> HealthChanged;

        // Khởi tạo HP và đăng ký reset máu khi một run mới bắt đầu.
        private void Awake()
        {
            _currentHP = MaximumHP;
            GameManager.RunStarted += ResetForNewRun;
        }

        // Hủy đăng ký sự kiện để GameManager không giữ callback cũ.
        private void OnDestroy()
        {
            GameManager.RunStarted -= ResetForNewRun;
        }

        // Đưa máu về mức tối đa đầu run và báo cho HUD cập nhật ngay.
        public void ResetForNewRun()
        {
            _currentHP = MaximumHP;
            HealthChanged?.Invoke(_currentHP, MaximumHP);
        }

        // Trừ HP trong giới hạn hợp lệ, phát sự kiện và báo đúng khi máu về 0.
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
