using System;
using SpaceExplorer.Combat;
using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    [RequireComponent(typeof(WeaponController), typeof(SpriteRenderer))]
    public sealed class DroneWingController : MonoBehaviour
    {
        public const int MaximumDroneCount = 2;

        [Header("Formation")]
        [SerializeField] private Vector2 _firstDroneOffset = new Vector2(-0.55f, 0.75f);
        [SerializeField] private Vector2 _secondDroneOffset = new Vector2(-0.55f, -0.75f);
        [SerializeField, Min(0.01f)] private float _droneScale = 0.45f;
        [SerializeField, Min(0f)] private float _muzzleForwardOffset = 0.4f;
        [SerializeField] private Color _droneTint = new Color(0.45f, 0.9f, 1f, 1f);
        [SerializeField] private Sprite _droneSprite;

        private readonly DroneController[] _drones = new DroneController[MaximumDroneCount];
        private WeaponController _weaponController;
        private SpriteRenderer _sourceRenderer;
        private int _droneCount;
        private bool _runStartedSubscribed;

        public int DroneCount => _droneCount;
        // Báo liệu còn có thể thêm drone trong giới hạn đã định hay không.
        public bool CanAddDrone => isActiveAndEnabled && _droneCount < MaximumDroneCount;
        public event Action StatsChanged;

        // Lấy vũ khí và sprite nguồn, đồng thời kiểm tra pool đạn dùng chung.
        private void Awake()
        {
            _weaponController = GetComponent<WeaponController>();
            _sourceRenderer = GetComponent<SpriteRenderer>();
            if (_weaponController == null || _sourceRenderer == null
                || (_droneSprite == null && _sourceRenderer.sprite == null)
                || _weaponController.SharedBulletPool == null)
                FailConfiguration("DroneWingController needs a Drone Sprite or Player sprite fallback, a Player SpriteRenderer, and a WeaponController with its shared BulletPool assigned.");
        }

        // Đăng ký reset số drone khi bắt đầu run mới.
        private void OnEnable()
        {
            if (_runStartedSubscribed)
                return;

            GameManager.RunStarted += ResetForNewRun;
            _runStartedSubscribed = true;
        }

        // Hủy đăng ký reset run trước khi controller bị hủy.
        private void OnDestroy()
        {
            if (!_runStartedSubscribed)
                return;

            GameManager.RunStarted -= ResetForNewRun;
            _runStartedSubscribed = false;
        }

        // Mở một trong hai drone đã tạo hoặc tạo mới; cả hai dùng WeaponController và BulletPool chung.
        public bool TryAddDrone()
        {
            if (!isActiveAndEnabled || !CanAddDrone || _weaponController == null || _sourceRenderer == null)
                return false;

            int droneIndex = _droneCount;
            DroneController drone = _drones[droneIndex];
            if (drone == null)
            {
                drone = CreateDrone(droneIndex);
                if (drone == null)
                    return false;

                _drones[droneIndex] = drone;
            }

            drone.PrepareForActivation();
            drone.gameObject.SetActive(true);
            _droneCount++;
            StatsChanged?.Invoke();
            return true;
        }

        // Tạo drone con với sprite, vị trí và WeaponController dùng chung.
        private DroneController CreateDrone(int index)
        {
            var droneObject = new GameObject($"Drone {index + 1}");
            droneObject.SetActive(false);
            Transform droneTransform = droneObject.transform;
            droneTransform.SetParent(transform, false);
            droneTransform.localPosition = index == 0 ? _firstDroneOffset : _secondDroneOffset;
            droneTransform.localRotation = Quaternion.identity;
            droneTransform.localScale = Vector3.one * _droneScale;

            SpriteRenderer droneRenderer = droneObject.AddComponent<SpriteRenderer>();
            droneRenderer.sprite = _droneSprite != null ? _droneSprite : _sourceRenderer.sprite;
            droneRenderer.flipX = false;
            droneRenderer.flipY = false;
            droneRenderer.color = _droneTint;
            droneRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
            droneRenderer.sortingOrder = _sourceRenderer.sortingOrder + 1;

            DroneController drone = droneObject.AddComponent<DroneController>();
            drone.Initialize(_weaponController, _muzzleForwardOffset);
            return drone.enabled ? drone : null;
        }

        // Đưa số drone về 0 và tắt các drone đã tạo để tái sử dụng.
        private void ResetForNewRun()
        {
            _droneCount = 0;
            for (int i = 0; i < _drones.Length; i++)
            {
                if (_drones[i] == null)
                    continue;

                _drones[i].ResetForNewRun();
                _drones[i].gameObject.SetActive(false);
            }

            StatsChanged?.Invoke();
        }

        // Báo cấu hình lỗi rồi tắt controller để tránh hoạt động thiếu tham chiếu.
        private void FailConfiguration(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}
