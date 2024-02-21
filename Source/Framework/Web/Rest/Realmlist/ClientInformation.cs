// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Framework.Web.Rest.Realmlist;

namespace Framework.Web
{
    public class ClientInformation
    {
        [JsonPropertyName("platform")]
        public int Platform { get; set; }

        [JsonPropertyName("buildVariant")]
        public string BuildVariant { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("timeZone")]
        public string Timezone { get; set; }

        [JsonPropertyName("currentTime")]
        public int CurrentTime { get; set; }

        [JsonPropertyName("textLocale")]
        public int TextLocale { get; set; }

        [JsonPropertyName("audioLocale")]
        public int AudioLocale { get; set; }

        [JsonPropertyName("versionDataBuild")]
        public int VersionDataBuild { get; set; }

        [JsonPropertyName("version")]
        public ClientVersion ClientVersion { get; set; } = new ClientVersion();

        [JsonPropertyName("secret")]
        public List<int> Secret { get; set; }

        [JsonPropertyName("clientArch")]
        public int ClientArch { get; set; }

        [JsonPropertyName("systemVersion")]
        public string SystemVersion { get; set; }

        [JsonPropertyName("platformType")]
        public int PlatformType { get; set; }

        [JsonPropertyName("systemArch")]
        public int SystemArch { get; set; }
    }
}
