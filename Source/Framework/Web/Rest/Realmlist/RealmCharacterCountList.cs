// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmCharacterCountList
    {
        [DataMember(Name = "counts")]
        public IList<RealmCharacterCountEntry> Counts { get; set; } = new List<RealmCharacterCountEntry>();
    }
}
