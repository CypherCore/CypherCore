// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class FormInputs
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "prompt")]
        public string Prompt { get; set; }

        [DataMember(Name = "inputs")]
        public List<FormInput> Inputs { get; set; } = new List<FormInput>();
    }
}
