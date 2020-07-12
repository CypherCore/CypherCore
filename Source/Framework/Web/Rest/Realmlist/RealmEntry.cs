// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
