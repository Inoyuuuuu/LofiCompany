using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Collections;

namespace LofiCompany.Configs
{
    [DataContract]
    internal class LofiConfigs : SyncedConfig<LofiConfigs>
    {
        private const string CONDITIONS_SEPARATOR = ", ";
        private const string CONDITIONS_KEYWORD_ALL = "ALL";
        private const string CONDITIONS_KEYWORD_DEFAULT = "DEFAULT";

        private static Dictionary<string, LevelWeatherType> weatherKeywords = [];
        private static Dictionary<string, DayMode> dayModeKeywords = [];
        internal static bool areAllWeatherTypesAccepted = false;
        internal static bool areAllDayModesAccepted = false;

        internal const string defaultLofiWeatherTypes = "RAINY, STORMY, FLOODED";
        internal const string defaultLofiDaytimes = "NOON, SUNDOWN";
        internal const int attemptsPerHourBaseValue = 2;
        internal const int defaultChancePerAttempt = 15;
        internal const float defaultMusicVolume = 0.2f;
        internal const float defaultPlayerLeaveShipTimer = 15f;

        [DataMember]
        internal SyncedEntry<string> dayModes, weatherTypes;
        [DataMember]
        internal SyncedEntry<float> playerLeaveShipTimer;
        [DataMember]
        internal SyncedEntry<int> attemptsPerHour, chancePerAttempt;
        [DataMember]
        internal SyncedEntry<float> musicVolume;

        internal LofiConfigs(ConfigFile cfg) : base(MyPluginInfo.PLUGIN_NAME)
        {
            ConfigManager.Register(this);

            InitWeatherKeywords();
            InitDayModeKeywords();

            weatherTypes = cfg.BindSyncedEntry("LofiConditions", "weatherTypes", defaultLofiWeatherTypes, "Lofi music will only be played under these weather conditions. \n" +
                "Please write the weather types in caps and separated by a comma and a whitespace (eg. 'RAINY, FOGGY'). \n" +
                "Following input will be accepted: RAINY, STORMY, FLOODED, ECLIPSED, FOGGY, DUSTCLOUDS, NORMAL, ALL, DEFAULT (with 'NORMAL' being the standard sunny day)");
            
            dayModes = cfg.BindSyncedEntry("LofiConditions", "dayModes", defaultLofiDaytimes, "Lofi music will only be played at these times of the day. \n" +
                "Please write the day types in caps and separated by a comma and a whitespace (eg. 'NOONE, SUNRISE'). \n" +
                "Following input will be accepted: SUNRISE, NOON, SUNDOWN, MIDNIGHT, ALL, DEFAULT");
            playerLeaveShipTimer = cfg.BindSyncedEntry("LofiConditions", "playerLeaveShipTimer", defaultPlayerLeaveShipTimer, "If all players leave the shiproom, after this amount of seconds lofi will stop playing.");
            attemptsPerHour = cfg.BindSyncedEntry("LofiMusicChance", "attemptsPerHour", attemptsPerHourBaseValue, "This determines how often the company will try to put on some lofi music per hour (if LofiConditions are met). \nOne try per hour is the minimum.");
            chancePerAttempt = cfg.BindSyncedEntry("LofiMusicChance", "chancePerAttempt", defaultChancePerAttempt, "This determines how likely it is that lofi will be played (per attempt). \nThis Value is a percentage, so 100 is the max value.");
            musicVolume = cfg.BindSyncedEntry("LofiMusic", "musicVolume", defaultMusicVolume, "This is the music volume. \nMax volume is at 1, min is 0.1.");
            //ambienceVolumeReduction = cfg.BindSyncedEntry("LofiMusic", "ambienceVolumeReduction", defaultAmbienceAudioVolumeReduction, "This decreases the ambience sounds (stuff like weather noises) if music is playing. \nThis is in percent, so ambience audio will play at the given percentage (eg. 80 --> ambience music set to 80% of its original volume).");

            FixConfigs();
        }

        private void FixConfigs()
        {
            if (attemptsPerHour.Value < 1)
            {
                attemptsPerHour.Value = 1;
            }
            else if (attemptsPerHour.Value > 59)
            {
                attemptsPerHour.Value = 59;
            }

            if (chancePerAttempt.Value < 0)
            {
                chancePerAttempt.Value = 0;
            }
            else if (chancePerAttempt.Value > 100)
            {
                chancePerAttempt.Value = 100;
            }

            if (musicVolume.Value < 0.01f)
            {
                musicVolume.Value = 0.01f;
            }
            else if (musicVolume.Value > 1f)
            {
                musicVolume.Value = 1f;
            }

            if (playerLeaveShipTimer.Value > 9999)
            {
                playerLeaveShipTimer.Value = 9999;
            }
            else if (playerLeaveShipTimer.Value <= 1)
            {
                playerLeaveShipTimer.Value = 1;
            }
        }

        private void InitWeatherKeywords()
        {
            weatherKeywords ??= [];
            weatherKeywords.Clear();
            weatherKeywords.Add("RAINY", LevelWeatherType.Rainy);
            weatherKeywords.Add("STORMY", LevelWeatherType.Stormy);
            weatherKeywords.Add("FLOODED", LevelWeatherType.Flooded);
            weatherKeywords.Add("ECLIPSED", LevelWeatherType.Eclipsed);
            weatherKeywords.Add("FOGGY", LevelWeatherType.Foggy);
            weatherKeywords.Add("DUSTCLOUDS", LevelWeatherType.DustClouds);
            weatherKeywords.Add("NORMAL", LevelWeatherType.None);

        }

        private void InitDayModeKeywords()
        {
            dayModeKeywords ??= [];
            dayModeKeywords.Clear();
            dayModeKeywords.Add("SUNRISE", DayMode.Dawn);
            dayModeKeywords.Add("NOON", DayMode.Noon);
            dayModeKeywords.Add("SUNDOWN", DayMode.Sundown);
            dayModeKeywords.Add("MIDNIGHT", DayMode.Midnight);
        }

        internal void ParseLofiConditions()
        {
            InitWeatherKeywords();
            InitDayModeKeywords();

            ParseWeatherConditions();
            ParseDayModeConditions();
        }

        private void ParseWeatherConditions()
        {
            List<LevelWeatherType> weatherConditions = [];
            List<string> weatherAsStrings = [.. weatherTypes.Value.Split(CONDITIONS_SEPARATOR)];
            areAllWeatherTypesAccepted = false;

            if (weatherAsStrings.Contains(CONDITIONS_KEYWORD_ALL))
            {
                areAllWeatherTypesAccepted = true;
                weatherConditions.AddRange(weatherKeywords.Values);
            } 
            else if (weatherAsStrings.Contains(CONDITIONS_KEYWORD_DEFAULT))
            {
                weatherConditions.Add(LevelWeatherType.Rainy);
                weatherConditions.Add(LevelWeatherType.Stormy);
                weatherConditions.Add(LevelWeatherType.Flooded);
            }
            else
            {
                for (int i = 0; i < weatherAsStrings.Count; i++)
                {
                    if (weatherKeywords.ContainsKey(weatherAsStrings[i]))
                    {
                        weatherConditions.Add(weatherKeywords.Get(weatherAsStrings[i]));
                    } 
                    else
                    {
                        LofiCompany.Logger.LogError("ERROR_02: There was an error with parsing a weather type: \"" + weatherAsStrings[i] + "\". Make sure the weather was typed in correctly!");
                    }
                }
            }

            if (weatherConditions.Count == 0)
            {
                LofiCompany.Logger.LogError("ERROR_03: No weather types were found, will reset to standard ones.");

                weatherTypes.Value = defaultLofiWeatherTypes;

                weatherConditions.Add(LevelWeatherType.Rainy);
                weatherConditions.Add(LevelWeatherType.Stormy);
                weatherConditions.Add(LevelWeatherType.Flooded);
            }

            LofiCompany.lofiWeatherTypes.Clear();
            LofiCompany.lofiWeatherTypes.AddRange(weatherConditions);
        }

        private void ParseDayModeConditions()
        {
            List<DayMode> dayModeConditions = [];
            List<string> dayModesAsStrings = [.. dayModes.Value.Split(CONDITIONS_SEPARATOR)];
            areAllDayModesAccepted = false;

            if (dayModesAsStrings.Contains(CONDITIONS_KEYWORD_ALL))
            {
                areAllDayModesAccepted = true;
                dayModeConditions.AddRange(dayModeKeywords.Values);
            }
            else if (dayModesAsStrings.Contains(CONDITIONS_KEYWORD_DEFAULT))
            {
                dayModeConditions.Add(DayMode.Noon);
                dayModeConditions.Add(DayMode.Sundown);
            }
            else
            {
                for (int i = 0; i < dayModesAsStrings.Count; i++)
                {
                    if (dayModeKeywords.ContainsKey(dayModesAsStrings[i]))
                    {
                        dayModeConditions.Add(dayModeKeywords.Get(dayModesAsStrings[i]));
                    }
                    else
                    {
                        LofiCompany.Logger.LogError("ERROR_04: There was an error with parsing a day mode: \"" + dayModesAsStrings[i] + "\". Make sure the time of day was typed in correctly!");
                    }
                }
            }

            if (dayModeConditions.Count == 0)
            {
                LofiCompany.Logger.LogError("ERROR_05: No day modes were found, will reset to standard ones.");

                dayModes.Value = defaultLofiDaytimes;

                dayModeConditions.Add(DayMode.Noon);
                dayModeConditions.Add(DayMode.Sundown);
            }

            LofiCompany.lofiDayModes.Clear();
            LofiCompany.lofiDayModes.AddRange(dayModeConditions);
        }
    }
}
