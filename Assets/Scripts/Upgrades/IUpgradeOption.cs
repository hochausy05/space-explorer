using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public interface IUpgradeOption
    {
        // Mã duy nhất dùng để nhận diện và lọc lựa chọn nâng cấp.
        string Id { get; }
        // Tên hiển thị trên thẻ lựa chọn trong giao diện.
        string DisplayName { get; }
        // Cho biết nâng cấp đã có hiệu ứng chạy trong gameplay hay chưa.
        bool HasImplementedEffect { get; }
        // Kiểm tra nâng cấp có thể nhận với trạng thái Player hiện tại không.
        bool IsEligible(GameObject player);
        // Áp dụng hiệu ứng nâng cấp lên Player đã chọn.
        void Apply(GameObject player);
    }
}
