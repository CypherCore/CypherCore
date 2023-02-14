// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web
{
    [DataContract]
    public class RealmEntry
    {

        [DataMember(Name = "wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [DataMember(Name = "cfgTimezonesID")]
        public int CfgTimezonesID { get; set; }

        [DataMember(Name = "populationState")]
        public int PopulationState { get; set; }

        [DataMember(Name = "cfgCategoriesID")]
        public int CfgCategoriesID { get; set; }

        [DataMember(Name = "version")]
        public ClientVersion Version { get; set; } = new ClientVersion();

        [DataMember(Name = "cfgRealmsID")]
        public int CfgRealmsID { get; set; }

        [DataMember(Name = "flags")]
        public int Flags { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "cfgConfigsID")]
        public int CfgConfigsID { get; set; }

        [DataMember(Name = "cfgLanguagesID")]
        public int CfgLanguagesID { get; set; }
    }
}
