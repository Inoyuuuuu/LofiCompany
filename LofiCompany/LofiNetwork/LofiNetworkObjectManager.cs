using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LofiCompany.LofiNetwork
{
    [HarmonyPatch]
    public class LofiNetworkObjectManager
    {
        public static GameObject networkPrefab;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)LofiCompany.lofiAssetBundle.LoadAsset("ExampleNetworkHandler");
            networkPrefab.AddComponent<LofiNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
        static void SubscribeToHandler()
        {
            LofiNetworkHandler.LevelEvent += ReceivedEventFromServer;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        static void UnsubscribeFromHandler()
        {
            LofiNetworkHandler.LevelEvent -= ReceivedEventFromServer;
        }

        static void ReceivedEventFromServer(string eventName)
        {
            // Event Code Here
        }

        static void SendEventToClients(string eventName)
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return;

            LofiNetworkHandler.Instance.EventClientRpc(eventName);
        }
    }
}
