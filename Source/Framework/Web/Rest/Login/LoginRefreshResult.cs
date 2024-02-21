// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Login
{
    public class LoginRefreshResult
    {
        [JsonPropertyName("login_ticket_expiry")]
        public long LoginTicketExpiry { get; set; }

        [JsonPropertyName("is_expired")]
        public bool IsExpired { get; set; }
    }
}
