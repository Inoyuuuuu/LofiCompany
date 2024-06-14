using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace LofiCompany
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class LofiCompany : BaseUnityPlugin
    {
        public static LofiCompany Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        internal static string ASSET_BUNDLE_NAME = "lofiassetbundle";
        internal static AssetBundle? lofiAssetBundle;

        internal static List<AudioClip> lofiSongs = [];
        internal static List<LevelWeatherType> lofiWeatherTypes = [];
        internal static List<DayMode> lofiDayModes = [];

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            LoadLofiMusic();
            InitLofiWeatherTypes();
            InitLofiDayModes();

            if (lofiSongs.Count > 0)
            {
                Patch();
                Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
            }
            else
            {
                Logger.LogError($"ERROR: Couldn't load lofi songs. Make sure the assetbundle ({ASSET_BUNDLE_NAME}) is located next to this mod's assembly file.");
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
            Logger.LogInfo("loader reached");
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string lofiAssetBundleDir = Path.Combine(assemblyDir, ASSET_BUNDLE_NAME);
            lofiAssetBundle = AssetBundle.LoadFromFile(lofiAssetBundleDir);
            lofiSongs = [.. lofiAssetBundle.LoadAllAssets<AudioClip>()];
        }

        internal static void InitLofiWeatherTypes()
        {
            lofiWeatherTypes.Add(LevelWeatherType.Rainy);
            lofiWeatherTypes.Add(LevelWeatherType.Stormy);
            lofiWeatherTypes.Add(LevelWeatherType.Flooded);
        }

        internal static void InitLofiDayModes()
        {
            lofiDayModes.Add(DayMode.Noon);
        }
    }
}
