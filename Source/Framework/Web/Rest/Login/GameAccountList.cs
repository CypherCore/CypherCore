// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Login
{
    public class GameAccountList
    {
        [JsonPropertyName("game_accounts")]
        public List<GameAccountInfo> GameAccounts { get; set; }
    }
}
