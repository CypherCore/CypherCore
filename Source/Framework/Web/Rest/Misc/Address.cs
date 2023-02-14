// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class Address
    {
        [DataMember(Name = "ip")]
        public string Ip { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }
    }
}
