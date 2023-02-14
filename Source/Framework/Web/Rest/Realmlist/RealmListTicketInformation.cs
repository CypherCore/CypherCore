// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListTicketInformation
    {
        [DataMember(Name = "platform")]
        public int Platform { get; set; }

        [DataMember(Name = "buildVariant")]
        public string BuildVariant { get; set; }

        [DataMember(Name = "type")]
        public int Type { get; set; }

        [DataMember(Name = "timeZone")]
        public string Timezone { get; set; }

        [DataMember(Name = "currentTime")]
        public int CurrentTime { get; set; }

        [DataMember(Name = "textLocale")]
        public int TextLocale { get; set; }

        [DataMember(Name = "audioLocale")]
        public int AudioLocale { get; set; }

        [DataMember(Name = "versionDataBuild")]
        public int VersionDataBuild { get; set; }

        [DataMember(Name = "version")]
        public ClientVersion ClientVersion { get; set; } = new ClientVersion();

        [DataMember(Name = "secret")]
        public List<int> Secret { get; set; }

        [DataMember(Name = "clientArch")]
        public int ClientArch { get; set; }

        [DataMember(Name = "systemVersion")]
        public string SystemVersion { get; set; }

        [DataMember(Name = "platformType")]
        public int PlatformType { get; set; }

        [DataMember(Name = "systemArch")]
        public int SystemArch { get; set; }
    }
}
