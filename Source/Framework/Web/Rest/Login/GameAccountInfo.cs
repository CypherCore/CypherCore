// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Login
{
    public class GameAccountInfo
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("expansion")]
        public int Expansion { get; set; }

        [JsonPropertyName("is_suspended")]
        public bool IsSuspended { get; set; }

        [JsonPropertyName("is_banned")]
        public bool IsBanned { get; set; }

        [JsonPropertyName("suspension_expires")]
        public long SuspensionExpires { get; set; }

        [JsonPropertyName("suspension_reason")]
        public string SuspensionReason { get; set; }
    }
}
