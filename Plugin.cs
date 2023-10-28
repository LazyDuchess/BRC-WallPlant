using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using System;
using System.IO;
using UnityEngine;

namespace WallPlant
{
    [BepInPlugin(GUID, Name, Version)]
    internal class Plugin : BaseUnityPlugin
    {
        public static Material GraffitiMaterial;
        public static Plugin Instance;
        public const string GUID = "com.LazyDuchess.BRC.WallPlant";
        public const string Name = "Wall Plant";
        public const string Version = "2.1.0";
        private void Awake()
        {
            Instance = this;
            try
            {
                GraffitiDatabase.Initialize();
                DecalManager.Initialize();
                var wallPlantBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "wallplant"));
                GraffitiMaterial = wallPlantBundle.LoadAsset<Material>("GraffitiMaterial");
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
