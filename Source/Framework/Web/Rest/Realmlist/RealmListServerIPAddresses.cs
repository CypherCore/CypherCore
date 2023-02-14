// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListServerIPAddresses
    {
        [DataMember(Name = "families")]
        public IList<AddressFamily> Families { get; set; } = new List<AddressFamily>();
    }
}
