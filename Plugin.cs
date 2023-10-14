using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using UnityEngine;

namespace WallPlant
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    internal class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private const string GUID = "org.lazyduchess.plugins.brc.wallplant";
        private const string Name = "Wall Plant";
        private const string Version = "1.0.0.0";
        private void Awake()
        {
            Instance = this;

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {Name} {Version} is loaded!");
        }

        public ManualLogSource GetLogger()
        {
            return Logger;
        }
    }
}
