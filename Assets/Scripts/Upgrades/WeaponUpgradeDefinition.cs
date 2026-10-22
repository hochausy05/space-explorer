using SpaceExplorer.Combat;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public enum WeaponUpgradeType
    {
        MultiShot,
        RapidFire,
        DamageUp
    }

    [CreateAssetMenu(fileName = "WeaponUpgrade", menuName = "Space Explorer/Upgrades/Weapon Upgrade")]
    public sealed class WeaponUpgradeDefinition : UpgradeDefinition
    {
        [SerializeField] private WeaponUpgradeType _upgradeType;

        public override bool HasImplementedEffect => _upgradeType == WeaponUpgradeType.MultiShot
            || _upgradeType == WeaponUpgradeType.RapidFire
            || _upgradeType == WeaponUpgradeType.DamageUp;

        // Kiểm tra loại nâng cấp và giới hạn tương ứng trên WeaponController.
        public override bool IsEligible(GameObject player)
        {
            if (!HasImplementedEffect || player == null || !player.TryGetComponent(out WeaponController weapon))
                return false;

            return _upgradeType switch
            {
                WeaponUpgradeType.MultiShot => weapon.CanUpgradeMultiShot,
                WeaponUpgradeType.RapidFire => weapon.CanUpgradeRapidFire,
                WeaponUpgradeType.DamageUp => weapon.CanUpgradeDamage,
                _ => false
            };
        }

        // Gọi đúng API của vũ khí để áp dụng hiệu ứng đã chọn.
        public override void Apply(GameObject player)
        {
            if (!HasImplementedEffect || player == null || !player.TryGetComponent(out WeaponController weapon))
                return;

            switch (_upgradeType)
            {
                case WeaponUpgradeType.MultiShot:
                    weapon.TryApplyMultiShot();
                    break;
                case WeaponUpgradeType.RapidFire:
                    weapon.TryApplyRapidFire();
                    break;
                case WeaponUpgradeType.DamageUp:
                    weapon.TryApplyDamageUp();
                    break;
            }
        }
    }
}
