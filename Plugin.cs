using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using System;
using UnityEngine;

namespace WallPlant
{
    [BepInPlugin(GUID, Name, Version)]
    internal class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private const string GUID = "com.LazyDuchess.BRC.WallPlant";
        private const string Name = "Wall Plant";
        private const string Version = "1.0.0";
        private void Awake()
        {
            Instance = this;
            try
            {
                WallPlantSettings.Initialize(Config);
                var harmony = new Harmony(GUID);
                harmony.PatchAll();
                Logger.LogInfo($"{Name} {Version} loaded!");
            }
            catch(Exception e)
            {
                Logger.LogError($"{Name} {Version} failed to load! {e}");
            }
        }

        public ManualLogSource GetLogger()
        {
            return Logger;
        }
    }
}
