// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class FormInput
    {
        [DataMember(Name = "input_id")]
        public string Id { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "label")]
        public string Label { get; set; }

        [DataMember(Name = "max_length")]
        public int MaxLength { get; set; }
    }
}
