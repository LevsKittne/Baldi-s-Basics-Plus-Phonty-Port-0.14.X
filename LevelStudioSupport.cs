using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using UnityEngine;

namespace PhontyPlus {
    public static class LevelStudioSupport {
        public static void Register() {
            NPC phontyPrefab = Mod.assetManager.Get<NPC>("Phonty");
            EditorInterface.AddNPCVisual("Phonty", phontyPrefab);
            if (LevelLoaderPlugin.Instance != null && !LevelLoaderPlugin.Instance.npcAliases.ContainsKey("Phonty")) {
                LevelLoaderPlugin.Instance.npcAliases.Add("Phonty", phontyPrefab);
            }

            EditorInterfaceModes.AddModeCallback((mode, isVanilla) => {
                Texture2D iconTex = AssetLoader.TextureFromMod(Mod.Instance, "Textures", "npc_phonty.png");
                Sprite icon = AssetLoader.SpriteFromTexture2D(iconTex, 100f);
                EditorInterfaceModes.AddToolToCategory(mode, "npcs", new PhontyTool(icon));
            });
        }
    }

    public class PhontyTool : NPCTool {
        public PhontyTool(Sprite sprite) : base("Phonty", sprite) { }
        public override string titleKey => "Ed_Tool_Npc_Phonty";
        public override string descKey => "Ed_Tool_Npc_Phonty_Desc";
    }
}