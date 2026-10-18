using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public abstract class UpgradeDefinition : ScriptableObject, IUpgradeOption
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        // Concrete definitions must explicitly declare an implemented effect.
        public abstract bool HasImplementedEffect { get; }
        public abstract bool IsEligible(GameObject player);
        public abstract void Apply(GameObject player);
    }
}
