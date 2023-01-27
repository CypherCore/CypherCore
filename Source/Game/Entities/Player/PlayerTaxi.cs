// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Collections;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking.Packets;

namespace Game.Entities
{
	public class PlayerTaxi
	{
		private uint _flightMasterFactionId;
		private List<uint> _TaxiDestinations = new();
		public byte[] _taximask;

		public void InitTaxiNodesForLevel(Race race, Class chrClass, uint level)
		{
			_taximask = new byte[((CliDB.TaxiNodesStorage.GetNumRows() - 1) / 8) + 1];

			// class specific initial known nodes
			if (chrClass == Class.Deathknight)
			{
				var factionMask = Player.TeamForRace(race) == Team.Horde ? CliDB.HordeTaxiNodesMask : CliDB.AllianceTaxiNodesMask;
				_taximask = new byte[factionMask.Length];

				for (int i = 0; i < factionMask.Length; ++i)
					_taximask[i] |= (byte)(CliDB.OldContinentsNodesMask[i] & factionMask[i]);
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
					SetTaximaskNode(2);   // Stormwind, Elwynn
					SetTaximaskNode(6);   // Ironforge, Dun Morogh
					SetTaximaskNode(26);  // Lor'danel, Darkshore
					SetTaximaskNode(27);  // Rut'theran Village, Teldrassil
					SetTaximaskNode(49);  // Moonglade (Alliance)
					SetTaximaskNode(94);  // The Exodar
					SetTaximaskNode(456); // Dolanaar, Teldrassil
					SetTaximaskNode(457); // Darnassus, Teldrassil
					SetTaximaskNode(582); // Goldshire, Elwynn
					SetTaximaskNode(589); // Eastvale Logging Camp, Elwynn
					SetTaximaskNode(619); // Kharanos, Dun Morogh
					SetTaximaskNode(620); // Gol'Bolar Quarry, Dun Morogh
					SetTaximaskNode(624); // Azure Watch, Azuremyst Isle

					break;
				case Race.Orc:
				case Race.Undead:
				case Race.Tauren:
				case Race.Troll:
				case Race.BloodElf:
				case Race.Goblin:
				case Race.PandarenHorde:
					SetTaximaskNode(11);  // Undercity, Tirisfal
					SetTaximaskNode(22);  // Thunder Bluff, Mulgore
					SetTaximaskNode(23);  // Orgrimmar, Durotar
					SetTaximaskNode(69);  // Moonglade (Horde)
					SetTaximaskNode(82);  // Silvermoon City
					SetTaximaskNode(384); // The Bulwark, Tirisfal
					SetTaximaskNode(402); // Bloodhoof Village, Mulgore
					SetTaximaskNode(460); // Brill, Tirisfal Glades
					SetTaximaskNode(536); // Sen'jin Village, Durotar
					SetTaximaskNode(537); // Razor Hill, Durotar
					SetTaximaskNode(625); // Fairbreeze Village, Eversong Woods
					SetTaximaskNode(631); // Falconwing Square, Eversong Woods

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
				SetTaximaskNode(213); //Shattered Sun Staging Area
		}

		public void LoadTaxiMask(string data)
		{
			_taximask = new byte[((CliDB.TaxiNodesStorage.GetNumRows() - 1) / 8) + 1];

			var split = new StringArray(data, ' ');

			int index = 0;

			for (var i = 0; index < _taximask.Length && i != split.Length; ++i, ++index)
				// load and set bits only for existing taxi nodes
				if (uint.TryParse(split[i], out uint id))
					_taximask[index] = (byte)(CliDB.TaxiNodesMask[index] & id);
		}

		public void AppendTaximaskTo(ShowTaxiNodes data, bool all)
		{
			data.CanLandNodes = new byte[CliDB.TaxiNodesMask.Length];
			data.CanUseNodes  = new byte[CliDB.TaxiNodesMask.Length];

			if (all)
			{
				Buffer.BlockCopy(CliDB.TaxiNodesMask, 0, data.CanLandNodes, 0, data.CanLandNodes.Length); // all existed nodes
				Buffer.BlockCopy(CliDB.TaxiNodesMask, 0, data.CanUseNodes, 0, data.CanUseNodes.Length);
			}
			else
			{
				Buffer.BlockCopy(_taximask, 0, data.CanLandNodes, 0, data.CanLandNodes.Length); // known nodes
				Buffer.BlockCopy(_taximask, 0, data.CanUseNodes, 0, data.CanUseNodes.Length);
			}
		}

		public bool LoadTaxiDestinationsFromString(string values, Team team)
		{
			ClearTaxiDestinations();

			var stringArray = new StringArray(values, ' ');

			if (stringArray.Length > 0)
				uint.TryParse(stringArray[0], out _flightMasterFactionId);

			for (var i = 1; i < stringArray.Length; ++i)
				if (uint.TryParse(stringArray[i], out uint node))
					AddTaxiDestination(node);

			if (_TaxiDestinations.Empty())
				return true;

			// Check integrity
			if (_TaxiDestinations.Count < 2)
				return false;

			for (int i = 1; i < _TaxiDestinations.Count; ++i)
			{
				uint path;
				Global.ObjectMgr.GetTaxiPath(_TaxiDestinations[i - 1], _TaxiDestinations[i], out path, out _);

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
			if (_TaxiDestinations.Empty())
				return "";

			Cypher.Assert(_TaxiDestinations.Count >= 2);

			StringBuilder ss = new();
			ss.Append($"{_flightMasterFactionId} ");

			for (int i = 0; i < _TaxiDestinations.Count; ++i)
				ss.Append($"{_TaxiDestinations[i]} ");

			return ss.ToString();
		}

		public uint GetCurrentTaxiPath()
		{
			if (_TaxiDestinations.Count < 2)
				return 0;

			uint path;

			Global.ObjectMgr.GetTaxiPath(_TaxiDestinations[0], _TaxiDestinations[1], out path, out _);

			return path;
		}

		public bool RequestEarlyLanding()
		{
			if (_TaxiDestinations.Count <= 2)
				return false;

			// start from first destination - _TaxiDestinations[0] is the current starting node
			for (var i = 1; i < _TaxiDestinations.Count; ++i)
				if (IsTaximaskNodeKnown(_TaxiDestinations[i]))
				{
					if (++i == _TaxiDestinations.Count - 1)
						return false; // if we are left with only 1 known node on the path don't change the spline, its our final destination anyway

					_TaxiDestinations.RemoveRange(i, _TaxiDestinations.Count - i);

					return true;
				}

			return false;
		}

		public FactionTemplateRecord GetFlightMasterFactionTemplate()
		{
			return CliDB.FactionTemplateStorage.LookupByKey(_flightMasterFactionId);
		}

		public void SetFlightMasterFactionTemplateId(uint factionTemplateId)
		{
			_flightMasterFactionId = factionTemplateId;
		}

		public bool IsTaximaskNodeKnown(uint nodeidx)
		{
			uint field   = (nodeidx - 1) / 8;
			uint submask = (uint)(1 << (int)((nodeidx - 1) % 8));

			return (_taximask[field] & submask) == submask;
		}

		public bool SetTaximaskNode(uint nodeidx)
		{
			uint field   = (nodeidx - 1) / 8;
			uint submask = (uint)(1 << (int)((nodeidx - 1) % 8));

			if ((_taximask[field] & submask) != submask)
			{
				_taximask[field] |= (byte)submask;

				return true;
			}
			else
			{
				return false;
			}
		}

		public void ClearTaxiDestinations()
		{
			_TaxiDestinations.Clear();
		}

		public void AddTaxiDestination(uint dest)
		{
			_TaxiDestinations.Add(dest);
		}

		private void SetTaxiDestination(List<uint> nodes)
		{
			_TaxiDestinations.Clear();
			_TaxiDestinations.AddRange(nodes);
		}

		public uint GetTaxiSource()
		{
			return _TaxiDestinations.Empty() ? 0 : _TaxiDestinations[0];
		}

		public uint GetTaxiDestination()
		{
			return _TaxiDestinations.Count < 2 ? 0 : _TaxiDestinations[1];
		}

		public uint NextTaxiDestination()
		{
			_TaxiDestinations.RemoveAt(0);

			return GetTaxiDestination();
		}

		public List<uint> GetPath()
		{
			return _TaxiDestinations;
		}

		public bool Empty()
		{
			return _TaxiDestinations.Empty();
		}
	}
}