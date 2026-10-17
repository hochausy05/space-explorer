using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    [CreateAssetMenu(fileName = "DroneWingUpgrade", menuName = "Space Explorer/Upgrades/Drone Wing")]
    public sealed class DroneUpgradeDefinition : UpgradeDefinition
    {
        public override bool HasImplementedEffect => true;

        public override bool IsEligible(GameObject player)
        {
            return player != null
                && player.TryGetComponent(out DroneWingController droneWing)
                && droneWing.CanAddDrone;
        }

        public override void Apply(GameObject player)
        {
            if (player != null && player.TryGetComponent(out DroneWingController droneWing))
                droneWing.TryAddDrone();
        }
    }
}
