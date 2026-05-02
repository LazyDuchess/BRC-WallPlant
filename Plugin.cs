using BepInEx;
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
    [BepInDependency(CrewBoomGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(AllCityNetworkGUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal class Plugin : BaseUnityPlugin
    {
        public static bool AllCityNetworkInstalled { get; private set; }
        public static bool CrewBoomInstalled { get; private set; }
        private const string CrewBoomGUID = "CrewBoom";
        private const string AllCityNetworkGUID = "BombRushMP.Plugin";
        public static Material GraffitiMaterial;
        public static Plugin Instance;
        public const string GUID = "com.LazyDuchess.BRC.WallPlant";
        public const string Name = "Wall Plant";
        public const string Version = "2.7.0";

        private void Awake()
        {
            Instance = this;
            try
            {
                AllCityNetworkInstalled = IsAllCityNetworkInstalled();
                CrewBoomInstalled = IsCrewBoomInstalled();
                if (AllCityNetworkInstalled)
                {
                    try
                    {
                        Net.Initialize();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to initialize networking support for Wallplant even though SlopCrew is installed. You might have an outdated mod or an older version of the SlopCrew API for some reason.{Environment.NewLine}{e}");
                        AllCityNetworkInstalled = false;
                    }
                }
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

        private void Update()
        {
            if (AllCityNetworkInstalled)
                Net.Update();
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

        private static bool IsCrewBoomInstalled()
        {
            return Chainloader.PluginInfos.Keys.Contains(CrewBoomGUID);
        }

        private static bool IsAllCityNetworkInstalled()
        {
            return Chainloader.PluginInfos.Keys.Contains(AllCityNetworkGUID);
        }
    }
}
