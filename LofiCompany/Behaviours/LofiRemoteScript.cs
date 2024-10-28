using LofiCompany.Patches;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LofiCompany.Behaviours
{
    internal class LofiRemoteScript : GrabbableObject
    {
        public AudioSource lofiRemoteAudioSource;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            lofiRemoteAudioSource.PlayOneShot(lofiRemoteAudioSource.clip);
            WalkieTalkie.TransmitOneShotAudio(lofiRemoteAudioSource, lofiRemoteAudioSource.clip, 0.7f);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.4f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);

            StartOfRound startOfRound = StartOfRound.Instance;


            if (!startOfRound.speakerAudioSource.isPlaying)
            {
                PlayLofiPatch.PlayRandomSong();
            } else
            {
                startOfRound.DisableShipSpeaker();
                PlayLofiPatch.isPlayingLofiSong = false;
            }
        }

        public override void __initializeVariables()
        {
            base.__initializeVariables();
        }

        public override string __getTypeName()
        {
            return "LofiRemoteProp";
        }

        //[ServerRpc]
        //public void PlayLofiSongServerRpc()
        //{
        //    NetworkManager networkManager = base.NetworkManager;
        //    if ((object)networkManager == null || !networkManager.IsListening)
        //    {
        //        return;
        //    }
        //    if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
        //    {
        //        ClientRpcParams clientRpcParams = default(ClientRpcParams);
        //        FastBufferWriter bufferWriter = __beginSendClientRpc(152346789u, clientRpcParams, RpcDelivery.Reliable);
        //        __endSendClientRpc(ref bufferWriter, 152346789u, clientRpcParams, RpcDelivery.Reliable);
        //    }
        //    if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
        //    {
        //        PlayLofiSongClientRpc();
        //    }
        //}

        //[ClientRpc]
        //public void PlayLofiSongClientRpc(int songIndex)
        //{
        //    NetworkManager networkManager = base.NetworkManager;
        //    if ((object)networkManager == null || !networkManager.IsListening)
        //    {
        //        return;
        //    }
        //    if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
        //    {
        //        ClientRpcParams clientRpcParams = default(ClientRpcParams);
        //        FastBufferWriter bufferWriter = __beginSendClientRpc(152346789u, clientRpcParams, RpcDelivery.Reliable);
        //        __endSendClientRpc(ref bufferWriter, 152346789u, clientRpcParams, RpcDelivery.Reliable);
        //    }
        //    if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
        //    {
                
        //    }
        //}
    }
}
