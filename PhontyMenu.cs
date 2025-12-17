using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.UI;
using TMPro;
using UnityEngine;

namespace PhontyPlus {
    public class PhontyMenu : CustomOptionsCategory {
        public static ConfigEntry<bool> nonLethalConfig;
        public static ConfigEntry<float> deafTimeConfig;
        public static ConfigEntry<int> timeLeftUntilMad;
        public static ConfigEntry<float> chaseSpeedConfig;
        public static ConfigEntry<bool> guaranteeSpawn;

        private MenuToggle nonlethalToggle;
        private MenuToggle guaranteeSpawnToggle;

        public override void Build() {
            nonlethalToggle = CreateToggle(
                "NonLethal",
                "Non-Lethal",
                nonLethalConfig.Value,
                new Vector2(60f, 30f),
                300f
            );
            AddTooltip(nonlethalToggle, "If enabled, Phonty will deafen the player instead of ending the game");
            nonlethalToggle.transform.SetParent(transform, false);

            var nonLethalButton = Traverse.Create(nonlethalToggle).Field<GameObject>("hotspot").Value.GetComponent<StandardMenuButton>();
            nonLethalButton.OnPress.AddListener(() => {
                nonLethalConfig.Value = nonlethalToggle.Value;
                Mod.Instance.OverrideConfig();
            });

            CreateClickableTextFloat(
                "DeafTimeBtn",
                "Deaf Time: ",
                deafTimeConfig,
                1f, 3600f,
                "s",
                new Vector2(0f, -10f),
                "Click to type duration (1s - 3600s)."
            );

            CreateClickableTextInt(
                "WindUpTimeBtn",
                "Wind Up: ",
                timeLeftUntilMad,
                10, 600,
                "s",
                new Vector2(0f, -50f),
                "Click to type wind up time (10s - 600s)."
            );

            CreateClickableTextFloat(
                "ChaseSpeedBtn",
                "Speed: ",
                chaseSpeedConfig,
                5f, 100f,
                "",
                new Vector2(0f, -90f),
                "Click to type chase speed (5 - 100)."
            );

            guaranteeSpawnToggle = CreateToggle(
                "GuaranteeSpawn",
                "Guarantee Spawn",
                guaranteeSpawn.Value,
                new Vector2(90f, -130f),
                300f
            );
            AddTooltip(guaranteeSpawnToggle, "If enabled, Phonty will be guaranteed to always spawn. <color=red>(requires game reload)</color>");
            guaranteeSpawnToggle.transform.SetParent(transform, false);

            var guaranteeSpawnButton = Traverse.Create(guaranteeSpawnToggle).Field<GameObject>("hotspot").Value.GetComponent<StandardMenuButton>();
            guaranteeSpawnButton.OnPress.AddListener(() => {
                guaranteeSpawn.Value = guaranteeSpawnToggle.Value;
                Mod.Instance.OverrideConfig();
            });
        }

        private void CreateClickableTextFloat(string name, string prefix, ConfigEntry<float> config, float min, float max, string suffix, Vector3 pos, string tooltip) {
            StandardMenuButton button = CreateTextButton(
                () => { },
                name,
                prefix + config.Value + suffix,
                pos,
                BaldiFonts.ComicSans24,
                TextAlignmentOptions.Center,
                new Vector2(400f, 40f),
                Color.black
            );

            GameClickableText clickable = button.gameObject.AddComponent<GameClickableText>();
            clickable.button = button;
            clickable.Init(config, min, max, prefix, suffix);

            button.OnPress.RemoveAllListeners();
            button.OnPress.AddListener(clickable.OnClick);

            AddTooltip(button, tooltip);
        }

        private void CreateClickableTextInt(string name, string prefix, ConfigEntry<int> config, int min, int max, string suffix, Vector3 pos, string tooltip) {
            StandardMenuButton button = CreateTextButton(
                () => { },
                name,
                prefix + config.Value + suffix,
                pos,
                BaldiFonts.ComicSans24,
                TextAlignmentOptions.Center,
                new Vector2(400f, 40f),
                Color.black
            );

            GameClickableText clickable = button.gameObject.AddComponent<GameClickableText>();
            clickable.button = button;
            clickable.Init(config, min, max, prefix, suffix);

            button.OnPress.RemoveAllListeners();
            button.OnPress.AddListener(clickable.OnClick);

            AddTooltip(button, tooltip);
        }

        public static void OnMenuInitialize(OptionsMenu optionsMenu, CustomOptionsHandler handler) {
            if (BaseGameManager.Instance == null) {
                handler.AddCategory<PhontyMenu>("Phonty Phonograph");
            }
        }

        public static void Setup(ConfigFile config) {
            nonLethalConfig = config.Bind("Phonty", "NonLethal", false, "Enabling this will replace Phonty's ability to end the game with deafening the player.");
            deafTimeConfig = config.Bind("Phonty", "DeafTime", 20f, "Duration of deafness in seconds.");
            timeLeftUntilMad = config.Bind("Phonty", "WindUpTime", 180, "Amount of seconds until Phonty will become mad if not wound up.");
            chaseSpeedConfig = config.Bind("Phonty", "ChaseSpeed", 20f, "Speed of Phonty when chasing.");
            guaranteeSpawn = config.Bind("Phonty", "GuaranteeSpawn", false, "Enabling this will make sure that Phonty will ALWAYS spawn. Used to check if Phonty actually works.");
        }
    }
}