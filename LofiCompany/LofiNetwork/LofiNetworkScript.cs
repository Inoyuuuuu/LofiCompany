using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LofiCompany.LofiNetwork
{
    internal class LofiNetworkScript : NetworkBehaviour
    {
        //[ClientRpc]
        //public void PlayRandomSongClientRpc()
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
        //        BytePacker.WriteValuePacked(bufferWriter, gnomePitch);
        //        __endSendClientRpc(ref bufferWriter, 152346789u, clientRpcParams, RpcDelivery.Reliable);
        //    }
        //    if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
        //    {
        //        ChangePitch(gnomePitch);
        //        gnomeAudioSource.PlayOneShot(gnomeSound);
        //    }
        //}

        //[ClientRpc]
        //public void FadeOutTODMusicClientRpc()
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
        //        BytePacker.WriteValuePacked(bufferWriter, gnomePitch);
        //        __endSendClientRpc(ref bufferWriter, 152346789u, clientRpcParams, RpcDelivery.Reliable);
        //    }
        //    if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
        //    {
        //        ChangePitch(gnomePitch);
        //        gnomeAudioSource.PlayOneShot(gnomeSound);
        //    }
        //}

        //[ClientRpc]
        //public void ResetMusicQueueClientRpc()
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
        //        BytePacker.WriteValuePacked(bufferWriter, gnomePitch);
        //        __endSendClientRpc(ref bufferWriter, 152346789u, clientRpcParams, RpcDelivery.Reliable);
        //    }
        //    if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
        //    {
        //        ChangePitch(gnomePitch);
        //        gnomeAudioSource.PlayOneShot(gnomeSound);
        //    }
        //}



        //private void PlayRandomSong(System.Random random)
        //{
        //    StartOfRound startOfRound = StartOfRound.Instance;

        //    if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
        //    {
        //        FadeOutTODMusicClientRpc();
        //    }

        //    if (LofiCompany.lofiSongsInQueue.Count <= 0)
        //    {
        //        ResetMusicQueueClientRpc();
        //    }

        //    AudioSource speakers = startOfRound.speakerAudioSource;
        //    int songIndex = random.Next(0, LofiCompany.lofiSongsInQueue.Count - 1);


        //    AudioClip song = LofiCompany.lofiSongsInQueue[songIndex];

        //    startOfRound.StartCoroutine(AudioUtils.FadeInMusicSource(speakers, musicVolume));
        //    speakers.volume = 0f;
        //    speakers.PlayOneShot(song);
        //    isPlayingLofiSong = true;

        //    LofiCompany.lofiSongsInQueue.Remove(song);

        //    LofiCompany.Logger.LogInfo($"Hello and good {TimeOfDay.Instance.dayMode}! It is {GetTimeAsString()} and the company wants to play some music.");
        //    LofiCompany.Logger.LogInfo($"The ship's speakers are now playing: \n {song.name}");
        //}
    }
}
