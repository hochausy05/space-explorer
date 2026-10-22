using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    [CreateAssetMenu(fileName = "DroneWingUpgrade", menuName = "Space Explorer/Upgrades/Drone Wing")]
    public sealed class DroneUpgradeDefinition : UpgradeDefinition
    {
        public override bool HasImplementedEffect => true;

        // Chỉ cho chọn Drone Wing khi Player còn chỗ nhận drone.
        public override bool IsEligible(GameObject player)
        {
            return player != null
                && player.TryGetComponent(out DroneWingController droneWing)
                && droneWing.CanAddDrone;
        }

        // Yêu cầu bộ điều khiển Player thêm một drone nếu tham chiếu hợp lệ.
        public override void Apply(GameObject player)
        {
            if (player != null && player.TryGetComponent(out DroneWingController droneWing))
                droneWing.TryAddDrone();
        }
    }
}
