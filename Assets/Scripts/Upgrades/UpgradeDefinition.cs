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

        // Mỗi lớp nâng cấp cụ thể phải xác nhận hiệu ứng đã được triển khai.
        public abstract bool HasImplementedEffect { get; }
        // Kiểm tra điều kiện nhận nâng cấp theo trạng thái Player.
        public abstract bool IsEligible(GameObject player);
        // Thực hiện hiệu ứng nâng cấp lên Player.
        public abstract void Apply(GameObject player);
    }
}
