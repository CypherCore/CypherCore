// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web.Rest.Realmlist
{
    public class ClientVersion
    {
        [JsonPropertyName("versionMajor")]
        public int Major { get; set; }

        [JsonPropertyName("versionBuild")]
        public int Build { get; set; }

        [JsonPropertyName("versionMinor")]
        public int Minor { get; set; }

        [JsonPropertyName("versionRevision")]
        public int Revision { get; set; }
    }
}
