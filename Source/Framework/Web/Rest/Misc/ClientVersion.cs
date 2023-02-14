// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class ClientVersion
    {
        [DataMember(Name = "versionMajor")]
        public int Major { get; set; }

        [DataMember(Name = "versionBuild")]
        public int Build { get; set; }

        [DataMember(Name = "versionMinor")]
        public int Minor { get; set; }

        [DataMember(Name = "versionRevision")]
        public int Revision { get; set; }
    }
}
