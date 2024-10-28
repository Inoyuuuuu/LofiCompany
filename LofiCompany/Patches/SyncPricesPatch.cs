using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using UnityEngine;

namespace LofiCompany.Patches
{
    [HarmonyPatch()]
    internal class SyncPricesPatch
    {
        private const float methodUptimeDefVal = 12f;

        private static float methodUptime = 12f;     //letting this patch run in loop for couple of seconds since csync takes a bit to fully sync
        private static float updateConfigStart = 5f; //start sync after this (in seconds)


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPriority(Priority.Last)]
        public static void PatchLaddersConfigs()
        {
            if (methodUptime > 0)
            {
                methodUptime -= Time.deltaTime;

                if (methodUptime < (methodUptimeDefVal - updateConfigStart))
                {
                    SyncLadderPrices();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            methodUptime = methodUptimeDefVal;
        }

        private static void SyncLadderPrices()
        {
            if (LofiCompany.lofiRemoteItem != null)
            {
                UpdatePrice(LofiCompany.lofiRemoteItem, LofiCompany.lofiConfigs.lofiRemotePrice);
            }
        }

        private static void UpdatePrice(Item item, int updatedPrice)
        {
            if (item != null)
            {
                Items.UpdateShopItemPrice(item, updatedPrice);
            }
        }
    }
}
