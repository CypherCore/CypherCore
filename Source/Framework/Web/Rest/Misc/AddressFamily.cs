// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
