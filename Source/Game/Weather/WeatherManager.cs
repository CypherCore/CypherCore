// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IWeather;
using System.Collections.Generic;

namespace Game
{
    public class WeatherManager : Singleton<WeatherManager>
    {
        WeatherManager() { }

        public void LoadWeatherData()
        {
            uint oldMSTime = Time.GetMSTime();

            uint count = 0;

            SQLResult result = DB.World.Query("SELECT zone, spring_rain_chance, spring_snow_chance, spring_storm_chance," +
                "summer_rain_chance, summer_snow_chance, summer_storm_chance, fall_rain_chance, fall_snow_chance, fall_storm_chance," +
                "winter_rain_chance, winter_snow_chance, winter_storm_chance, ScriptName FROM game_weather");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 weather definitions. DB table `game_weather` is empty.");
                return;
            }

            do
            {
                uint zone_id = result.Read<uint>(0);

                WeatherData wzc = new();

                for (byte season = 0; season < 4; ++season)
                {
                    wzc.data[season].rainChance = result.Read<byte>(season * (4 - 1) + 1);
                    wzc.data[season].snowChance = result.Read<byte>(season * (4 - 1) + 2);
                    wzc.data[season].stormChance = result.Read<byte>(season * (4 - 1) + 3);

                    if (wzc.data[season].rainChance > 100)
                    {
                        wzc.data[season].rainChance = 25;
                        Log.outError(LogFilter.Sql, "Weather for zone {0} season {1} has wrong rain chance > 100%", zone_id, season);
                    }

                    if (wzc.data[season].snowChance > 100)
                    {
                        wzc.data[season].snowChance = 25;
                        Log.outError(LogFilter.Sql, "Weather for zone {0} season {1} has wrong snow chance > 100%", zone_id, season);
                    }

                    if (wzc.data[season].stormChance > 100)
                    {
                        wzc.data[season].stormChance = 25;
                        Log.outError(LogFilter.Sql, "Weather for zone {0} season {1} has wrong storm chance > 100%", zone_id, season);
                    }
                }

                wzc.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(13));
                _weatherData[zone_id] = wzc;
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} weather definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public WeatherData GetWeatherData(uint zone_id)
        {
            return _weatherData.LookupByKey(zone_id);
        }

        Dictionary<uint, WeatherData> _weatherData = new();
    }

    public class Weather
    {
        public Weather(uint zoneId, WeatherData weatherChances)
        {
            m_zone = zoneId;
            m_weatherChances = weatherChances;
            m_timer.SetInterval(10 * Time.Minute * Time.InMilliseconds);
            m_type = WeatherType.Fine;
            m_intensity = 0;

            //Log.outInfo(LogFilter.General, "WORLD: Starting weather system for zone {0} (change every {1} minutes).", m_zone, (m_timer.GetInterval() / (Time.Minute * Time.InMilliseconds)));
        }

        public bool Update(uint diff)
        {
            if (m_timer.GetCurrent() >= 0)
                m_timer.Update(diff);
            else
                m_timer.SetCurrent(0);

            // If the timer has passed, ReGenerate the weather
            if (m_timer.Passed())
            {
                m_timer.Reset();
                // update only if Regenerate has changed the weather
                if (ReGenerate())
                {
                    // Weather will be removed if not updated (no players in zone anymore)
                    if (!UpdateWeather())
                        return false;
                }
            }

            Global.ScriptMgr.RunScript<IWeatherOnUpdate>(p => p.OnUpdate(this, diff), GetScriptId());
            return true;
        }

        public bool ReGenerate()
        {
            if (m_weatherChances == null)
            {
                m_type = WeatherType.Fine;
                m_intensity = 0.0f;
                return false;
            }

            // Weather statistics:
            // 30% - no change
            // 30% - weather gets better (if not fine) or change weather type
            // 30% - weather worsens (if not fine)
            // 10% - radical change (if not fine)
            uint u = RandomHelper.URand(0, 99);

            if (u < 30)
                return false;

            // remember old values
            WeatherType old_type = m_type;
            float old_intensity = m_intensity;

            long gtime = GameTime.GetGameTime();
            var ltime = Time.UnixTimeToDateTime(gtime).ToLocalTime();
            uint season = (uint)((ltime.DayOfYear - 78 + 365) / 91) % 4;

            string[] seasonName = { "spring", "summer", "fall", "winter" };

            Log.outInfo(LogFilter.Server, "Generating a change in {0} weather for zone {1}.", seasonName[season], m_zone);

            if ((u < 60) && (m_intensity < 0.33333334f))                // Get fair
            {
                m_type = WeatherType.Fine;
                m_intensity = 0.0f;
            }

            if ((u < 60) && (m_type != WeatherType.Fine))          // Get better
            {
                m_intensity -= 0.33333334f;
                return true;
            }

            if ((u < 90) && (m_type != WeatherType.Fine))          // Get worse
            {
                m_intensity += 0.33333334f;
                return true;
            }

            if (m_type != WeatherType.Fine)
            {
                // Radical change:
                // if light . heavy
                // if medium . change weather type
                // if heavy . 50% light, 50% change weather type

                if (m_intensity < 0.33333334f)
                {
                    m_intensity = 0.9999f;                              // go nuts
                    return true;
                }
                else
                {
                    if (m_intensity > 0.6666667f)
                    {
                        // Severe change, but how severe?
                        uint rnd = RandomHelper.URand(0, 99);
                        if (rnd < 50)
                        {
                            m_intensity -= 0.6666667f;
                            return true;
                        }
                    }
                    m_type = WeatherType.Fine;                     // clear up
                    m_intensity = 0;
                }
            }

            // At this point, only weather that isn't doing anything remains but that have weather data
            uint chance1 = m_weatherChances.data[season].rainChance;
            uint chance2 = chance1 + m_weatherChances.data[season].snowChance;
            uint chance3 = chance2 + m_weatherChances.data[season].stormChance;
            uint rn = RandomHelper.URand(1, 100);
            if (rn <= chance1)
                m_type = WeatherType.Rain;
            else if (rn <= chance2)
                m_type = WeatherType.Snow;
            else if (rn <= chance3)
                m_type = WeatherType.Storm;
            else
                m_type = WeatherType.Fine;

            // New weather statistics (if not fine):
            // 85% light
            // 7% medium
            // 7% heavy
            // If fine 100% sun (no fog)

            if (m_type == WeatherType.Fine)
            {
                m_intensity = 0.0f;
            }
            else if (u < 90)
            {
                m_intensity = (float)RandomHelper.NextDouble() * 0.3333f;
            }
            else
            {
                // Severe change, but how severe?
                rn = RandomHelper.URand(0, 99);
                if (rn < 50)
                    m_intensity = (float)RandomHelper.NextDouble() * 0.3333f + 0.3334f;
                else
                    m_intensity = (float)RandomHelper.NextDouble() * 0.3333f + 0.6667f;
            }

            // return true only in case weather changes
            return m_type != old_type || m_intensity != old_intensity;
        }

        public void SendWeatherUpdateToPlayer(Player player)
        {
            WeatherPkt weather = new(GetWeatherState(), m_intensity);
            player.SendPacket(weather);
        }

        public static void SendFineWeatherUpdateToPlayer(Player player)
        {
            player.SendPacket(new WeatherPkt(WeatherState.Fine));
        }

        public bool UpdateWeather()
        {
            Player player = Global.WorldMgr.FindPlayerInZone(m_zone);
            if (player == null)
                return false;

            // Send the weather packet to all players in this zone
            if (m_intensity >= 1)
                m_intensity = 0.9999f;
            else if (m_intensity < 0)
                m_intensity = 0.0001f;

            WeatherState state = GetWeatherState();

            WeatherPkt weather = new(state, m_intensity);

            //- Returns false if there were no players found to update
            if (!Global.WorldMgr.SendZoneMessage(m_zone, weather))
                return false;

            // Log the event
            string wthstr;
            switch (state)
            {
                case WeatherState.Fog:
                    wthstr = "fog";
                    break;
                case WeatherState.LightRain:
                    wthstr = "light rain";
                    break;
                case WeatherState.MediumRain:
                    wthstr = "medium rain";
                    break;
                case WeatherState.HeavyRain:
                    wthstr = "heavy rain";
                    break;
                case WeatherState.LightSnow:
                    wthstr = "light snow";
                    break;
                case WeatherState.MediumSnow:
                    wthstr = "medium snow";
                    break;
                case WeatherState.HeavySnow:
                    wthstr = "heavy snow";
                    break;
                case WeatherState.LightSandstorm:
                    wthstr = "light sandstorm";
                    break;
                case WeatherState.MediumSandstorm:
                    wthstr = "medium sandstorm";
                    break;
                case WeatherState.HeavySandstorm:
                    wthstr = "heavy sandstorm";
                    break;
                case WeatherState.Thunders:
                    wthstr = "thunders";
                    break;
                case WeatherState.BlackRain:
                    wthstr = "blackrain";
                    break;
                case WeatherState.Fine:
                default:
                    wthstr = "fine";
                    break;
            }
            Log.outInfo(LogFilter.Server, "Change the weather of zone {0} to {1}.", m_zone, wthstr);

            Global.ScriptMgr.RunScript<IWeatherOnChange>(p => p.OnChange(this, state, m_intensity), GetScriptId());
            return true;
        }

        public void SetWeather(WeatherType type, float grade)
        {
            if (m_type == type && m_intensity == grade)
                return;

            m_type = type;
            m_intensity = grade;
            UpdateWeather();
        }

        public WeatherState GetWeatherState()
        {
            if (m_intensity < 0.27f)
                return WeatherState.Fine;

            switch (m_type)
            {
                case WeatherType.Rain:
                    if (m_intensity < 0.40f)
                        return WeatherState.LightRain;
                    else if (m_intensity < 0.70f)
                        return WeatherState.MediumRain;
                    else
                        return WeatherState.HeavyRain;
                case WeatherType.Snow:
                    if (m_intensity < 0.40f)
                        return WeatherState.LightSnow;
                    else if (m_intensity < 0.70f)
                        return WeatherState.MediumSnow;
                    else
                        return WeatherState.HeavySnow;
                case WeatherType.Storm:
                    if (m_intensity < 0.40f)
                        return WeatherState.LightSandstorm;
                    else if (m_intensity < 0.70f)
                        return WeatherState.MediumSandstorm;
                    else
                        return WeatherState.HeavySandstorm;
                case WeatherType.BlackRain:
                    return WeatherState.BlackRain;
                case WeatherType.Thunders:
                    return WeatherState.Thunders;
                case WeatherType.Fine:
                default:
                    return WeatherState.Fine;
            }
        }

        public uint GetZone() { return m_zone; }
        public uint GetScriptId() { return m_weatherChances.ScriptId; }

        uint m_zone;
        WeatherType m_type;
        float m_intensity;
        IntervalTimer m_timer = new();
        WeatherData m_weatherChances;
    }

    public class WeatherData
    {
        public WeatherSeasonChances[] data = new WeatherSeasonChances[4];
        public uint ScriptId;
    }

    public struct WeatherSeasonChances
    {
        public uint rainChance;
        public uint snowChance;
        public uint stormChance;
    }

    public enum WeatherState
    {
        Fine = 0,
        Fog = 1, // Used in some instance encounters.
        Drizzle = 2,
        LightRain = 3,
        MediumRain = 4,
        HeavyRain = 5,
        LightSnow = 6,
        MediumSnow = 7,
        HeavySnow = 8,
        LightSandstorm = 22,
        MediumSandstorm = 41,
        HeavySandstorm = 42,
        Thunders = 86,
        BlackRain = 90,
        BlackSnow = 106
    }

    public enum WeatherType
    {
        Fine = 0,
        Rain = 1,
        Snow = 2,
        Storm = 3,
        Thunders = 86,
        BlackRain = 90
    }
}
