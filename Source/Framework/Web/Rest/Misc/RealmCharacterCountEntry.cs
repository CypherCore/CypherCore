// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmCharacterCountEntry
    {
        [DataMember(Name = "wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
}
