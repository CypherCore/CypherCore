// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Realmlist
{
    public class BleepProxy
    {
        [JsonPropertyName("ping_token_valid_duration")]
        public long PingTokenValidDuration;

        [JsonPropertyName("ping_port")]
        public long PingPort;

        [JsonPropertyName("address")]
        public string Address;

        [JsonPropertyName("ping_token")]
        public string PingToken;

        [JsonPropertyName("port")]
        public long Port;

        [JsonPropertyName("proxy_id")]
        public string ProxyId;
    }
}
