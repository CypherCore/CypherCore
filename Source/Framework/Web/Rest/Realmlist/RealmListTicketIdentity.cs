// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web
{
    public class RealmListTicketIdentity
    {
        [JsonPropertyName("gameAccountID")]
        public int GameAccountId { get; set; }

        [JsonPropertyName("gameAccountRegion")]
        public int GameAccountRegion { get; set; }
    }
}
