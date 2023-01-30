// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Maps
{
    public class ZoneDynamicInfo
    {
        public struct LightOverride
        {
            public uint AreaLightId;
            public uint OverrideLightId;
            public uint TransitionMilliseconds;
        }

        public Weather DefaultWeather { get; set; }
        public float Intensity { get; set; }
        public List<LightOverride> LightOverrides { get; set; } = new();
        public uint MusicId { get; set; }
        public WeatherState WeatherId { get; set; }
    }
}