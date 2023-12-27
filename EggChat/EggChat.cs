using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EggChat {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
        public static Plugin Instance;
        public static int maxCharLimit = 127;
        private void Awake() {
            Logger.LogInfo("Initializing EggChat");
            
            Instance = this;
            var Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony.PatchAll();

            Logger.LogInfo("Initialized EggChat");
        }

        public static bool isLocalPlayerDead() {
            return GameNetworkManager.Instance.localPlayerController.isPlayerDead;
        }
    }
}
