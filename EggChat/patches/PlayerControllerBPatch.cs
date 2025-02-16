using GameNetcodeStuff;
using HarmonyLib;

namespace EggChat.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerBPatch
{
    [HarmonyPatch("KillPlayer")]
    [HarmonyPostfix]
    private static void KillPlayer_postfix()
    {
        HUDManager.Instance.HideHUD(false);
    }
}