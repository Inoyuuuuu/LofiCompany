using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static BepInEx.BepInDependency;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using LofiCompany.Configs;
using System.Linq;

namespace LofiCompany
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.sigurd.csync", DependencyFlags.HardDependency)]
    public class LofiCompany : BaseUnityPlugin
    {
        public static LofiCompany Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        internal static string ASSET_BUNDLE_NAME = "lofiassetbundle";
        internal static AssetBundle? lofiAssetBundle;

        internal static LofiConfigs lofiConfigs;

        internal static List<AudioClip> allLofiSongs = [];
        internal static List<int> lofiSongIndexesInQueue = [];
        internal static List<int> playedLofiSongIndexes = [];
        internal static List<LevelWeatherType> lofiWeatherTypes = [];
        internal static List<DayMode> lofiDayModes = [];
        internal static bool wasLofiStopped = false;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            lofiConfigs = new LofiConfigs(Config);
            LoadLofiMusic();

            if (lofiSongIndexesInQueue.Count > 0)
            {
                Patch();
                Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
            }
            else
            {
                Logger.LogError($"ERROR_01: Couldn't load lofi songs. Make sure the assetbundle ({ASSET_BUNDLE_NAME}) is located next to this mod's assembly file.");
            }

        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        internal static void LoadLofiMusic()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string lofiAssetBundleDir = Path.Combine(assemblyDir, ASSET_BUNDLE_NAME);

            lofiAssetBundle = AssetBundle.LoadFromFile(lofiAssetBundleDir);
            allLofiSongs = [.. lofiAssetBundle.LoadAllAssets<AudioClip>()];

            for (int i = 0; i < allLofiSongs.Count; i++)
            {
                lofiSongIndexesInQueue.Add(i);
            }
        }
    }
}
