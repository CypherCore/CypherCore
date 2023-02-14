// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class FormInputValue
    {
        [DataMember(Name = "input_id")]
        public string Id { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }
    }
}
