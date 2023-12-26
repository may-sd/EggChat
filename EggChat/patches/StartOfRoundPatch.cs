using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace EggChat.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatch {
    [HarmonyPatch(typeof(StartOfRound), "EndOfGame", MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EndOfGame_transpiler(IEnumerable<CodeInstruction> instructions) {
        // keeps the HUD up during end of game screen
        var hideHUD = typeof(HUDManager).GetMethod("HideHUD");
        foreach (var instruction in instructions) {
            if (CodeInstructionExtensions.Calls(instruction, hideHUD)) {
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return instruction;
            } else {
                yield return instruction;
            }
        }
    }
}