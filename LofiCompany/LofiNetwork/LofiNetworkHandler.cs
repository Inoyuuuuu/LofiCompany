using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LofiCompany.LofiNetwork
{
    public class LofiNetworkHandler : NetworkBehaviour
    {
        public static LofiNetworkHandler Instance { get; private set; }
        public static event Action<String> LevelEvent;

        [ClientRpc]
        public void EventClientRpc(string eventName)
        {
            LevelEvent?.Invoke(eventName); 
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }
            Instance = this;

            base.OnNetworkSpawn();
        }
    }
}
