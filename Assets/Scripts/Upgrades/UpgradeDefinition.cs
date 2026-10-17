using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public abstract class UpgradeDefinition : ScriptableObject, IUpgradeOption
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        public string Id => _id;
        public string DisplayName => _displayName;

        // Concrete definitions must explicitly declare an implemented effect.
        public abstract bool HasImplementedEffect { get; }
        public abstract bool IsEligible(GameObject player);
        public abstract void Apply(GameObject player);
    }
}
