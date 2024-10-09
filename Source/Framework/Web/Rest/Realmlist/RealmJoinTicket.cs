// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web
{
    public class RealmJoinTicket
    {
        [JsonPropertyName("gameAccount")]
        public string GameAccount { get; set; }

        [JsonPropertyName("platform")]
        public int Platform { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("clientArch")]
        public int ClientArch { get; set; }
    }
}
