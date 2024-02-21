// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Framework.Web.Rest.Realmlist;

namespace Framework.Web
{
    public class RealmEntry
    {
        [JsonPropertyName("wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [JsonPropertyName("cfgTimezonesID")]
        public int CfgTimezonesID { get; set; }

        [JsonPropertyName("populationState")]
        public int PopulationState { get; set; }

        [JsonPropertyName("cfgCategoriesID")]
        public int CfgCategoriesID { get; set; }

        [JsonPropertyName("version")]
        public ClientVersion Version { get; set; } = new ClientVersion();

        [JsonPropertyName("cfgRealmsID")]
        public int CfgRealmsID { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cfgConfigsID")]
        public int CfgConfigsID { get; set; }

        [JsonPropertyName("cfgLanguagesID")]
        public int CfgLanguagesID { get; set; }
    }
}
