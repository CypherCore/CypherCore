// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class LogonData
    {
        public string this[string inputId] => Inputs.SingleOrDefault(i => i.Id == inputId)?.Value;

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "program_id")]
        public string Program { get; set; }

        [DataMember(Name = "platform_id")]
        public string Platform { get; set; }

        [DataMember(Name = "inputs")]
        public List<FormInputValue> Inputs { get; set; } = new List<FormInputValue>();
    }
}
