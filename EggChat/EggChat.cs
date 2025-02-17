using BepInEx;
using BepInEx.Configuration;
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
        private static ConfigEntry<bool> wideChat;
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
            HUDManagerPatch.chatBgOpacity = chatBgOpacity.Value;
            chatBgOpacity.SettingChanged += (obj, args) =>
            {
                HUDManagerPatch.chatBgOpacity = chatBgOpacity.Value;
                HUDManagerPatch.SetBgOpacity();
            };

            wideChat = Config.Bind("UI", "Wide Chat", false, "Make the chat wider.");
            HUDManagerPatch.wideChat = wideChat.Value;
            wideChat.SettingChanged += (obj, args) =>
            {
                HUDManagerPatch.wideChat = wideChat.Value;
                HUDManagerPatch.SetWidthType();
            };
        }
    }
}
