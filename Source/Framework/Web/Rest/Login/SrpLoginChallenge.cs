// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Login
{
    public class SrpLoginChallenge
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("iterations")]
        public int Iterations { get; set; }

        [JsonPropertyName("modulus")]
        public string Modulus { get; set; }

        [JsonPropertyName("generator")]
        public string Generator { get; set; }

        [JsonPropertyName("hash_function")]
        public string HashFunction { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("salt")]
        public string Salt { get; set; }

        [JsonPropertyName("public_B")]
        public string PublicB { get; set; }

        [JsonPropertyName("eligible_credential_upgrade")]
        public bool EligibleCredentialUpgrade { get; set; }
    }
}
