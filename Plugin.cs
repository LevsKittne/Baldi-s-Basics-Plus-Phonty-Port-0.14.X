using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using JetBrains.Annotations;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PhontyPlus {
    [BepInPlugin(Mod.ModGuid, Mod.ModName, Mod.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    public class Mod : BaseUnityPlugin {
        public const string ModName = "Phonty";
        public const string ModGuid = "levs_kittne.baldiplus.phonty";
        public const string ModVersion = "4.0.5";

        public static AssetManager assetManager = new AssetManager();
        private List<string> scenes = new List<string>();

        public static Mod Instance { get; private set; }
        private PhontySaveGameIO saveGame;
        private string modpath;
        private static NPC phontyPrefab;
        public static bool gameAssetsLoaded = false;
        public static AudioMixer GlobalMixer;

        void Awake() {
            Instance = this;
            modpath = AssetLoader.GetModPath(this);

            new Harmony(ModGuid).PatchAll();

            //SceneManager.sceneLoaded += OnSceneLoad;

            AssetLoader.LocalizationFromFile(Path.Combine(modpath, "Lang_En.json"), Language.English);

            LoadingEvents.RegisterOnAssetsLoaded(Info, CreateNpcPrefab(), LoadingEventOrder.Pre);

            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorAddend);

            CustomOptionsCore.OnMenuInitialize += PhontyMenu.OnMenuInitialize;
            PhontyMenu.Setup(Config);

            saveGame = new PhontySaveGameIO(Info);
            ModdedSaveGame.AddSaveHandler(saveGame);
        }

        public void Start() {
            SubtitleManager.Instance.gameObject.AddComponent<CanvasGroup>();
        }

        /*public List<T> PrefabInstances<T>() where T : UnityEngine.Object {
            var resources = Resources.FindObjectsOfTypeAll<T>();
            return resources.ToList();
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode) {
            if (!scenes.Contains(scene.name)) {
                scenes.Add(scene.name);
                if (scene.name == "MainMenu") {
                    var MainPre = PrefabInstances<GameObject>().Find(x => x.name == "Main");
                    if (MainPre != null) {
                        var SubManPre = MainPre.GetComponentInChildren<SubtitleManager>();
                        if (SubManPre != null) {
                            SubManPre.gameObject.AddComponent<CanvasGroup>();
                        }
                    }
                }
            }
        }*/

        public void OverrideConfig() {
            Config.Save();
            saveGame.GenerateTags();
            if (ModdedFileManager.Instance != null) {
                ModdedFileManager.Instance.RegenerateTags();
            }
        }

        private void GeneratorAddend(string floorName, int floorNumber, SceneObject sceneObject) {
            if (phontyPrefab == null) {
                Debug.LogError("PhontyPlus: Phonty prefab is not loaded, cannot add to level generation!");
                return;
            }
            if (floorName.StartsWith("F") || floorName == "END") {
                sceneObject.MarkAsNeverUnload();
                AddNpc(floorName == "END" || floorNumber == 0, sceneObject);
            }
        }

        private void AddNpc(bool guaranteeSpawn, SceneObject sceneObject) {
            if (PhontyMenu.guaranteeSpawn.Value && guaranteeSpawn) {
                sceneObject.forcedNpcs = sceneObject.forcedNpcs.AddToArray(phontyPrefab);
                sceneObject.additionalNPCs = Mathf.Max(0, sceneObject.additionalNPCs - 1);
            }
            else if (!PhontyMenu.guaranteeSpawn.Value) {
                sceneObject.potentialNPCs.Add(new WeightedNPC() { selection = phontyPrefab, weight = 75 });
            }
        }

        private IEnumerator CreateNpcPrefab() {
            yield return 2;

            yield return "Loading Phonty...";
            try {
                Phonty.LoadAssets();
            }
            catch (System.Exception e) {
                Debug.LogError($"PhontyPlus: Failed to load assets! {e}");
                yield break;
            }

            yield return "Creating Phonty NPC Prefab...";
            try {
                var phonty = new NPCBuilder<Phonty>(Info)
                    .SetName("Phonty")
                    .SetEnum("Phonty")
                    .SetMetaName("Phonty")
                    .AddMetaFlag(NPCFlags.Standard)
                    .SetPoster(AssetLoader.TextureFromMod(this, "Textures", "pri_phonty.png"), "Phonty_Pri_1", "Phonty_Pri_2")
                    .AddLooker()
                    .SetMinMaxAudioDistance(0, 300)
                    .AddSpawnableRoomCategories(RoomCategory.Faculty)
                    .Build();

                phonty.audMan = phonty.GetComponent<AudioManager>();

                var animator = phonty.gameObject.AddComponent<CustomSpriteRendererAnimator>();
                animator.renderer = phonty.spriteRenderer[0];
                phonty.animator = animator;

                phonty.spriteRenderer[0].sprite = Phonty.idle;

                phontyPrefab = phonty;
                assetManager.Add("Phonty", phonty);
            }
            catch (System.Exception e) {
                Debug.LogError($"PhontyPlus: Failed to create NPC prefab! {e}");
                yield break;
            }

            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio")) {
                yield return "Registering Phonty for Level Studio...";
                LevelStudioSupport.Register();
            }
        }
    }
}