using BepInEx.Configuration;
using UnityEngine;

namespace PhontyPlus {
    public class GameClickableText : MonoBehaviour {
        public StandardMenuButton button;
        public ConfigEntry<float> floatConfig;
        public ConfigEntry<int> intConfig;

        public float min;
        public float max;

        public string prefix;
        public string suffix = "";
        public bool isInt;

        private bool isEditing = false;
        private bool justClicked = false;
        private string editBuffer = "";
        private int caretPos = 0;
        private float blinkTimer = 0f;
        private Color originalColor;

        public void Init(ConfigEntry<float> config, float min, float max, string prefix, string suffix = "") {
            floatConfig = config;
            this.min = min;
            this.max = max;
            this.prefix = prefix;
            this.suffix = suffix;
            isInt = false;
            originalColor = button.text.color;
            UpdateDisplay();
        }

        public void Init(ConfigEntry<int> config, int min, int max, string prefix, string suffix = "") {
            intConfig = config;
            this.min = min;
            this.max = max;
            this.prefix = prefix;
            this.suffix = suffix;
            isInt = true;
            originalColor = button.text.color;
            UpdateDisplay();
        }

        public void OnClick() {
            if (isEditing) return;

            isEditing = true;
            justClicked = true;

            if (isInt)
                editBuffer = intConfig.Value.ToString();
            else
                editBuffer = floatConfig.Value.ToString();

            caretPos = editBuffer.Length;
            button.text.color = Color.red;
        }

        private void Update() {
            if (!isEditing) return;

            if (justClicked) {
                justClicked = false;
                return;
            }

            foreach (char c in Input.inputString) {
                if (c == '\b') {
                    if (caretPos > 0 && editBuffer.Length > 0) {
                        editBuffer = editBuffer.Remove(caretPos - 1, 1);
                        caretPos--;
                    }
                }
                else if (c == '\n' || c == '\r') {
                    StopEditing();
                    return;
                }
                else {
                    bool isSeparator = (c == '.' || c == ',');
                    if (char.IsDigit(c) || (!isInt && isSeparator && !editBuffer.Contains(".") && !editBuffer.Contains(","))) {
                        editBuffer = editBuffer.Insert(caretPos, c.ToString());
                        caretPos++;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                caretPos = Mathf.Max(0, caretPos - 1);
                blinkTimer = 0f;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                caretPos = Mathf.Min(editBuffer.Length, caretPos + 1);
                blinkTimer = 0f;
            }

            if (Singleton<InputManager>.Instance.GetDigitalInput("Interact", onDown: true)) {
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, Input.mousePosition)) {
                    StopEditing();
                    return;
                }
            }

            UpdateDisplayEditing();
        }

        private void StopEditing() {
            isEditing = false;
            button.text.color = originalColor;
            ApplyValue();
            UpdateDisplay();
            Mod.Instance.OverrideConfig();
        }

        private void ApplyValue() {
            if (string.IsNullOrEmpty(editBuffer)) editBuffer = min.ToString();

            if (isInt) {
                if (int.TryParse(editBuffer, out int result)) {
                    result = Mathf.Clamp(result, (int)min, (int)max);
                    intConfig.Value = result;
                }
            }
            else {
                string parseBuffer = editBuffer.Replace(',', '.');
                if (float.TryParse(parseBuffer, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result)) {
                    result = Mathf.Clamp(result, min, max);
                    floatConfig.Value = result;
                }
            }
        }

        private void UpdateDisplay() {
            if (isInt)
                button.text.text = prefix + intConfig.Value + suffix;
            else
                button.text.text = prefix + floatConfig.Value.ToString("0.##") + suffix;
        }

        private void UpdateDisplayEditing() {
            blinkTimer += Time.unscaledDeltaTime;
            string cursorChar = (blinkTimer % 1f < 0.5f) ? "|" : " ";

            string visualText = editBuffer;
            if (caretPos >= 0 && caretPos <= visualText.Length) {
                visualText = visualText.Insert(caretPos, cursorChar);
            }
            else {
                visualText += cursorChar;
            }

            button.text.text = prefix + visualText + suffix;
        }
    }
}