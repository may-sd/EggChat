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
    internal static bool wideChat;
    private static Transform bottomLeftCorner;
    private static Transform input;
    private static Transform text;
    private static Transform image;
    private static GameObject background;

    public class Attributes
    {
        public static float DefaultSizeDeltaX;

        private static readonly float DefaultAnchorMaxX = 0.5f;
        private static readonly float WideAnchorMaxX = 1f;
        private static readonly float WideSizeDeltaX = 141f;
        public class Text
        {
            public class Default
            {
                public static float anchorMinX;
                public static float anchorMaxX = DefaultAnchorMaxX;
                public static float sizeDeltaX;
            }
            public class Wide
            {
                public static float anchorMinX = 0.6f;
                public static float anchorMaxX = WideAnchorMaxX;
                public static float sizeDeltaX = WideSizeDeltaX;
            }
        }

        public class Input
        {
            public class Default
            {
                public static float anchorMinX;
                public static float anchorMaxX = DefaultAnchorMaxX;
                public static float sizeDeltaX;
            }
            public class Wide
            {
                public static float anchorMinX = 0.4f;
                public static float anchorMaxX = WideAnchorMaxX;
                public static float sizeDeltaX = WideSizeDeltaX;
            }
        }

        public class Image
        {
            public class Default
            {
                public static float anchorMinX;
                public static float sizeDeltaX = WideSizeDeltaX;
            }
            public class Wide
            {
                public static float anchorMinX = 1f;
                public static float sizeDeltaX = WideSizeDeltaX + 100;
            }
        }

        public class Background
        {
            public class Default
            {
                public static float anchorMaxX = DefaultAnchorMaxX;
                public static float sizeDeltaX = DefaultSizeDeltaX;
            }
            public class Wide
            {
                public static float anchorMaxX = WideAnchorMaxX;
                public static float sizeDeltaX = WideSizeDeltaX;
            }
        }

    }

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void HUDManager_awake(HUDManager __instance)
    {
        __instance.chatTextField.characterLimit = Plugin.maxCharLimit - 1;

        bottomLeftCorner = __instance.HUDContainer.transform.Find("BottomLeftCorner");
        text = bottomLeftCorner.Find("ChatText");
        input = bottomLeftCorner.Find("InputField (TMP)");
        image = bottomLeftCorner.Find("Image");
        SetDefaultAttributes();

        AddChatBg();
        SetBgOpacity();

        if (wideChat)
        {
            SetWidthType();
        }
    }

    private static void AddChatBg()
    {
        if (!background)
        {
            background = new("ChatBG");
            Transform bgGO = background.transform;
            bgGO.SetParent(bottomLeftCorner);
            bgGO.SetSiblingIndex(0);

            bgGO.SetPositionAndRotation(image.position, image.rotation);
            bgGO.localScale = image.localScale;

            RectTransform rectTransform = background.AddComponent<RectTransform>();
            CopyAttributes(rectTransform, image.GetComponent<RectTransform>());

            CanvasGroup canvasGroup = background.AddComponent<CanvasGroup>();
            canvasGroup.alpha = ((float)chatBgOpacity) / 100;
            canvasGroup.ignoreParentGroups = true;

            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0f));
            Image img = background.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.black;
        }
    }

    private static void SetDefaultAttributes()
    {
        Attributes.DefaultSizeDeltaX = image.GetComponent<RectTransform>().sizeDelta.x;

        Attributes.Text.Default.anchorMinX = text.GetComponent<RectTransform>().anchorMin.x;
        Attributes.Text.Default.sizeDeltaX = text.GetComponent<RectTransform>().sizeDelta.x;

        Attributes.Input.Default.anchorMinX = input.GetComponent<RectTransform>().anchorMin.x;
        Attributes.Input.Default.sizeDeltaX = input.GetComponent<RectTransform>().sizeDelta.x;

        Attributes.Image.Default.anchorMinX = input.GetComponent<RectTransform>().anchorMin.x;
    }

    public static void SetBgOpacity()
    {
        CanvasGroup canvasGroup = background.GetComponent<CanvasGroup>();
        canvasGroup.alpha = ((float)chatBgOpacity) / 100;
    }

    private static void SetWidthAttributes(RectTransform rt, float? anchorMin, float? anchorMax, float sizeDelta)
    {
        if (anchorMin != null)
        {
            rt.anchorMin = new((float)anchorMin, rt.anchorMin.y);

        }
        if (anchorMax != null)
        {
            rt.anchorMax = new((float)anchorMax, rt.anchorMax.y);
        }
        rt.sizeDelta = new(sizeDelta, rt.sizeDelta.y);
    }

    public static void SetWidthType()
    {
        // set widths on input, text, bg, and image
        if (wideChat)
        {
            SetWidthAttributes(input.GetComponent<RectTransform>(), Attributes.Input.Wide.anchorMinX, Attributes.Input.Wide.anchorMaxX, Attributes.Input.Wide.sizeDeltaX);
            SetWidthAttributes(text.GetComponent<RectTransform>(), Attributes.Text.Wide.anchorMinX, Attributes.Text.Wide.anchorMaxX, Attributes.Text.Wide.sizeDeltaX);
            SetWidthAttributes(image.GetComponent<RectTransform>(), Attributes.Image.Wide.anchorMinX, null, Attributes.Image.Wide.sizeDeltaX);
            SetWidthAttributes(background.GetComponent<RectTransform>(), null, Attributes.Background.Wide.anchorMaxX, Attributes.Background.Wide.sizeDeltaX);
        }
        else
        {
            SetWidthAttributes(input.GetComponent<RectTransform>(), Attributes.Input.Default.anchorMinX, Attributes.Input.Default.anchorMaxX, Attributes.Input.Default.sizeDeltaX);
            SetWidthAttributes(text.GetComponent<RectTransform>(), Attributes.Text.Default.anchorMinX, Attributes.Text.Default.anchorMaxX, Attributes.Text.Default.sizeDeltaX);
            SetWidthAttributes(image.GetComponent<RectTransform>(), Attributes.Image.Default.anchorMinX, null, Attributes.Image.Default.sizeDeltaX);
            SetWidthAttributes(background.GetComponent<RectTransform>(), null, Attributes.Background.Default.anchorMaxX, Attributes.Background.Default.sizeDeltaX);
        }
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