using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EggChat.Patches;
using HarmonyLib;

namespace EggChat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static int maxCharLimit = 127;
        private static ConfigEntry<int> chatBgOpacity;
        private void Awake()
        {
            Logger.LogInfo("Initializing EggChat");

            SetUpConfigs();

            Instance = this;
            var Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony.PatchAll();

            Logger.LogInfo("Initialized EggChat");
        }

        public static bool isLocalPlayerDead()
        {
            return GameNetworkManager.Instance.localPlayerController.isPlayerDead;
        }

        private void SetUpConfigs()
        {
            chatBgOpacity = Config.Bind("UI", "Chat Background Opacity", 0, "The opacity of the chat background. Set values 0-100.");
            chatBgOpacity.SettingChanged += (obj, args) =>
            {
                HUDManagerPatch.chatBgOpacity = chatBgOpacity.Value;
            };
        }
    }
}
