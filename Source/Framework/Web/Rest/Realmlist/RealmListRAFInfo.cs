// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web
{
    internal class RealmListRAFInfo
    {
        [JsonPropertyName("wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [JsonPropertyName("faction")]
        public int Faction { get; set; }
    }
}
