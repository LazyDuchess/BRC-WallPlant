﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using System;
using System.IO;
using UnityEngine;
using BepInEx.Bootstrap;
using System.Linq;

namespace WallPlant
{
    [BepInPlugin(GUID, Name, Version)]
    internal class Plugin : BaseUnityPlugin
    {
        private const string CrewBoomGUID = "CrewBoom";
        public static Material GraffitiMaterial;
        public static Plugin Instance;
        public const string GUID = "com.LazyDuchess.BRC.WallPlant";
        public const string Name = "Wall Plant";
        public const string Version = "2.3.3";
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

        public static GraffitiArtInfo GetGraffitiArtInfo()
        {
            var assets = Core.Instance.Assets;
            var grafArtInfo = assets.LoadAssetFromBundle<GraffitiArtInfo>("graffiti", "GraffitiArtInfo");
            return grafArtInfo;
        }

        internal static bool IsCrewBoomInstalled()
        {
            return Chainloader.PluginInfos.Keys.Any(x => x == CrewBoomGUID);
        }
    }
}
