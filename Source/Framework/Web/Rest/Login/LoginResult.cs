// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Login
{
    public class LoginResult
    {
        [JsonPropertyName("authentication_state")]
        public string AuthenticationState { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("login_ticket")]
        public string LoginTicket { get; set; }

        [JsonPropertyName("server_evidence_M2")]
        public string ServerEvidenceM2 { get; set; }
    }

    public enum AuthenticationState
    {
        NONE = 0,
        LOGIN = 1,
        LEGAL = 2,
        AUTHENTICATOR = 3,
        DONE = 4,
    }
}
