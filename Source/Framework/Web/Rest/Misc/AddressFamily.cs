// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class AddressFamily
    {
        [DataMember(Name = "family")]
        public int Id { get; set; }

        [DataMember(Name = "addresses")]
        public IList<Address> Addresses { get; set; } = new List<Address>();
    }
}
