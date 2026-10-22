using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Combat
{
    public sealed class DroneController : MonoBehaviour
    {
        private WeaponController _weaponController;
        private float _muzzleOffset;
        private double _nextFireTime;
        private bool _initialized;

        // Gắn WeaponController dùng chung và chuẩn bị lịch bắn cho Drone.
        public void Initialize(WeaponController weaponController, float muzzleOffset)
        {
            if (weaponController == null || weaponController.SharedBulletPool == null)
            {
                enabled = false;
                return;
            }

            _weaponController = weaponController;
            _muzzleOffset = Mathf.Max(0f, muzzleOffset);
            _initialized = true;
            PrepareForActivation();
        }

        // Đặt lịch bắn mới mỗi khi Drone được kích hoạt lại.
        public void PrepareForActivation()
        {
            if (_initialized && _weaponController != null)
                _nextFireTime = Time.timeAsDouble + _weaponController.BaseFireInterval;
        }

        // Xóa lịch bắn cũ khi bắt đầu một run mới.
        public void ResetForNewRun()
        {
            _nextFireTime = 0d;
        }

        // Drone chỉ bắn khi run đang chơi và dùng pool, nhịp bắn cùng sát thương cơ bản của Player.
        private void Update()
        {
            if (!_initialized || _weaponController == null)
                return;

            GameManager gameManager = GameManager.Instance;
            BulletPool bulletPool = _weaponController.SharedBulletPool;
            if (gameManager == null || gameManager.State != RunState.Playing
                || bulletPool == null || !bulletPool.IsReady)
                return;

            double now = Time.timeAsDouble;
            if (now < _nextFireTime)
                return;

            _nextFireTime = now + _weaponController.BaseFireInterval;
            Vector3 muzzlePosition = transform.position + Vector3.right * _muzzleOffset;
            bulletPool.TrySpawn(muzzlePosition, Vector2.right, _weaponController.DamagePerProjectile);
        }
    }
}
