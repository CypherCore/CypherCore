// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class Address
    {
        [DataMember(Name = "ip")]
        public string Ip { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }
    }
}
