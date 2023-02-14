// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListTicketClientInformation
    {
        [DataMember(Name = "info")]
        public RealmListTicketInformation Info { get; set; } = new RealmListTicketInformation();
    }
}
