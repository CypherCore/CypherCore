/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Runtime.Serialization;

namespace Framework.Rest
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
