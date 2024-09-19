using BepInEx.Configuration;
using CSync.Lib;
using CSync.Extensions;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.UIElements.Collections;

namespace LofiCompany.Configs
{
    [DataContract]
    internal class LofiConfigs : SyncedConfig2<LofiConfigs>
    {
        private const string CONDITIONS_SEPARATOR = ", ";
        private const string CONDITIONS_KEYWORD_ALL = "ALL";
        private const string CONDITIONS_KEYWORD_DEFAULT = "DEFAULT";
        private const int MIN_ATTEMPTS_PER_HOUR = 1;
        private const int MAX_ATTEMPTS_PER_HOUR = 59;
        private const int MIN_CHANCE = 0;
        private const int MAX_CHANCE = 100;
        private const float MIN_VOLUME = 0.01f;
        private const float MAX_VOLUME = 1f;
        private const float MIN_LEAVE_TIMER = 1f;
        private const float MAX_LEAVE_TIMER = 9999f;

        private static Dictionary<string, LevelWeatherType> weatherKeywords = [];
        private static Dictionary<string, DayMode> dayModeKeywords = [];
        internal static bool areAllWeatherTypesAccepted = false;
        internal static bool areAllDayModesAccepted = false;

        internal const string defaultLofiWeatherTypes = "RAINY, STORMY, FLOODED";
        internal const string defaultLofiDaytimes = "NOON, SUNDOWN";
        internal const int attemptsPerHourBaseValue = 1;
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
        [DataMember]
        internal SyncedEntry<bool> isLofiStopActive;

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

            playerLeaveShipTimer = cfg.BindSyncedEntry("LofiConditions", "playerLeaveShipTimer", defaultPlayerLeaveShipTimer, 
                new ConfigDescription("If all players leave the shiproom, after this amount of seconds lofi will stop playing.", 
                new AcceptableValueRange<float>(MIN_LEAVE_TIMER, MAX_LEAVE_TIMER)));
            attemptsPerHour = cfg.BindSyncedEntry("LofiMusicChance", "attemptsPerHour", attemptsPerHourBaseValue,
                new ConfigDescription("This determines how often the company will try to put on some lofi music per hour (if LofiConditions are met). \nOne try per hour is the minimum.", 
                new AcceptableValueRange<int>(MIN_ATTEMPTS_PER_HOUR, MAX_ATTEMPTS_PER_HOUR)));
            chancePerAttempt = cfg.BindSyncedEntry("LofiMusicChance", "chancePerAttempt", defaultChancePerAttempt, 
                new ConfigDescription("This determines how likely it is that lofi will be played (per attempt). \nThis Value is a percentage, so 100 is the max value.", 
                new AcceptableValueRange<int>(MIN_CHANCE, MAX_CHANCE)));
            musicVolume = cfg.BindSyncedEntry("LofiMusic", "musicVolume", defaultMusicVolume, 
                new ConfigDescription("This is the music volume. \nMax volume is at 1, min is 0.1.",
                new AcceptableValueRange<float>(MIN_VOLUME, MAX_VOLUME)));

            isLofiStopActive = cfg.BindSyncedEntry("LofiConditions", "dontPlayLofiAfterStop", false, "If enabled, LofiMusic will stop and not play for the rest of the day after turning off the speaker.");
            //ambienceVolumeReduction = cfg.BindSyncedEntry("LofiMusic", "ambienceVolumeReduction", defaultAmbienceAudioVolumeReduction, "This decreases the ambience sounds (stuff like weather noises) if music is playing. \nThis is in percent, so ambience audio will play at the given percentage (eg. 80 --> ambience music set to 80% of its original volume).");
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

                dayModeConditions.Add(DayMode.Noon);
                dayModeConditions.Add(DayMode.Sundown);
            }

            LofiCompany.lofiDayModes.Clear();
            LofiCompany.lofiDayModes.AddRange(dayModeConditions);
        }
    }
}
