using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace LofiCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class PlayLofiPatch
    {
        private static int currentMapSeed = 0;
        private static System.Random random = new System.Random(currentMapSeed);

        private static bool wasShipPowerSurged = false;

        private static int[] hoursMinutes = new int[2];
        private static int currentHour = 0;

        private static int musicPlayingChance = 20;
        private static float musicVolume = 0.15f;
        private static bool isPlayingLofiSong = false;
        private static bool isPlayingMusicNextOpportunity = false;

        [HarmonyPatch(nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        private static void PlayLofiAtRandom(StartOfRound __instance)
        {
            UpdateRandom(__instance);
            UpdateWasShipPowerSurged(__instance);
            hoursMinutes = GetCurrentHoursAndMinutes();

            if (currentHour != hoursMinutes[0])
            {
                currentHour = hoursMinutes[0];
                OnHourPassed();
            }


            //---- play random song if conditions are met
            if (!isPlayingLofiSong && !__instance.speakerAudioSource.isPlaying && !__instance.inShipPhase)
            {
                LevelWeatherType currentWeather = TimeOfDay.Instance.currentLevelWeather;
                DayMode currentTimeOfDay = TimeOfDay.Instance.dayMode;
                bool isLofiWeather = LofiCompany.lofiWeatherTypes.Contains(currentWeather);
                bool isLofiDaytime = LofiCompany.lofiDayModes.Contains(currentTimeOfDay);


                if (isLofiWeather && isLofiDaytime && isPlayingMusicNextOpportunity)
                {
                    PlayRandomSong(__instance, random);
                }
            }

            //---- while lofi is playing

            if (isPlayingLofiSong)
            {
                AudioSource speakers = __instance.speakerAudioSource;
                speakers.volume = musicVolume;

                if (!speakers.isPlaying || __instance.inShipPhase)
                {
                    isPlayingLofiSong = false;
                    speakers.Stop();
                    speakers.volume = 1f;
                }

                if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
                {
                    TimeOfDay.Instance.TimeOfDayMusic.Stop();
                }
            }
        }

        [HarmonyPatch(nameof(StartOfRound.PowerSurgeShip))]
        [HarmonyPrefix]
        private static void ShipPowerSurgeListener()
        {
            wasShipPowerSurged = true;
        }

        private static bool IsPlayingOnNextOpportunity(System.Random random)
        {
            int[] hoursMinutes = GetCurrentHoursAndMinutes();
            int hour = hoursMinutes[0];
            int minute = hoursMinutes[1];
            int rN = random.Next(0, 100);

            LofiCompany.Logger.LogInfo(rN + "<" + musicPlayingChance);
            return rN < musicPlayingChance;
        }

        //events that get updated hourly
        private static void OnHourPassed()
        {
            isPlayingMusicNextOpportunity = IsPlayingOnNextOpportunity(random);
        }

        private static int[] GetCurrentHoursAndMinutes()
        {
            TimeOfDay timeOfDay = TimeOfDay.Instance;

            float planetTimeOfDay = timeOfDay.CalculatePlanetTime(timeOfDay.currentLevel);
            float timeOfDayInHours = planetTimeOfDay / timeOfDay.lengthOfHours;

            int[] hoursMinutes = [(int)timeOfDayInHours + 6, (int)((timeOfDayInHours - (int)timeOfDayInHours) * 60)];
            return hoursMinutes;
        }

        private static void PlayRandomSong(StartOfRound startOfRound, System.Random random)
        {
            if (TimeOfDay.Instance.TimeOfDayMusic.isPlaying)
            {
                startOfRound.StartCoroutine(FadeOutMusicSource(TimeOfDay.Instance.TimeOfDayMusic));
            }

            AudioSource speakers = startOfRound.speakerAudioSource;
            AudioClip song = LofiCompany.lofiSongs[random.Next(0, LofiCompany.lofiSongs.Count - 1)];

            speakers.volume = musicVolume;
            speakers.PlayOneShot(song);
            isPlayingLofiSong = true;
        }

        private static void UpdateWasShipPowerSurged(StartOfRound startOfRound)
        {
            if (wasShipPowerSurged && startOfRound.shipRoomLights.areLightsOn)
            {
                wasShipPowerSurged = false;
            }
        }

        private static void UpdateRandom(StartOfRound startOfRound)
        {
            if (currentMapSeed != startOfRound.randomMapSeed)
            {
                currentMapSeed = startOfRound.randomMapSeed;
                random = new System.Random(startOfRound.randomMapSeed);
            }
        }

        private static IEnumerator FadeOutMusicSource(AudioSource audioSource)
        {
            float initialVolume = audioSource.volume;
            while (audioSource.volume > 0.1f)
            {
                LofiCompany.Logger.LogInfo(audioSource.volume);
                audioSource.volume -= 0.1f;
                yield return new WaitForSeconds(0.2f);
            }
            audioSource.Stop();
            audioSource.volume = initialVolume;
            LofiCompany.Logger.LogInfo("after while " + audioSource.volume);
            yield break;
        }
    }
}
