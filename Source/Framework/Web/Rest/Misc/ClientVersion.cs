// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
