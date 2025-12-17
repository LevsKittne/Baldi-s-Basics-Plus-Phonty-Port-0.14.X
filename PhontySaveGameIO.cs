using BepInEx;
using MTM101BaldAPI.SaveSystem;
using System.IO;

namespace PhontyPlus {
    public class PhontySaveGameIO : ModdedSaveGameIOBinary {
        private readonly PluginInfo _info;
        public override PluginInfo pluginInfo => _info;

        public PhontySaveGameIO(PluginInfo info) { _info = info; }

        public override void Load(BinaryReader reader) { reader.ReadByte(); }

        public override void Save(BinaryWriter writer) { writer.Write((byte)0); }

        public override void Reset() { }

        public override string[] GenerateTags() {
            return new string[] {
                PhontyMenu.nonLethalConfig.Value.ToString(),
                PhontyMenu.timeLeftUntilMad.Value.ToString(),
                PhontyMenu.guaranteeSpawn.Value.ToString()
                };
        }

        public override string DisplayTags(string[] tags) {
            if (tags.Length != 3) return "Invalid";
            return $"Non-Lethal: {tags[0]}\nWind-up Time: {tags[1]}s\nGuarantee Spawn: {tags[2]}";
        }
    }
}
