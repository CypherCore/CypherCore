// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework
{
    public class Timezone
    {
        static Dictionary<uint, TimeSpan> _timezoneOffsetsByHash = InitTimezoneHashDb();

        static Dictionary<uint, TimeSpan> InitTimezoneHashDb()
        {
            // Generate our hash db to match values sent in client authentication packets
            Dictionary<uint, TimeSpan> hashToOffset = new();
            foreach (var sysInfo in TimeZoneInfo.GetSystemTimeZones())
            {
                TimeSpan offsetMinutes = sysInfo.BaseUtcOffset;
                hashToOffset.TryAdd(offsetMinutes.TotalMinutes.ToString().HashFnv1a(), offsetMinutes);
            }

            return hashToOffset;
        }

        static (TimeSpan, string)[] _clientSupportedTimezones =
        {
            (TimeSpan.FromMinutes(-480), "America/Los_Angeles"),
            ( TimeSpan.FromMinutes(-420), "America/Denver" ),
            ( TimeSpan.FromMinutes(-360), "America/Chicago" ),
            ( TimeSpan.FromMinutes(-300), "America/New_York" ),
            ( TimeSpan.FromMinutes(-180), "America/Sao_Paulo" ),
            ( TimeSpan.FromMinutes(0), "Etc/UTC" ),
            ( TimeSpan.FromMinutes(60), "Europe/Paris" ),
            ( TimeSpan.FromMinutes(480), "Asia/Shanghai" ),
            ( TimeSpan.FromMinutes(480), "Asia/Taipei" ),
            ( TimeSpan.FromMinutes(540), "Asia/Seoul" ),
            ( TimeSpan.FromMinutes(600), "Australia/Melbourne" ),
        };

        public static TimeSpan GetOffsetByHash(uint hash)
        {
            if (_timezoneOffsetsByHash.TryGetValue(hash, out var offset))
                return offset;

            return TimeSpan.Zero;
        }

        public static TimeSpan GetSystemZoneOffsetAt(DateTime date)
        {
            return TimeZoneInfo.Local.GetUtcOffset(date);
        }

        public static TimeSpan GetSystemZoneOffset()
        {
            return DateTimeOffset.Now.Offset;
        }

        public static string GetSystemZoneName()
        {
            return TimeZoneInfo.Local.StandardName;
        }

        public static string FindClosestClientSupportedTimezone(string currentTimezone, TimeSpan currentTimezoneOffset)
        {
            // try exact match
            var pair = _clientSupportedTimezones.FirstOrDefault(tz => tz.Item2 == currentTimezone);
            if (!pair.Item2.IsEmpty())
                return pair.Item2;

            // try closest offset
            pair = _clientSupportedTimezones.MinBy(left => left.Item1 - currentTimezoneOffset);

            return pair.Item2;
        }
    }
}
