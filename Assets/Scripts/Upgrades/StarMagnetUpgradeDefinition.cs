using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    [CreateAssetMenu(fileName = "StarMagnetUpgrade", menuName = "Space Explorer/Upgrades/Star Magnet")]
    public sealed class StarMagnetUpgradeDefinition : UpgradeDefinition
    {
        public override bool HasImplementedEffect => true;

        public override bool IsEligible(GameObject player)
        {
            return player != null
                && player.TryGetComponent(out StarMagnetController magnet)
                && magnet.CanUpgrade;
        }

        public override void Apply(GameObject player)
        {
            if (player != null && player.TryGetComponent(out StarMagnetController magnet))
                magnet.TryUpgrade();
        }
    }
}
