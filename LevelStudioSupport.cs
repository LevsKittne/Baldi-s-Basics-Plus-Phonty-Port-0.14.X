using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.UI;
using PlusLevelStudio;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using TMPro;
using UnityEngine;

namespace PhontyPlus {
    public static class LevelStudioSupport {
        public static void Register() {
            NPC phontyPrefab = Mod.assetManager.Get<NPC>("Phonty");
            EditorInterface.AddNPCVisual("Phonty", phontyPrefab);

            if (LevelLoaderPlugin.Instance != null && !LevelLoaderPlugin.Instance.npcAliases.ContainsKey("Phonty")) {
                LevelLoaderPlugin.Instance.npcAliases.Add("Phonty", phontyPrefab);
            }

            PosterObject phontyPoster = ScriptableObject.CreateInstance<PosterObject>();
            phontyPoster.name = "Pri_Phonty";
            phontyPoster.baseTexture = AssetLoader.TextureFromMod(Mod.Instance, "Textures", "pri_phonty.png");

            phontyPoster.textData = new PosterTextData[] {
                new PosterTextData() {
                    textKey = "Phonty_Pri_2",
                    position = new IntVector2(144, 98),
                    size = new IntVector2(96, 128),
                    fontSize = 12,
                    font = BaldiFonts.ComicSans12.FontAsset(),
                    color = Color.black,
                    alignment = TextAlignmentOptions.Center,
                    style = FontStyles.Normal
                },
                new PosterTextData() {
                    textKey = "Phonty_Pri_1",
                    position = new IntVector2(48, 48),
                    size = new IntVector2(160, 32),
                    fontSize = 18,
                    font = BaldiFonts.ComicSans18.FontAsset(),
                    color = Color.black,
                    alignment = TextAlignmentOptions.Center,
                    style = FontStyles.Bold
                }
            };

            if (LevelLoaderPlugin.Instance != null && !LevelLoaderPlugin.Instance.posterAliases.ContainsKey("phonty_rule")) {
                LevelLoaderPlugin.Instance.posterAliases.Add("phonty_rule", phontyPoster);
            }

            EditorInterfaceModes.AddModeCallback((mode, isVanilla) => {
                Texture2D iconTex = AssetLoader.TextureFromMod(Mod.Instance, "Textures", "npc_phonty.png");
                Sprite icon = AssetLoader.SpriteFromTexture2D(iconTex, 100f);
                EditorInterfaceModes.AddToolToCategory(mode, "npcs", new PhontyTool(icon));

                if (mode.availableTools.ContainsKey("posters")) {
                    EditorInterfaceModes.AddToolToCategory(mode, "posters", new PhontyPosterTool());
                }
            });
        }
    }

    public class PhontyTool : NPCTool {
        public PhontyTool(Sprite sprite) : base("Phonty", sprite) { }
        public override string titleKey => "Ed_Tool_Npc_Phonty";
        public override string descKey => "Ed_Tool_Npc_Phonty_Desc";
    }

    public class PhontyPosterTool : PosterTool {
        public PhontyPosterTool() : base("phonty_rule") { }
        public override string titleKey => "Phonty's Office Poster";
        public override string descKey => titleKey;
    }
}