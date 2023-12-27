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
        // when player is dead, don't show the inventory + playerinfo + clock
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead) {
            __instance.Inventory.canvasGroup.alpha = 0;
            __instance.PlayerInfo.canvasGroup.alpha = 0;
            __instance.Clock.canvasGroup.alpha = 0;
        }
    }

    [HarmonyPatch("EnableChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EnableChat_performed_transpiler(IEnumerable<CodeInstruction> instructions) {
        // lets player enable chat when dead
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
        // lets player submit chat when dead
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
                // increases max char limit on messages
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
                // increase max char limit on messages
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, Plugin.maxCharLimit);
            } else {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("AddPlayerChatMessageClientRpc")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddPlayerChatMessageClientRpc_transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL) {
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        Label addChatMessageLabel = new Label();
        // i do not like how i did this. does one loop of instructions to get the label we need to go to
        foreach (var instruction in instructions) {
            if (instruction.opcode == OpCodes.Brtrue) {
                addChatMessageLabel = (Label)instruction.operand; // last usage of brtrue's operand is the label to AddChatMessage
            }
        }
        // then does another loop of instructions to edit them
        foreach (var instruction in instructions) {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead)) {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret) {
                // if receiver.isDead != sender.isDead, check if receiver.isDead. if so, can skip right to AddChatMessage. otherwise, dont do anything
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Call, typeof(Plugin).GetMethod("isLocalPlayerDead"));
                yield return new CodeInstruction(OpCodes.Brtrue, addChatMessageLabel);
                yield return instruction;
            } else {
                yield return instruction;
            }
        }
    }
}