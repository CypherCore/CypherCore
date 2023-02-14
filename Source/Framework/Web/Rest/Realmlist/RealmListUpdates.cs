// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListUpdates
    {
        [DataMember(Name = "updates")]
        public IList<RealmListUpdate> Updates { get; set; } = new List<RealmListUpdate>();
    }
}
