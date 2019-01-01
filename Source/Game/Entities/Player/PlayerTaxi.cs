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

using Framework.Collections;
using Framework.Constants;
using Game.DataStorage;
using Game.Network.Packets;
using System.Collections.Generic;
using System.Text;

namespace Game.Entities
{
    public class PlayerTaxi
    {
        public void InitTaxiNodesForLevel(Race race, Class chrClass, uint level)
        {
            // class specific initial known nodes
            var factionMask = Player.TeamForRace(race) == Team.Horde ? CliDB.HordeTaxiNodesMask : CliDB.AllianceTaxiNodesMask;
            switch (chrClass)
            {
                case Class.Deathknight:
                    {
                        for (int i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                            m_taximask[i] |= (byte)(CliDB.OldContinentsNodesMask[i] & factionMask[i]);
                        break;
                    }
            }

            // race specific initial known nodes: capital and taxi hub masks
            switch (race)
            {
                case Race.Human:
                case Race.Dwarf:
                case Race.NightElf:
                case Race.Gnome:
                case Race.Draenei:
                case Race.Worgen:
                case Race.PandarenAlliance:
                    SetTaximaskNode(2);     // Stormwind, Elwynn
                    SetTaximaskNode(6);     // Ironforge, Dun Morogh
                    SetTaximaskNode(26);    // Lor'danel, Darkshore
                    SetTaximaskNode(27);    // Rut'theran Village, Teldrassil
                    SetTaximaskNode(49);    // Moonglade (Alliance)
                    SetTaximaskNode(94);    // The Exodar
                    SetTaximaskNode(456);   // Dolanaar, Teldrassil
                    SetTaximaskNode(457);   // Darnassus, Teldrassil
                    SetTaximaskNode(582);   // Goldshire, Elwynn
                    SetTaximaskNode(589);   // Eastvale Logging Camp, Elwynn
                    SetTaximaskNode(619);   // Kharanos, Dun Morogh
                    SetTaximaskNode(620);   // Gol'Bolar Quarry, Dun Morogh
                    SetTaximaskNode(624);   // Azure Watch, Azuremyst Isle
                    break;
                case Race.Orc:
                case Race.Undead:
                case Race.Tauren:
                case Race.Troll:
                case Race.BloodElf:
                case Race.Goblin:
                case Race.PandarenHorde:
                    SetTaximaskNode(11);    // Undercity, Tirisfal
                    SetTaximaskNode(22);    // Thunder Bluff, Mulgore
                    SetTaximaskNode(23);    // Orgrimmar, Durotar
                    SetTaximaskNode(69);    // Moonglade (Horde)
                    SetTaximaskNode(82);    // Silvermoon City
                    SetTaximaskNode(384);   // The Bulwark, Tirisfal
                    SetTaximaskNode(402);   // Bloodhoof Village, Mulgore
                    SetTaximaskNode(460);   // Brill, Tirisfal Glades
                    SetTaximaskNode(536);   // Sen'jin Village, Durotar
                    SetTaximaskNode(537);   // Razor Hill, Durotar
                    SetTaximaskNode(625);   // Fairbreeze Village, Eversong Woods
                    SetTaximaskNode(631);   // Falconwing Square, Eversong Woods
                    break;
            }

            // new continent starting masks (It will be accessible only at new map)
            switch (Player.TeamForRace(race))
            {
                case Team.Alliance:
                    SetTaximaskNode(100);
                    break;
                case Team.Horde:
                    SetTaximaskNode(99);
                    break;
            }
            // level dependent taxi hubs
            if (level >= 68)
                SetTaximaskNode(213);                               //Shattered Sun Staging Area
        }

        public void LoadTaxiMask(string data)
        {
            var split = new StringArray(data, ' ');

            int index = 0;
            for (var i = 0; index < PlayerConst.TaxiMaskSize && i != split.Length; ++i, ++index)
            {
                // load and set bits only for existing taxi nodes
                if (uint.TryParse(split[i], out uint id))
                    m_taximask[index] = (byte)(CliDB.TaxiNodesMask[index] & id);
            }
        }

        public void AppendTaximaskTo(ShowTaxiNodes data, bool all)
        {
            if (all)
            {
                data.CanLandNodes = CliDB.TaxiNodesMask;              // all existed nodes
                data.CanUseNodes = CliDB.TaxiNodesMask;
            }
            else
            {
                data.CanLandNodes = m_taximask;                  // known nodes
                data.CanUseNodes = m_taximask;
            }
        }

        public bool LoadTaxiDestinationsFromString(string values, Team team)
        {
            ClearTaxiDestinations();

            var stringArray = new StringArray(values, ' ');
            if (stringArray.Length > 0)
                uint.TryParse(stringArray[0], out m_flightMasterFactionId);

            for (var i = 1; i < stringArray.Length; ++i)
            {
                if (uint.TryParse(stringArray[i], out uint node))
                    AddTaxiDestination(node);
            }

            if (m_TaxiDestinations.Empty())
                return true;

            // Check integrity
            if (m_TaxiDestinations.Count < 2)
                return false;

            for (int i = 1; i < m_TaxiDestinations.Count; ++i)
            {
                uint cost;
                uint path;
                Global.ObjectMgr.GetTaxiPath(m_TaxiDestinations[i - 1], m_TaxiDestinations[i], out path, out cost);
                if (path == 0)
                    return false;
            }

            // can't load taxi path without mount set (quest taxi path?)
            if (Global.ObjectMgr.GetTaxiMountDisplayId(GetTaxiSource(), team, true) == 0)
                return false;

            return true;
        }

        public string SaveTaxiDestinationsToString()
        {
            if (m_TaxiDestinations.Empty())
                return "";

            StringBuilder ss = new StringBuilder();
            ss.Append($"{m_flightMasterFactionId} ");

            for (int i = 0; i < m_TaxiDestinations.Count; ++i)
                ss.Append($"{m_TaxiDestinations[i]} ");

            return ss.ToString();
        }

        public uint GetCurrentTaxiPath()
        {
            if (m_TaxiDestinations.Count < 2)
                return 0;

            uint path;
            uint cost;

            Global.ObjectMgr.GetTaxiPath(m_TaxiDestinations[0], m_TaxiDestinations[1], out path, out cost);

            return path;
        }

        public bool RequestEarlyLanding()
        {
            if (m_TaxiDestinations.Count <= 2)
                return false;

            // start from first destination - m_TaxiDestinations[0] is the current starting node
            for (var i = 1; i < m_TaxiDestinations.Count; ++i)
            {
                if (IsTaximaskNodeKnown(m_TaxiDestinations[i]))
                {
                    if (++i == m_TaxiDestinations.Count - 1)
                        return false;   // if we are left with only 1 known node on the path don't change the spline, its our final destination anyway

                    m_TaxiDestinations.RemoveRange(i, m_TaxiDestinations.Count - 1);
                    return true;
                }
            }

            return false;
        }

        public FactionTemplateRecord GetFlightMasterFactionTemplate()
        {
            return CliDB.FactionTemplateStorage.LookupByKey(m_flightMasterFactionId);
        }

        public void SetFlightMasterFactionTemplateId(uint factionTemplateId)
        {
            m_flightMasterFactionId = factionTemplateId;
        }

        public bool IsTaximaskNodeKnown(uint nodeidx)
        {
            uint field = (nodeidx - 1) / 8;
            uint submask = (uint)(1 << (int)((nodeidx - 1) % 8));
            return (m_taximask[field] & submask) == submask;
        }
        public bool SetTaximaskNode(uint nodeidx)
        {
            uint field = (nodeidx - 1) / 8;
            uint submask = (uint)(1 << (int)((nodeidx - 1) % 8));
            if ((m_taximask[field] & submask) != submask)
            {
                m_taximask[field] |= (byte)submask;
                return true;
            }
            else
                return false;
        }

        public void ClearTaxiDestinations() { m_TaxiDestinations.Clear(); }
        public void AddTaxiDestination(uint dest) { m_TaxiDestinations.Add(dest); }
        void SetTaxiDestination(List<uint> nodes)
        {
            m_TaxiDestinations.Clear();
            m_TaxiDestinations.AddRange(nodes);
        }
        public uint GetTaxiSource() { return m_TaxiDestinations.Empty() ? 0 : m_TaxiDestinations[0]; }
        public uint GetTaxiDestination() { return m_TaxiDestinations.Count < 2 ? 0 : m_TaxiDestinations[1]; }
        public uint NextTaxiDestination()
        {
            m_TaxiDestinations.RemoveAt(0);
            return GetTaxiDestination();
        }
        public List<uint> GetPath() { return m_TaxiDestinations; }
        public bool empty() { return m_TaxiDestinations.Empty(); }

        public byte[] m_taximask = new byte[PlayerConst.TaxiMaskSize];
        List<uint> m_TaxiDestinations = new List<uint>();
        uint m_flightMasterFactionId;
    }
}
