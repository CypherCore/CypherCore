/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Rest
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
