// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListTicketIdentity
    {
        [DataMember(Name = "gameAccountID")]
        public int GameAccountId { get; set; }

        [DataMember(Name = "gameAccountRegion")]
        public int GameAccountRegion { get; set; }
    }
}
