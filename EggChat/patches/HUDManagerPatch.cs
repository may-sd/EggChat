using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace EggChat.Patches;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch {
    [HarmonyPatch("SubmitChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SubmitChat_performed_transpiler(IEnumerable<CodeInstruction> instructions) {
        foreach (var instruction in instructions) {
            if (instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 50) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 127);
            } else {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("AddPlayerChatMessageServerRpc")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddPlayerChatMessageServerRpc_transpiler(IEnumerable<CodeInstruction> instructions) {
        foreach (var instruction in instructions) {
            if (instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 50) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 127);
            } else {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void HUDManager_awake(HUDManager __instance) {
        __instance.chatTextField.characterLimit = 126;
    }
}