// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
