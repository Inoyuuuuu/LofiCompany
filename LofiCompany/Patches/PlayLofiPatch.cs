using HarmonyLib;
using LofiCompany.Configs;
using System.Collections;
using UnityEngine;

namespace LofiCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class PlayLofiPatch
    {
        private static System.Random random = new(0);

        private static bool wasShipPowerSurged = false;

        private static int[] hoursMinutes = new int[2];
        private static int currentHour = 0;
        private static int currentMinute = 0;

        private static int chancePerAttempt = LofiConfigs.defaultChancePerAttempt;
        private static int attemptsPerHour = LofiConfigs.attemptsPerHourBaseValue;
        private static float musicVolume = LofiConfigs.defaultMusicVolume;

        private static bool isPlayingLofiSong = false;
        private static bool isAPlayerInShiproom = false;
        private static bool isPlayingMusicNextOpportunity = false;

        [HarmonyPatch(nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        public static void PlayLofiAtRandom(StartOfRound __instance)
        {
            chancePerAttempt = LofiConfigs.Instance.chancePerAttempt;
            attemptsPerHour = LofiConfigs.Instance.attemptsPerHour;
            musicVolume = LofiConfigs.Instance.musicVolume;

            UpdateWasShipPowerSurged();

            hoursMinutes = GetCurrentHoursAndMinutes();
            if (currentHour != hoursMinutes[0])
            {
                currentHour = hoursMinutes[0];
                OnHourPassed();
            }

            if (currentMinute != hoursMinutes[1])
            {
                currentMinute = hoursMinutes[1];
                OnMinutePassed();
            }

            //---- play random song if conditions are met
            if (!isPlayingLofiSong && !__instance.speakerAudioSource.isPlaying && !__instance.shipIsLeaving && !__instance.inShipPhase && IsAPlayerInShiproom())
            {
                LevelWeatherType currentWeather = TimeOfDay.Instance.currentLevelWeather;
                DayMode currentTimeOfDay = TimeOfDay.Instance.dayMode;
                bool isLofiWeather = LofiCompany.lofiWeatherTypes.Contains(currentWeather);
                bool isLofiDaytime = LofiCompany.lofiDayModes.Contains(currentTimeOfDay);


                if (isLofiWeather && isLofiDaytime && isPlayingMusicNextOpportunity)
                {
                    PlayRandomSong(__instance);
                }
            }

            //---- while lofi is playing
            if (isPlayingLofiSong)
            {
                AudioSource speakers = __instance.speakerAudioSource;
                speakers.volume = musicVolume;

                bool isMusicFadeOutNeeded = __instance.inShipPhase || __instance.shipIsLeaving || !isAPlayerInShiproom;

                if (!speakers.isPlaying || isMusicFadeOutNeeded)
                {
                    isPlayingLofiSong = false;

                    if (isMusicFadeOutNeeded)
                    {
                        __instance.StartCoroutine(AudioUtils.FadeOutMusicSource(speakers));
                    }

                    speakers.volume = 1f;
                    isPlayingMusicNextOpportunity = false;

                    LofiCompany.Logger.LogInfo("not playing music anymore");
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

        [HarmonyPatch(nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        public static void OnStartPatch(StartOfRound __instance)
        {
            if (LofiCompany.lofiSongsInQueue.Count != LofiCompany.allLofiSongs.Count)
            {
                ResetMusicQueue();
            }

            LofiConfigs.Instance.ParseLofiConditions();
            random = new System.Random(__instance.randomMapSeed);
        }

        private static bool IsPlayingOnNextOpportunity()
        {
            int rN = random.Next(0, 100);
            return rN < chancePerAttempt;
        }

        //events that get updated hourly
        private static void OnHourPassed()
        {
            currentMinute = 0;
            LofiCompany.Logger.LogInfo("it is: " + currentHour + ":" + currentMinute + " this is the 1. check of the current hour");
            isPlayingMusicNextOpportunity = IsPlayingOnNextOpportunity();
            isAPlayerInShiproom = IsAPlayerInShiproom();
        }

        //events that get updated minutely
        private static void OnMinutePassed()
        {
            int targetMinute = (int) (60 / attemptsPerHour);

            for (int i = 0; i < attemptsPerHour; i++)
            {
                if (i * targetMinute == currentMinute)
                {
                    LofiCompany.Logger.LogInfo("it is: " + currentHour + ":" + currentMinute + " this is the " + (i + 2) + ". check of the current hour");

                    isPlayingMusicNextOpportunity = IsPlayingOnNextOpportunity();
                }
            }
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

            LofiCompany.Logger.LogInfo("Now playing: " + song.name);
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
