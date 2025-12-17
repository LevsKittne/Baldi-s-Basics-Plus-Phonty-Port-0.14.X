using HarmonyLib;
using UnityEngine;

namespace PhontyPlus.Patches {
    [HarmonyPatch]
    public class AudioPatches {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.ReturnToMenu))]
        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.RestartLevel))]
        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.LoadNextLevel))]
        [HarmonyPrefix]
        private static void RestoreAudio() {
            AudioListener.volume = 1f;
            if (Mod.GlobalMixer != null) {
                Mod.GlobalMixer.SetFloat("EchoWetMix", 0f);
            }
        }
    }
}