using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EggChat.Patches;
[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{
    internal static int chatBgOpacity;
    private static GameObject container;

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void HUDManager_awake(HUDManager __instance)
    {
        __instance.chatTextField.characterLimit = Plugin.maxCharLimit - 1;

        AddChatBg(__instance);
    }

    private static void AddChatBg(HUDManager __instance)
    {
        if (!container)
        {
            GameObject bottomLeftCorner = __instance.HUDContainer.transform.Find("BottomLeftCorner").gameObject;
            container = new("ChatBGContainer");
            container.transform.SetParent(bottomLeftCorner.transform);
            container.transform.SetSiblingIndex(0);
            CopyAttributes(container.transform, bottomLeftCorner.transform.Find("Image"));

            RectTransform rectTransform = container.AddComponent<RectTransform>();
            CopyAttributes(rectTransform, bottomLeftCorner.transform.Find("Image").GetComponent<RectTransform>());

            CanvasGroup canvasGroup = container.AddComponent<CanvasGroup>();
            canvasGroup.alpha = ((float)chatBgOpacity) / 100;
            canvasGroup.ignoreParentGroups = true;

            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0f));
            Image image = container.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.black;
        }
        SetBgOpacity();
    }

    public static void SetBgOpacity()
    {
        CanvasGroup canvasGroup = container.GetComponent<CanvasGroup>();
        canvasGroup.alpha = ((float)chatBgOpacity) / 100;
    }

    private static void CopyAttributes(RectTransform t1, RectTransform t2)
    {
        t1.anchoredPosition = t2.anchoredPosition;
        t1.anchoredPosition3D = t2.anchoredPosition3D;
        t1.offsetMax = t2.offsetMax;
        t1.offsetMin = t2.offsetMin;
        t1.eulerAngles = t2.eulerAngles;
        t1.forward = t2.forward;
    }

    private static void CopyAttributes(Transform t1, Transform t2)
    {
        t1.SetPositionAndRotation(t2.position, t2.rotation);
        t1.localScale = t2.localScale;
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void HUDManager_update(HUDManager __instance)
    {
        // when player is dead, don't show the inventory + playerinfo + clock
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
        {
            __instance.Inventory.canvasGroup.alpha = 0;
            __instance.PlayerInfo.canvasGroup.alpha = 0;
            __instance.Clock.canvasGroup.alpha = 0;
        }
    }

    [HarmonyPatch("EnableChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EnableChat_performed_transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // lets player enable chat when dead
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        foreach (var instruction in instructions)
        {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead))
            {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret)
            {
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Nop);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("SubmitChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SubmitChat_performed_transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // lets player submit chat when dead
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        foreach (var instruction in instructions)
        {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead))
            {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret)
            {
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Nop);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 50)
            {
                // increases max char limit on messages
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, Plugin.maxCharLimit);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("AddPlayerChatMessageServerRpc")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddPlayerChatMessageServerRpc_transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 50)
            {
                // increase max char limit on messages
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, Plugin.maxCharLimit);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch("AddPlayerChatMessageClientRpc")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddPlayerChatMessageClientRpc_transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
    {
        var isPlayerDead = typeof(PlayerControllerB).GetField("isPlayerDead");
        var referenceToIsPlayerDead = false;
        Label addChatMessageLabel = new Label();
        // i do not like how i did this. does one loop of instructions to get the label we need to go to
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Brtrue)
            {
                addChatMessageLabel = (Label)instruction.operand; // last usage of brtrue's operand is the label to AddChatMessage
            }
        }
        // then does another loop of instructions to edit them
        foreach (var instruction in instructions)
        {
            if (CodeInstructionExtensions.LoadsField(instruction, isPlayerDead))
            {
                referenceToIsPlayerDead = true;
            }
            if (referenceToIsPlayerDead && instruction.opcode == OpCodes.Ret)
            {
                // if receiver.isDead != sender.isDead, check if receiver.isDead. if so, can skip right to AddChatMessage. otherwise, dont do anything
                referenceToIsPlayerDead = false;
                yield return new CodeInstruction(OpCodes.Call, typeof(Plugin).GetMethod("isLocalPlayerDead"));
                yield return new CodeInstruction(OpCodes.Brtrue, addChatMessageLabel);
                yield return instruction;
            }
            else
            {
                yield return instruction;
            }
        }
    }
}