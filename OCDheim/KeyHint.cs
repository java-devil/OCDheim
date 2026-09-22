using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OCDheim
{
    public class KeyHint
    {
        private const string KeyPath = "key_bkg/Key";
        private const string LabelPath = "Text";

        private string label { get; }

        private Func<string> keyResolver { get; }
        private Func<string> joyResolver { get; }

        private GameObject keyHint { get; }
        private GameObject joyHint { get; }
        private TMP_Text keyHintText { get; }
        private TMP_Text joyHintText { get; }

        private bool isVisible { get; set; }
        private bool isJoyActive { get; set; }

        public KeyHint(string label, Transform keyTemplate, Transform joyTemplate, Func<string> keyResolver, Func<string> joyResolver)
        {
            this.label = label;
            this.keyResolver = keyResolver;
            this.joyResolver = joyResolver;
            keyHint = Clone(keyTemplate, label);
            joyHint = Clone(joyTemplate, label);

            keyHint.transform.Find(LabelPath).GetComponent<TMP_Text>().text = label;
            keyHintText = keyHint.transform.Find(KeyPath)?.GetComponent<TMP_Text>();
            joyHintText = joyHint.GetComponent<TMP_Text>();
        }

        public KeyHint Before(Transform keyNeighbour, Transform joyNeighbour) => MoveNextTo(keyNeighbour, joyNeighbour, 0);
        public KeyHint After(Transform keyNeighbour, Transform joyNeighbour) => MoveNextTo(keyNeighbour, joyNeighbour, 1);

        public void Refresh(bool shouldBeVisible)
        {
            var shouldJoyBeActive = ZInput.IsGamepadActive();
            if (shouldBeVisible == isVisible && shouldJoyBeActive == isJoyActive) { return; }
            isJoyActive = shouldJoyBeActive;
            isVisible = shouldBeVisible;

            if (!shouldBeVisible)
            {
                keyHint.SetActive(false);
                joyHint.SetActive(false);
                return;
            }

            var keyText = keyResolver?.Invoke(); // null: the key is baked into keyTemplate → show it
            var joyText = joyResolver();
            if (keyHintText) { keyHintText.text = keyText ?? ""; }
            joyHintText.text = $"{label} <mspace=0.6em> {joyText}</mspace>";

            // An unbound (None) key resolves to "" → hide it
            keyHint.SetActive(keyText != "");
            joyHint.SetActive(joyText != "");
        }

        private KeyHint MoveNextTo(Transform keyNeighbour, Transform joyNeighbour, int offset)
        {
            keyHint.transform.SetSiblingIndex(keyNeighbour.GetSiblingIndex() + offset);
            joyHint.transform.SetSiblingIndex(joyNeighbour.GetSiblingIndex() + offset);
            return this;
        }

        private static GameObject Clone(Transform template, string label)
        {
            var clone = Object.Instantiate(template.gameObject, template.parent);
            clone.name = $"OCDheim {label}";
            clone.SetActive(false);

            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = "";
            }

            return clone;
        }
    }
}
