using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LofiCompany.Configs;
using LofiCompany.LofiNetwork;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static BepInEx.BepInDependency;

namespace LofiCompany
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.sigurd.csync", DependencyFlags.HardDependency)]
    public class LofiCompany : BaseUnityPlugin
    {
        public static LofiCompany Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        internal static string ASSET_BUNDLE_NAME = "lofiassetbundle";
        internal static string LOFI_NETWORK_OBJECT_PATH = "Assets/Mods/LofiCompany/LofiNetworkObject.prefab";
        internal static AssetBundle? lofiAssetBundle;

        internal static LofiConfigs lofiConfigs;

        internal static List<AudioClip> allLofiSongs = [];
        internal static List<AudioClip> lofiSongsInQueue = [];
        internal static List<AudioClip> playedLofiSongs = [];
        internal static List<LevelWeatherType> lofiWeatherTypes = [];
        internal static List<DayMode> lofiDayModes = [];
        internal static bool wasLofiStopped = false;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            lofiConfigs = new LofiConfigs(Config);
            NetcodePatcher();
            LoadLofiMusic();

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ASSET_BUNDLE_NAME);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(assetDir);
            if (assetBundle == null)
            {
                Logger.LogError($"ERROR_06: Couldn't load asset bundle. Make sure the assetbundle ({ASSET_BUNDLE_NAME}) is located next to this mod's assembly file.");
            }

            GameObject lofiNetworkObject = assetBundle.LoadAsset<GameObject>(LOFI_NETWORK_OBJECT_PATH);

            if (lofiNetworkObject == null)
            {
                Logger.LogError($"ERROR_07: Couldn't load lofi network object. Make sure the assetbundle ({ASSET_BUNDLE_NAME}) is located next to this mod's assembly file.");
            }

            LofiNetworkScript lofiNetworkScript = lofiNetworkObject.AddComponent<LofiNetworkScript>();

            if (lofiSongsInQueue.Count > 0 && lofiNetworkObject.GetComponent<LofiNetworkScript>() != null)
            {
                Patch();
                Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
            }
            else if (lofiNetworkObject.GetComponent<LofiNetworkScript>() == null)
            {
                Logger.LogError($"ERROR_08: Couldn't add lofi network script to object.");
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
            lofiSongsInQueue.AddRange(allLofiSongs);
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

    }
}
