using GameNetcodeStuff;
using HarmonyLib;
using LofiCompany.Configs;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace LofiCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class PlayLofiPatch
    {
        private static System.Random random = new(0);

        private static int[] hoursMinutes = new int[2];
        private static int currentHours = 0;
        private static int currentMinutes = 0;

        private static int chancePerAttempt = LofiConfigs.defaultChancePerAttempt;
        private static int attemptsPerHour = LofiConfigs.attemptsPerHourBaseValue;
        private static float musicVolume = LofiConfigs.defaultMusicVolume;
        private static float playerShipLeaveTimer = LofiConfigs.defaultPlayerLeaveShipTimer;

        private static bool wasShipPowerSurged = false;
        private static bool isPlayingLofiSong = false;
        private static bool isPlayingMusicNextOpportunity = false;
        private static bool isShiproomEmpty = false;

        [HarmonyPatch(nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        public static void PlayLofiAtRandom(StartOfRound __instance)
        {
            chancePerAttempt = LofiCompany.lofiConfigs.chancePerAttempt.Value;
            attemptsPerHour = LofiCompany.lofiConfigs.attemptsPerHour.Value;
            musicVolume = LofiCompany.lofiConfigs.musicVolume.Value;


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
                } else
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
                    int checkPointMinutes = (int)(60 / attemptsPerHour);

                    for (int i = 0; i < attemptsPerHour; i++)
                    {
                        if (i * checkPointMinutes == currentMinutes || i * checkPointMinutes == 60)
                        {
                            isPlayingMusicNextOpportunity = IsPlayingOnNextOpportunity();
                        }
                    }

                    if (isPlayingMusicNextOpportunity)
                    {
                        PlayRandomSong(__instance);
                    }
                }
            }

            //------------ while lofi is playing ------------
            if (isPlayingLofiSong)
            {
                AudioSource speakers = __instance.speakerAudioSource;
                speakers.volume = musicVolume;

                bool isMusicFadeOutNeeded = __instance.inShipPhase || __instance.shipIsLeaving || isShiproomEmpty;

                if (!speakers.isPlaying || isMusicFadeOutNeeded)
                {
                    LofiCompany.Logger.LogInfo("stopping music");
                    isPlayingLofiSong = false;

                    if (isMusicFadeOutNeeded)
                    {
                        __instance.StartCoroutine(AudioUtils.FadeOutMusicSource(speakers));
                    }

                    isPlayingMusicNextOpportunity = false;

                }

                if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
                {
                    TimeOfDay.Instance.TimeOfDayMusic.Stop();
                }
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
            if (LofiCompany.lofiSongsInQueue.Count != LofiCompany.allLofiSongs.Count)
            {
                ResetMusicQueue();
            }

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

        private static int[] GetCurrentHoursAndMinutes()
        {
            TimeOfDay timeOfDay = TimeOfDay.Instance;

            float planetTimeOfDay = timeOfDay.CalculatePlanetTime(timeOfDay.currentLevel);
            float timeOfDayInHours = planetTimeOfDay / timeOfDay.lengthOfHours;

            int[] hoursMinutes = [(int)timeOfDayInHours + 6, (int)((timeOfDayInHours - (int)timeOfDayInHours) * 60)];
            return hoursMinutes;
        }

        private static void PlayRandomSong(StartOfRound startOfRound)
        {
            if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
            {
                startOfRound.StartCoroutine(AudioUtils.FadeOutMusicSource(TimeOfDay.Instance.TimeOfDayMusic));
            }

            if (LofiCompany.lofiSongsInQueue.Count <= 0)
            {
                ResetMusicQueue();
            }

            AudioSource speakers = startOfRound.speakerAudioSource;
            AudioClip song = LofiCompany.lofiSongsInQueue[random.Next(0, LofiCompany.lofiSongsInQueue.Count - 1)];

            LofiCompany.lofiSongsInQueue.Remove(song);

            startOfRound.StartCoroutine(AudioUtils.FadeInMusicSource(speakers, musicVolume));
            speakers.volume = 0f;
            speakers.PlayOneShot(song);
            isPlayingLofiSong = true;

            LofiCompany.Logger.LogInfo($"Good {TimeOfDay.Instance.dayMode}! It is {GetTimeAsString()} and the company wants to play some music.");
            LofiCompany.Logger.LogInfo("The ship's speakers are now playing: " + song.name);
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
            LofiCompany.lofiSongsInQueue.Clear();
            LofiCompany.lofiSongsInQueue.AddRange(LofiCompany.allLofiSongs);
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
    }
}
