// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
