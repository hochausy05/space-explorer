using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    [CreateAssetMenu(fileName = "EnergyShieldUpgrade", menuName = "Space Explorer/Upgrades/Energy Shield")]
    public sealed class EnergyShieldUpgradeDefinition : UpgradeDefinition
    {
        public override bool HasImplementedEffect => true;

        public override bool IsEligible(GameObject player)
        {
            return player != null
                && player.TryGetComponent(out EnergyShieldController shield)
                && shield.CanUpgrade;
        }

        public override void Apply(GameObject player)
        {
            if (player != null && player.TryGetComponent(out EnergyShieldController shield))
                shield.TryUpgrade();
        }
    }
}
