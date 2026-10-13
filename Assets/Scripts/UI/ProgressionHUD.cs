using SpaceExplorer.Pickups;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class ProgressionHUD : MonoBehaviour
    {
        [Header("Progression Source")]
        [SerializeField] private StarXPCollector _collector;

        [Header("HUD Elements")]
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _xpLabel;
        [SerializeField] private Image _progressFill;

        private void OnEnable()
        {
            if (_collector == null || _levelLabel == null || _xpLabel == null || _progressFill == null)
            {
                Debug.LogError("ProgressionHUD requires a StarXPCollector, Level label, XP label, and progress fill Image.", this);
                enabled = false;
                return;
            }

            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            _collector.ProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_collector != null)
                _collector.ProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_collector == null)
                return;

            int requiredXP = Mathf.Max(1, _collector.XPRequiredForNextLevel);
            _levelLabel.text = $"Level {_collector.CurrentLevel}";
            _xpLabel.text = $"{_collector.XPIntoCurrentLevel} / {requiredXP} XP";
            _progressFill.fillAmount = Mathf.Clamp01((float)_collector.XPIntoCurrentLevel / requiredXP);
        }
    }
}
