using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace EggChat.Patches;
[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch {
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void HUDManager_awake(HUDManager __instance) {
        __instance.chatTextField.characterLimit = Plugin.maxCharLimit - 1;
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void HUDManager_update(HUDManager __instance) {
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead) {
            __instance.Inventory.canvasGroup.alpha = 0;
            __instance.PlayerInfo.canvasGroup.alpha = 0;
            __instance.Clock.canvasGroup.alpha = 0;
        }
    }

    [HarmonyPatch("EnableChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EnableChat_performed_transpiler(IEnumerable<CodeInstruction> instructions) {
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        foreach (var instruction in instructions) {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead)) {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret) {
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Nop);
            } else { 
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("SubmitChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SubmitChat_performed_transpiler(IEnumerable<CodeInstruction> instructions) {
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        foreach (var instruction in instructions) {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead)) {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret) {
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Nop);
            } else if (instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 50) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, Plugin.maxCharLimit);
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
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, Plugin.maxCharLimit);
            } else {
                yield return instruction;
            }
        }
    }
}