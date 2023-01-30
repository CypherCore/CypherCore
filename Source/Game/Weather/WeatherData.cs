// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class WeatherData
    {
        public WeatherSeasonChances[] Data { get; set; } = new WeatherSeasonChances[4];
        public uint ScriptId { get; set; }
    }
}