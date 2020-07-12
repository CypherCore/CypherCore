// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmListUpdate
    {
        [DataMember(Name = "update")]
        public RealmEntry Update { get; set; } = new RealmEntry();

        [DataMember(Name = "deleting")]
        public bool Deleting { get; set; }
    }
}
