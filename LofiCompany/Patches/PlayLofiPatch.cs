using HarmonyLib;
using LethalNetworkAPI;
using LofiCompany.Configs;
using LofiCompany.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LofiCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class PlayLofiPatch
    {
        private static LethalClientMessage<int> playSongClientMsg = new("playLofiSong", onReceivedFromClient: PlayLofiSongClient);
        private static LethalClientMessage<string> fadeInSFXSourceClientMsg = new("fadeInSFXSource", onReceivedFromClient: FadeInShipSpeakerSFXSourceClient);
        private static LethalClientMessage<string> fadeOutSFXSourceClientMsg = new("fadeOutSFXSource", onReceivedFromClient: FadeOutSFXSourceClient);
        private static LethalClientMessage<bool> setVolumeToStandartClientMsg = new("setVolumeToStandart", onReceivedFromClient: SetIsSpeakerVolStandartVolClient);

        private static System.Random random = new(0);

        private static int[] hoursMinutes = new int[2];
        private static int currentHours = 0;
        private static int currentMinutes = 0;

        private static int chancePerAttempt = LofiConfigs.defaultChancePerAttempt;
        private static int attemptsPerHour = LofiConfigs.attemptsPerHourBaseValue;
        private static float musicVolume = LofiConfigs.defaultMusicVolume;
        private static float playerShipLeaveTimer = LofiConfigs.defaultPlayerLeaveShipTimer;

        private static int[] lofiTimestamps = new int[chancePerAttempt];
        private static int lastCheckedTimestamp = -1;

        private static bool wasShipPowerSurged = false;
        private static bool isPlayingMusicNextOpportunity = false;
        private static bool isShiproomEmpty = false;
        internal static bool isPlayingLofiSong = false;

        private static bool isSpeakerVolStandartVol = true;

        [HarmonyPatch(nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        public static void PlayLofiAtRandom(StartOfRound __instance)
        {
            musicVolume = LofiCompany.lofiConfigs.musicVolume.Value;
            chancePerAttempt = LofiCompany.lofiConfigs.chancePerAttempt.Value;
            attemptsPerHour = LofiCompany.lofiConfigs.attemptsPerHour.Value;

            if (__instance.localPlayerController.IsHost)
            {
                UpdateWasShipPowerSurged();

                //------------ update time ------------
                hoursMinutes = GetCurrentHoursAndMinutes();
                if (currentMinutes != hoursMinutes[1])
                {
                    currentMinutes = hoursMinutes[1];
                }

                if (currentHours != hoursMinutes[0])
                {
                    currentHours = hoursMinutes[0];
                    currentMinutes = 0;
                }

                //------------ check if players haven't been in the ship for a while ------------
                if (IsAPlayerInShiproom())
                {
                    playerShipLeaveTimer = LofiCompany.lofiConfigs.playerLeaveShipTimer.Value;
                    isShiproomEmpty = false;
                }
                else
                {
                    if (playerShipLeaveTimer > 0)
                    {
                        playerShipLeaveTimer -= Time.deltaTime;
                    }
                    else
                    {
                        isShiproomEmpty = true;
                    }
                }

                //------------ play random song if conditions are met ------------
                bool areShipConditionsMet = !__instance.speakerAudioSource.isPlaying
                    && !__instance.shipIsLeaving
                    && __instance.shipHasLanded
                    && !__instance.inShipPhase
                    && !(LofiCompany.wasLofiStopped && LofiCompany.lofiConfigs.isLofiStopActive);

                if (!isPlayingLofiSong && areShipConditionsMet && IsAPlayerInShiproom())
                {
                    LevelWeatherType currentWeather = TimeOfDay.Instance.currentLevelWeather;
                    DayMode currentTimeOfDay = TimeOfDay.Instance.dayMode;
                    bool isLofiWeather = LofiCompany.lofiWeatherTypes.Contains(currentWeather) || LofiConfigs.areAllWeatherTypesAccepted;
                    bool isLofiDaytime = LofiCompany.lofiDayModes.Contains(currentTimeOfDay) || LofiConfigs.areAllDayModesAccepted;

                    if (isLofiWeather && isLofiDaytime)
                    {
                        if (lofiTimestamps.Contains(currentMinutes) && currentMinutes != lastCheckedTimestamp && !IsDiscoAudioPlaying())
                        {
                            lastCheckedTimestamp = currentMinutes;
                            isPlayingMusicNextOpportunity = IsPlayingOnNextOpportunity();
                        }

                        if (isPlayingMusicNextOpportunity)
                        {
                            PlayRandomSong();
                        }
                    }
                }

                //------------ while lofi is playing ------------
                if (isPlayingLofiSong)
                {
                    AudioSource speakers = __instance.speakerAudioSource;
                    bool isMusicFadeOutNeeded = __instance.shipIsLeaving || isShiproomEmpty;

                    if (isMusicFadeOutNeeded)
                    {
                        fadeOutSFXSourceClientMsg.SendAllClients("speakers");
                    }

                    if (!speakers.isPlaying)
                    {
                        isPlayingLofiSong = false;
                        isPlayingMusicNextOpportunity = false;

                    }

                    if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
                    {
                        TimeOfDay.Instance.TimeOfDayMusic.Stop();
                    }

                }
            }

            if (__instance.speakerAudioSource.isPlaying 
                && !AudioUtils.isFadingIn
                && !AudioUtils.isFadingOut
                && __instance.speakerAudioSource.volume != LofiCompany.lofiConfigs.musicVolume)
            {
                __instance.speakerAudioSource.volume = LofiCompany.lofiConfigs.musicVolume;
            }
        }

        [HarmonyPatch(nameof(StartOfRound.PowerSurgeShip))]
        [HarmonyPrefix]
        public static void ShipPowerSurgeListener()
        {
            wasShipPowerSurged = true;
        }

        [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPrefix]
        public static void OnShipLanded(StartOfRound __instance)
        {
            if (LofiCompany.lofiSongIndexesInQueue.Count <= 0)
            {
                ResetMusicQueue();
            }

            CalcAllLofiTimestamps();
            LofiCompany.lofiConfigs.ParseLofiConditions();
            LofiCompany.wasLofiStopped = false;

            for (int i = 0; i < LofiCompany.lofiWeatherTypes.Count; i++)
            {
                LofiCompany.Logger.LogDebug("lofi weather selection includes: " + LofiCompany.lofiWeatherTypes[i].ToString());
            }
            for (int i = 0; i < LofiCompany.lofiDayModes.Count; i++)
            {
                LofiCompany.Logger.LogDebug("lofi daytime selection includes: " + LofiCompany.lofiDayModes[i].ToString());
            }

            random = new System.Random(__instance.randomMapSeed);
        }

        [HarmonyPatch(nameof(StartOfRound.DisableShipSpeaker))]
        [HarmonyPrefix]
        public static void DisablePlaybackForTheDay()
        {
            if (isPlayingLofiSong)
            {
                LofiCompany.wasLofiStopped = true;
            }
        }

        private static bool IsPlayingOnNextOpportunity()
        {
            int rN = random.Next(0, 100);
            return rN < chancePerAttempt;
        }

        private static void CalcAllLofiTimestamps()
        {
            int checkPointMinutes = (int)(60 / attemptsPerHour);
            lofiTimestamps = new int[attemptsPerHour];

            for (int i = 0; i < attemptsPerHour; i++)
            {
                lofiTimestamps[i] = checkPointMinutes * i;
            }
        }

        private static int[] GetCurrentHoursAndMinutes()
        {
            if (TimeOfDay.Instance.currentLevel == null || StartOfRound.Instance.currentLevel == null)
            {
                return [999, 999];
            }

            float planetTimeOfDay = TimeOfDay.Instance.CalculatePlanetTime(StartOfRound.Instance.currentLevel);
            float timeOfDayInHours = planetTimeOfDay / TimeOfDay.Instance.lengthOfHours;

            return [(int)timeOfDayInHours + 6, (int)((timeOfDayInHours - (int)timeOfDayInHours) * 60)];
        }

        internal static void PlayRandomSong()
        {
            if (LofiCompany.lofiSongIndexesInQueue.Count <= 0)
            {
                ResetMusicQueue();
            }

            int songIndex = LofiCompany.lofiSongIndexesInQueue[random.Next(0, LofiCompany.lofiSongIndexesInQueue.Count - 1)];
            isPlayingLofiSong = true;

            fadeInSFXSourceClientMsg.SendAllClients("speakers");
            playSongClientMsg.SendAllClients(songIndex);
        }

        private static string GetTimeAsString()
        {
            StringBuilder hourString = new();
            StringBuilder minuteString = new();
            hourString.Append(currentHours);
            minuteString.Append(currentMinutes);

            if (hourString.Length == 1)
            {
                hourString.Insert(0, 0);
            }
            if (minuteString.Length == 1)
            {
                minuteString.Insert(0, 0);
            }
            return hourString.Append(':').Append(minuteString).ToString();
        }

        private static void UpdateWasShipPowerSurged()
        {
            if (wasShipPowerSurged && StartOfRound.Instance.shipRoomLights.areLightsOn)
            {
                wasShipPowerSurged = false;
            }
        }

        private static void ResetMusicQueue()
        {
            LofiCompany.lofiSongIndexesInQueue.Clear();
            for (int i = 0; i < LofiCompany.allLofiSongs.Count; i++)
            {
                LofiCompany.lofiSongIndexesInQueue.Add(i);
            }
        }

        private static bool IsAPlayerInShiproom()
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].isInHangarShipRoom)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsDiscoAudioPlaying()
        {
            GameObject discoAudioObject = GameObject.Find("DiscoBallContainer(Clone)/AnimContainer/Audio")
                ?? GameObject.Find("DiscoBallContainer/AnimContainer/Audio");
            if (discoAudioObject != null)
            {
                AudioSource discoAudioSource = discoAudioObject.GetComponent<AudioSource>();
                return discoAudioSource != null && discoAudioSource.isPlaying;
            }

            return false;
        }

        internal static void PlayLofiSongClient(int songIndex, ulong clientId)
        {
            StartOfRound startOfRound = StartOfRound.Instance;

            AudioSource speakers = startOfRound.speakerAudioSource;
            AudioClip song = LofiCompany.allLofiSongs[songIndex];

            if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
            {
                startOfRound.StartCoroutine(AudioUtils.FadeOutMusicSource(TimeOfDay.Instance.TimeOfDayMusic));
            }

            speakers.Stop();
            speakers.volume = 0f;
            speakers.PlayOneShot(song);

            LofiCompany.lofiSongIndexesInQueue.Remove(songIndex);

            int[] time = GetCurrentHoursAndMinutes();
            LofiCompany.Logger.LogInfo($"Hello and good {TimeOfDay.Instance.dayMode}! It is {time[0]}:{time[1]} and the company wants to play some music.");
            LofiCompany.Logger.LogInfo($"The ship's speakers are now playing track {songIndex}: {song.name}");
        }

        internal static void FadeInShipSpeakerSFXSourceClient(string audioSourceName, ulong clientId)
        {
            StartOfRound startOfRound = StartOfRound.Instance;
            AudioSource audioSource = startOfRound.speakerAudioSource;
            float initVolume = 1f;

            if (audioSourceName == "speakers")
            {
                audioSource = startOfRound.speakerAudioSource;
                initVolume = LofiCompany.lofiConfigs.musicVolume;
            }

            startOfRound.StartCoroutine(AudioUtils.FadeInMusicSource(audioSource, initVolume));
        }

        internal static void FadeOutSFXSourceClient(string audioSourceName, ulong clientId)
        {
            StartOfRound startOfRound = StartOfRound.Instance;
            AudioSource audioSource = startOfRound.speakerAudioSource;

            if (audioSourceName == "speakers")
            {
                audioSource = startOfRound.speakerAudioSource;
            }

            startOfRound.StartCoroutine(AudioUtils.FadeOutMusicSource(audioSource));
        }

        internal static void SetIsSpeakerVolStandartVolClient(bool isSVSV, ulong clientId)
        {
            isSpeakerVolStandartVol = isSVSV;
        }
    }
}
