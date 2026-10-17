using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public interface IUpgradeOption
    {
        string Id { get; }
        string DisplayName { get; }
        bool HasImplementedEffect { get; }
        bool IsEligible(GameObject player);
        void Apply(GameObject player);
    }
}
