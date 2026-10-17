using System;
using System.Collections.Generic;
using SpaceExplorer.Upgrades;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class UpgradeSelectionPanel : MonoBehaviour
    {
        [Header("Choice Buttons")]
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Text[] _labels;

        public bool IsConfigured
        {
            get
            {
                if (!enabled || _buttons == null || _labels == null || _buttons.Length == 0 || _buttons.Length != _labels.Length)
                    return false;

                for (int i = 0; i < _buttons.Length; i++)
                {
                    if (_buttons[i] == null || _labels[i] == null)
                        return false;
                }

                return true;
            }
        }

        private Action<int, int> _onChoiceSelected;
        private UnityAction[] _runtimeCallbacks;

        private void Awake()
        {
            if (_onChoiceSelected == null)
                Hide();
        }

        public bool Show(IReadOnlyList<IUpgradeOption> choices, int selectionToken, Action<int, int> onChoiceSelected)
        {
            if (choices == null || choices.Count == 0 || onChoiceSelected == null || !IsConfigured)
                return false;

            int choiceCount = Mathf.Min(choices.Count, Mathf.Min(_buttons.Length, UpgradeManager.MaximumChoices));
            if (choiceCount == 0)
                return false;

            for (int i = 0; i < choiceCount; i++)
            {
                if (!_buttons[i].enabled || !_buttons[i].interactable || !_labels[i].enabled)
                    return false;
            }

            // Set the callback before activation: an initially inactive panel runs Awake on activation.
            _onChoiceSelected = onChoiceSelected;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            RemoveRuntimeListeners();

            for (int i = 0; i < _buttons.Length; i++)
            {
                bool isVisible = i < choiceCount;
                _buttons[i].gameObject.SetActive(isVisible);
                if (!isVisible)
                    continue;

                _labels[i].text = choices[i].DisplayName;
                int capturedIndex = i;
                int capturedToken = selectionToken;
                UnityAction callback = () => _onChoiceSelected?.Invoke(capturedIndex, capturedToken);
                if (_runtimeCallbacks == null || _runtimeCallbacks.Length != _buttons.Length)
                    _runtimeCallbacks = new UnityAction[_buttons.Length];
                _runtimeCallbacks[i] = callback;
                _buttons[i].onClick.AddListener(callback);
            }

            gameObject.SetActive(true);
            return true;
        }

        public void Hide()
        {
            RemoveRuntimeListeners();
            _onChoiceSelected = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void RemoveRuntimeListeners()
        {
            if (_buttons == null || _runtimeCallbacks == null)
                return;

            int count = Mathf.Min(_buttons.Length, _runtimeCallbacks.Length);
            for (int i = 0; i < count; i++)
            {
                if (_buttons[i] != null && _runtimeCallbacks[i] != null)
                    _buttons[i].onClick.RemoveListener(_runtimeCallbacks[i]);
                _runtimeCallbacks[i] = null;
            }
        }
    }
}
