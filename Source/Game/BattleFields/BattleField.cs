// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.BattleFields
{
	public enum BattleFieldTypes
	{
		WinterGrasp = 1,
		TolBarad = 2,
		Max
	}

	public class BattleField : ZoneScript
	{
		protected uint _BattleId;   // BattleID (for packet)
		protected uint _BattleTime; // Length of a battle

		// Map of the objectives belonging to this OutdoorPvP
		private Dictionary<uint, BfCapturePoint> _capturePoints = new();
		protected Dictionary<int, uint> _Data32 = new();

		private Dictionary<int, ulong> _Data64 = new();
		protected uint _DefenderTeam;

		// Graveyard variables
		protected List<BfGraveyard> _GraveyardList = new(); // Vector witch contain the different GY of the battle

		private List<ObjectGuid>[] _Groups = new List<ObjectGuid>[2]; // Contain different raid group
		protected Dictionary<ObjectGuid, long>[] _InvitedPlayers = new Dictionary<ObjectGuid, long>[2];
		protected bool _isActive;
		protected bool _IsEnabled;
		private uint _LastResurectTimer; // Timer for resurect player every 30 sec
		protected Map _Map;
		protected uint _MapId;           // MapId where is Battlefield
		protected uint _MaxPlayer;       // Maximum number of player that participated to Battlefield
		protected uint _MinLevel;        // Required level to participate at Battlefield
		protected uint _MinPlayer;       // Minimum number of player for Battlefield start
		protected uint _NoWarBattleTime; // Time between two battles

		// Players info maps
		protected List<ObjectGuid>[] _players = new List<ObjectGuid>[2];        // Players in zone
		protected List<ObjectGuid>[] _PlayersInQueue = new List<ObjectGuid>[2]; // Players in the queue
		protected List<ObjectGuid>[] _PlayersInWar = new List<ObjectGuid>[2];   // Players in WG combat
		protected Dictionary<ObjectGuid, long>[] _PlayersWillBeKick = new Dictionary<ObjectGuid, long>[2];
		protected uint _RestartAfterCrash; // Delay to restart Wintergrasp if the server crashed during a running battle.
		protected bool _StartGrouping;     // bool for know if all players in area has been invited

		protected uint _StartGroupingTimer; // Timer for invite players in area 15 minute before start battle
		protected uint _TimeForAcceptInvite;
		protected uint _Timer; // Global timer for event

		// Variables that must exist for each battlefield
		protected uint _TypeId; // See enum BattlefieldTypes

		private uint _uiKickAfkPlayersTimer; // Timer for check Afk in war
		protected uint _uiKickDontAcceptTimer;
		protected uint _ZoneId;               // ZoneID of Wintergrasp = 4197
		protected WorldLocation KickPosition; // Position where players are teleported if they switch to afk during the battle or if they don't accept invitation

		public ObjectGuid StalkerGuid;

		public BattleField(Map map)
		{
			_IsEnabled    = true;
			_DefenderTeam = TeamId.Neutral;

			_TimeForAcceptInvite   = 20;
			_uiKickDontAcceptTimer = 1000;
			_uiKickAfkPlayersTimer = 1000;

			_LastResurectTimer = 30 * Time.InMilliseconds;

			_Map   = map;
			_MapId = map.GetId();

			for (byte i = 0; i < 2; ++i)
			{
				_players[i]           = new List<ObjectGuid>();
				_PlayersInQueue[i]    = new List<ObjectGuid>();
				_PlayersInWar[i]      = new List<ObjectGuid>();
				_InvitedPlayers[i]    = new Dictionary<ObjectGuid, long>();
				_PlayersWillBeKick[i] = new Dictionary<ObjectGuid, long>();
				_Groups[i]            = new List<ObjectGuid>();
			}
		}

		public void HandlePlayerEnterZone(Player player, uint zone)
		{
			// If battle is started,
			// If not full of players > invite player to join the war
			// If full of players > announce to player that BF is full and kick him after a few second if he desn't leave
			if (IsWarTime())
			{
				if (_PlayersInWar[player.GetTeamId()].Count + _InvitedPlayers[player.GetTeamId()].Count < _MaxPlayer) // Vacant spaces
				{
					InvitePlayerToWar(player);
				}
				else // No more vacant places
				{
					// todo Send a packet to announce it to player
					_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
					InvitePlayerToQueue(player);
				}
			}
			else
			{
				// If time left is < 15 minutes invite player to join queue
				if (_Timer <= _StartGroupingTimer)
					InvitePlayerToQueue(player);
			}

			// Add player in the list of player in zone
			_players[player.GetTeamId()].Add(player.GetGUID());
			OnPlayerEnterZone(player);
		}

		// Called when a player leave the zone
		public void HandlePlayerLeaveZone(Player player, uint zone)
		{
			if (IsWarTime())
				// If the player is participating to the battle
				if (_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()))
				{
					_PlayersInWar[player.GetTeamId()].Remove(player.GetGUID());
					Group group = player.GetGroup();

					if (group) // Remove the player from the raid group
						group.RemoveMember(player.GetGUID());

					OnPlayerLeaveWar(player);
				}

			foreach (var capturePoint in _capturePoints.Values)
				capturePoint.HandlePlayerLeave(player);

			_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());
			_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
			_players[player.GetTeamId()].Remove(player.GetGUID());
			SendRemoveWorldStates(player);
			RemovePlayerFromResurrectQueue(player.GetGUID());
			OnPlayerLeaveZone(player);
		}

		public virtual bool Update(uint diff)
		{
			if (_Timer <= diff)
			{
				// Battlefield ends on time
				if (IsWarTime())
					EndBattle(true);
				else // Time to start a new battle!
					StartBattle();
			}
			else
			{
				_Timer -= diff;
			}

			// Invite players a few minutes before the battle's beginning
			if (!IsWarTime() &&
			    !_StartGrouping &&
			    _Timer <= _StartGroupingTimer)
			{
				_StartGrouping = true;
				InvitePlayersInZoneToQueue();
				OnStartGrouping();
			}

			bool objective_changed = false;

			if (IsWarTime())
			{
				if (_uiKickAfkPlayersTimer <= diff)
				{
					_uiKickAfkPlayersTimer = 1000;
					KickAfkPlayers();
				}
				else
				{
					_uiKickAfkPlayersTimer -= diff;
				}

				// Kick players who chose not to accept invitation to the battle
				if (_uiKickDontAcceptTimer <= diff)
				{
					long now = GameTime.GetGameTime();

					for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
						foreach (var pair in _InvitedPlayers[team])
							if (pair.Value <= now)
								KickPlayerFromBattlefield(pair.Key);

					InvitePlayersInZoneToWar();

					for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
						foreach (var pair in _PlayersWillBeKick[team])
							if (pair.Value <= now)
								KickPlayerFromBattlefield(pair.Key);

					_uiKickDontAcceptTimer = 1000;
				}
				else
				{
					_uiKickDontAcceptTimer -= diff;
				}

				foreach (var pair in _capturePoints)
					if (pair.Value.Update(diff))
						objective_changed = true;
			}


			if (_LastResurectTimer <= diff)
			{
				for (byte i = 0; i < _GraveyardList.Count; i++)
					if (GetGraveyardById(i) != null)
						_GraveyardList[i].Resurrect();

				_LastResurectTimer = BattlegroundConst.ResurrectionInterval;
			}
			else
			{
				_LastResurectTimer -= diff;
			}

			return objective_changed;
		}

		private void InvitePlayersInZoneToQueue()
		{
			for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
				foreach (var guid in _players[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						InvitePlayerToQueue(player);
				}
		}

		private void InvitePlayerToQueue(Player player)
		{
			if (_PlayersInQueue[player.GetTeamId()].Contains(player.GetGUID()))
				return;

			if (_PlayersInQueue[player.GetTeamId()].Count <= _MinPlayer ||
			    _PlayersInQueue[GetOtherTeam(player.GetTeamId())].Count >= _MinPlayer)
				PlayerAcceptInviteToQueue(player);
		}

		private void InvitePlayersInQueueToWar()
		{
			for (byte team = 0; team < 2; ++team)
			{
				foreach (var guid in _PlayersInQueue[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
					{
						if (_PlayersInWar[player.GetTeamId()].Count + _InvitedPlayers[player.GetTeamId()].Count < _MaxPlayer)
						{
							InvitePlayerToWar(player);
						}
						else
						{
							//Full
						}
					}
				}

				_PlayersInQueue[team].Clear();
			}
		}

		private void InvitePlayersInZoneToWar()
		{
			for (byte team = 0; team < 2; ++team)
				foreach (var guid in _players[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
					{
						if (_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) ||
						    _InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
							continue;

						if (_PlayersInWar[player.GetTeamId()].Count + _InvitedPlayers[player.GetTeamId()].Count < _MaxPlayer)
							InvitePlayerToWar(player);
						else // Battlefield is full of players
							_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
					}
				}
		}

		private void InvitePlayerToWar(Player player)
		{
			if (!player)
				return;

			// todo needed ?
			if (player.IsInFlight())
				return;

			if (player.InArena() ||
			    player.GetBattleground())
			{
				_PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());

				return;
			}

			// If the player does not match minimal level requirements for the battlefield, kick him
			if (player.GetLevel() < _MinLevel)
			{
				if (!_PlayersWillBeKick[player.GetTeamId()].ContainsKey(player.GetGUID()))
					_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;

				return;
			}

			// Check if player is not already in war
			if (_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) ||
			    _InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
				return;

			_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
			_InvitedPlayers[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + _TimeForAcceptInvite;
			PlayerAcceptInviteToWar(player);
		}

		public void InitStalker(uint entry, Position pos)
		{
			Creature creature = SpawnCreature(entry, pos);

			if (creature)
				StalkerGuid = creature.GetGUID();
			else
				Log.outError(LogFilter.Battlefield, "Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
		}

		private void KickAfkPlayers()
		{
			for (byte team = 0; team < 2; ++team)
				foreach (var guid in _PlayersInWar[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						if (player.IsAFK())
							KickPlayerFromBattlefield(guid);
				}
		}

		public void KickPlayerFromBattlefield(ObjectGuid guid)
		{
			Player player = Global.ObjAccessor.FindPlayer(guid);

			if (player)
				if (player.GetZoneId() == GetZoneId())
					player.TeleportTo(KickPosition);
		}

		public void StartBattle()
		{
			if (_isActive)
				return;

			for (int team = 0; team < 2; team++)
			{
				_PlayersInWar[team].Clear();
				_Groups[team].Clear();
			}

			_Timer    = _BattleTime;
			_isActive = true;

			InvitePlayersInZoneToWar();
			InvitePlayersInQueueToWar();

			OnBattleStart();
		}

		public void EndBattle(bool endByTimer)
		{
			if (!_isActive)
				return;

			_isActive = false;

			_StartGrouping = false;

			if (!endByTimer)
				SetDefenderTeam(GetAttackerTeam());

			// Reset battlefield timer
			_Timer = _NoWarBattleTime;

			OnBattleEnd(endByTimer);
		}

		private void DoPlaySoundToAll(uint soundID)
		{
			BroadcastPacketToWar(new PlaySound(ObjectGuid.Empty, soundID, 0));
		}

		public bool HasPlayer(Player player)
		{
			return _players[player.GetTeamId()].Contains(player.GetGUID());
		}

		// Called in WorldSession:HandleBfQueueInviteResponse
		public void PlayerAcceptInviteToQueue(Player player)
		{
			// Add player in queue
			_PlayersInQueue[player.GetTeamId()].Add(player.GetGUID());
		}

		// Called in WorldSession:HandleBfExitRequest
		public void AskToLeaveQueue(Player player)
		{
			// Remove player from queue
			_PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());
		}

		// Called in WorldSession::HandleHearthAndResurrect
		public void PlayerAskToLeave(Player player)
		{
			// Player leaving Wintergrasp, teleport to Dalaran.
			// ToDo: confirm teleport destination.
			player.TeleportTo(571, 5804.1499f, 624.7710f, 647.7670f, 1.6400f);
		}

		// Called in WorldSession:HandleBfEntryInviteResponse
		public void PlayerAcceptInviteToWar(Player player)
		{
			if (!IsWarTime())
				return;

			if (AddOrSetPlayerToCorrectBfGroup(player))
			{
				_PlayersInWar[player.GetTeamId()].Add(player.GetGUID());
				_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());

				if (player.IsAFK())
					player.ToggleAFK();

				OnPlayerJoinWar(player); //for scripting
			}
		}

		public void TeamCastSpell(uint teamIndex, int spellId)
		{
			foreach (var guid in _PlayersInWar[teamIndex])
			{
				Player player = Global.ObjAccessor.FindPlayer(guid);

				if (player)
				{
					if (spellId > 0)
						player.CastSpell(player, (uint)spellId, true);
					else
						player.RemoveAuraFromStack((uint)-spellId);
				}
			}
		}

		public void BroadcastPacketToZone(ServerPacket data)
		{
			for (byte team = 0; team < 2; ++team)
				foreach (var guid in _players[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						player.SendPacket(data);
				}
		}

		public void BroadcastPacketToQueue(ServerPacket data)
		{
			for (byte team = 0; team < 2; ++team)
				foreach (var guid in _PlayersInQueue[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						player.SendPacket(data);
				}
		}

		public void BroadcastPacketToWar(ServerPacket data)
		{
			for (byte team = 0; team < 2; ++team)
				foreach (var guid in _PlayersInWar[team])
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						player.SendPacket(data);
				}
		}

		public void SendWarning(uint id, WorldObject target = null)
		{
			Creature stalker = GetCreature(StalkerGuid);

			if (stalker)
				Global.CreatureTextMgr.SendChat(stalker, (byte)id, target);
		}

		public void AddCapturePoint(BfCapturePoint cp)
		{
			if (_capturePoints.ContainsKey(cp.GetCapturePointEntry()))
				Log.outError(LogFilter.Battlefield, "Battlefield.AddCapturePoint: CapturePoint {0} already exists!", cp.GetCapturePointEntry());

			_capturePoints[cp.GetCapturePointEntry()] = cp;
		}

		private BfCapturePoint GetCapturePoint(uint entry)
		{
			return _capturePoints.LookupByKey(entry);
		}

		public void RegisterZone(uint zoneId)
		{
			Global.BattleFieldMgr.AddZone(zoneId, this);
		}

		public void HideNpc(Creature creature)
		{
			creature.CombatStop();
			creature.SetReactState(ReactStates.Passive);
			creature.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
			creature.DisappearAndDie();
			creature.SetVisible(false);
		}

		public void ShowNpc(Creature creature, bool aggressive)
		{
			creature.SetVisible(true);
			creature.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);

			if (!creature.IsAlive())
				creature.Respawn(true);

			if (aggressive)
			{
				creature.SetReactState(ReactStates.Aggressive);
			}
			else
			{
				creature.SetUnitFlag(UnitFlags.NonAttackable);
				creature.SetReactState(ReactStates.Passive);
			}
		}

		// ****************************************************
		// ******************* Group System *******************
		// ****************************************************
		private Group GetFreeBfRaid(int teamIndex)
		{
			foreach (var guid in _Groups[teamIndex])
			{
				Group group = Global.GroupMgr.GetGroupByGUID(guid);

				if (group)
					if (!group.IsFull())
						return group;
			}

			return null;
		}

		private Group GetGroupPlayer(ObjectGuid plguid, int teamIndex)
		{
			foreach (var guid in _Groups[teamIndex])
			{
				Group group = Global.GroupMgr.GetGroupByGUID(guid);

				if (group)
					if (group.IsMember(plguid))
						return group;
			}

			return null;
		}

		private bool AddOrSetPlayerToCorrectBfGroup(Player player)
		{
			if (!player.IsInWorld)
				return false;

			Group oldgroup = player.GetGroup();

			if (oldgroup)
				oldgroup.RemoveMember(player.GetGUID());

			Group group = GetFreeBfRaid(player.GetTeamId());

			if (!group)
			{
				group = new Group();
				group.SetBattlefieldGroup(this);
				group.Create(player);
				Global.GroupMgr.AddGroup(group);
				_Groups[player.GetTeamId()].Add(group.GetGUID());
			}
			else if (group.IsMember(player.GetGUID()))
			{
				byte subgroup = group.GetMemberGroup(player.GetGUID());
				player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
			}
			else
			{
				group.AddMember(player);
			}

			return true;
		}

		//***************End of Group System*******************

		public BfGraveyard GetGraveyardById(int id)
		{
			if (id < _GraveyardList.Count)
			{
				BfGraveyard graveyard = _GraveyardList[id];

				if (graveyard != null)
					return graveyard;
				else
					Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} not existed", id);
			}
			else
			{
				Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} cant be found", id);
			}

			return null;
		}

		public WorldSafeLocsEntry GetClosestGraveYard(Player player)
		{
			BfGraveyard closestGY = null;
			float       maxdist   = -1;

			for (byte i = 0; i < _GraveyardList.Count; i++)
				if (_GraveyardList[i] != null)
				{
					if (_GraveyardList[i].GetControlTeamId() != player.GetTeamId())
						continue;

					float dist = _GraveyardList[i].GetDistance(player);

					if (dist < maxdist ||
					    maxdist < 0)
					{
						closestGY = _GraveyardList[i];
						maxdist   = dist;
					}
				}

			if (closestGY != null)
				return Global.ObjectMgr.GetWorldSafeLoc(closestGY.GetGraveyardId());

			return null;
		}

		public virtual void AddPlayerToResurrectQueue(ObjectGuid npcGuid, ObjectGuid playerGuid)
		{
			for (byte i = 0; i < _GraveyardList.Count; i++)
			{
				if (_GraveyardList[i] == null)
					continue;

				if (_GraveyardList[i].HasNpc(npcGuid))
				{
					_GraveyardList[i].AddPlayer(playerGuid);

					break;
				}
			}
		}

		public void RemovePlayerFromResurrectQueue(ObjectGuid playerGuid)
		{
			for (byte i = 0; i < _GraveyardList.Count; i++)
			{
				if (_GraveyardList[i] == null)
					continue;

				if (_GraveyardList[i].HasPlayer(playerGuid))
				{
					_GraveyardList[i].RemovePlayer(playerGuid);

					break;
				}
			}
		}

		public void SendAreaSpiritHealerQuery(Player player, ObjectGuid guid)
		{
			AreaSpiritHealerTime areaSpiritHealerTime = new();
			areaSpiritHealerTime.HealerGuid = guid;
			areaSpiritHealerTime.TimeLeft   = _LastResurectTimer; // resurrect every 30 seconds

			player.SendPacket(areaSpiritHealerTime);
		}

		public Creature SpawnCreature(uint entry, Position pos)
		{
			if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
			{
				Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: entry {0} does not exist.", entry);

				return null;
			}

			Creature creature = Creature.CreateCreature(entry, _Map, pos);

			if (!creature)
			{
				Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);

				return null;
			}

			creature.SetHomePosition(pos);

			// Set creature in world
			_Map.AddToMap(creature);
			creature.SetActive(true);
			creature.SetFarVisible(true);

			return creature;
		}

		// Method for spawning gameobject on map
		public GameObject SpawnGameObject(uint entry, Position pos, Quaternion rotation)
		{
			if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
			{
				Log.outError(LogFilter.Battlefield, "Battlefield.SpawnGameObject: GameObject template {0} not found in database! Battlefield not created!", entry);

				return null;
			}

			// Create gameobject
			GameObject go = GameObject.CreateGameObject(entry, _Map, pos, rotation, 255, GameObjectState.Ready);

			if (!go)
			{
				Log.outError(LogFilter.Battlefield, "Battlefield:SpawnGameObject: Cannot create gameobject template {1}! Battlefield not created!", entry);

				return null;
			}

			// Add to world
			_Map.AddToMap(go);
			go.SetActive(true);
			go.SetFarVisible(true);

			return go;
		}

		public Creature GetCreature(ObjectGuid guid)
		{
			if (!_Map)
				return null;

			return _Map.GetCreature(guid);
		}

		public GameObject GetGameObject(ObjectGuid guid)
		{
			if (!_Map)
				return null;

			return _Map.GetGameObject(guid);
		}

		// Call this to init the Battlefield
		public virtual bool SetupBattlefield()
		{
			return true;
		}

		// Called when a Unit is kill in battlefield zone
		public virtual void HandleKill(Player killer, Unit killed)
		{
		}

		public uint GetTypeId()
		{
			return _TypeId;
		}

		public uint GetZoneId()
		{
			return _ZoneId;
		}

		public uint GetMapId()
		{
			return _MapId;
		}

		public Map GetMap()
		{
			return _Map;
		}

		public ulong GetQueueId()
		{
			return MathFunctions.MakePair64(_BattleId | 0x20000, 0x1F100000);
		}

		// Return true if battle is start, false if battle is not started
		public bool IsWarTime()
		{
			return _isActive;
		}

		private BattlefieldState GetState()
		{
			return _isActive ? BattlefieldState.InProgress : (_Timer <= _StartGroupingTimer ? BattlefieldState.Warnup : BattlefieldState.Inactive);
		}

		// Enable or Disable battlefield
		public void ToggleBattlefield(bool enable)
		{
			_IsEnabled = enable;
		}

		// Return if battlefield is enable
		public bool IsEnabled()
		{
			return _IsEnabled;
		}

		// All-purpose data storage 64 bit
		public virtual ulong GetData64(int dataId)
		{
			return _Data64[dataId];
		}

		public virtual void SetData64(int dataId, ulong value)
		{
			_Data64[dataId] = value;
		}

		// All-purpose data storage 32 bit
		public virtual uint GetData(int dataId)
		{
			return _Data32[dataId];
		}

		public virtual void SetData(int dataId, uint value)
		{
			_Data32[dataId] = value;
		}

		public virtual void UpdateData(int index, int pad)
		{
			if (pad < 0)
				_Data32[index] -= (uint)-pad;
			else
				_Data32[index] += (uint)pad;
		}

		// Battlefield - generic methods
		public uint GetDefenderTeam()
		{
			return _DefenderTeam;
		}

		public uint GetAttackerTeam()
		{
			return 1 - _DefenderTeam;
		}

		public int GetOtherTeam(int teamIndex)
		{
			return (teamIndex == TeamId.Horde ? TeamId.Alliance : TeamId.Horde);
		}

		private void SetDefenderTeam(uint team)
		{
			_DefenderTeam = team;
		}

		// Called on start
		public virtual void OnBattleStart()
		{
		}

		// Called at the end of battle
		public virtual void OnBattleEnd(bool endByTimer)
		{
		}

		// Called x minutes before battle start when player in zone are invite to join queue
		public virtual void OnStartGrouping()
		{
		}

		// Called when a player accept to join the battle
		public virtual void OnPlayerJoinWar(Player player)
		{
		}

		// Called when a player leave the battle
		public virtual void OnPlayerLeaveWar(Player player)
		{
		}

		// Called when a player leave battlefield zone
		public virtual void OnPlayerLeaveZone(Player player)
		{
		}

		// Called when a player enter in battlefield zone
		public virtual void OnPlayerEnterZone(Player player)
		{
		}

		public uint GetBattleId()
		{
			return _BattleId;
		}

		public virtual void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1)
		{
		}

		// Return if we can use mount in battlefield
		public bool CanFlyIn()
		{
			return !_isActive;
		}

		private List<BfGraveyard> GetGraveyardVector()
		{
			return _GraveyardList;
		}

		public uint GetTimer()
		{
			return _Timer;
		}

		public void SetTimer(uint timer)
		{
			_Timer = timer;
		}

		// use for switch off all worldstate for client
		public virtual void SendRemoveWorldStates(Player player)
		{
		}
	}

	public class BfGraveyard
	{
		protected BattleField _Bf;

		private uint _ControlTeam;
		private uint _GraveyardId;
		private List<ObjectGuid> _ResurrectQueue = new();
		private ObjectGuid[] _SpiritGuide = new ObjectGuid[SharedConst.PvpTeamsCount];

		public BfGraveyard(BattleField battlefield)
		{
			_Bf             = battlefield;
			_GraveyardId    = 0;
			_ControlTeam    = TeamId.Neutral;
			_SpiritGuide[0] = ObjectGuid.Empty;
			_SpiritGuide[1] = ObjectGuid.Empty;
		}

		public void Initialize(uint startControl, uint graveyardId)
		{
			_ControlTeam = startControl;
			_GraveyardId = graveyardId;
		}

		public void SetSpirit(Creature spirit, int teamIndex)
		{
			if (!spirit)
			{
				Log.outError(LogFilter.Battlefield, "BfGraveyard:SetSpirit: Invalid Spirit.");

				return;
			}

			_SpiritGuide[teamIndex] = spirit.GetGUID();
			spirit.SetReactState(ReactStates.Passive);
		}

		public float GetDistance(Player player)
		{
			WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(_GraveyardId);

			return player.GetDistance2d(safeLoc.Loc.GetPositionX(), safeLoc.Loc.GetPositionY());
		}

		public void AddPlayer(ObjectGuid playerGuid)
		{
			if (!_ResurrectQueue.Contains(playerGuid))
			{
				_ResurrectQueue.Add(playerGuid);
				Player player = Global.ObjAccessor.FindPlayer(playerGuid);

				if (player)
					player.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
			}
		}

		public void RemovePlayer(ObjectGuid playerGuid)
		{
			_ResurrectQueue.Remove(playerGuid);

			Player player = Global.ObjAccessor.FindPlayer(playerGuid);

			if (player)
				player.RemoveAurasDueToSpell(BattlegroundConst.SpellWaitingForResurrect);
		}

		public void Resurrect()
		{
			if (_ResurrectQueue.Empty())
				return;

			foreach (var guid in _ResurrectQueue)
			{
				// Get player object from his guid
				Player player = Global.ObjAccessor.FindPlayer(guid);

				if (!player)
					continue;

				// Check  if the player is in world and on the good graveyard
				if (player.IsInWorld)
				{
					Creature spirit = _Bf.GetCreature(_SpiritGuide[_ControlTeam]);

					if (spirit)
						spirit.CastSpell(spirit, BattlegroundConst.SpellSpiritHeal, true);
				}

				// Resurect player
				player.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
				player.ResurrectPlayer(1.0f);
				player.CastSpell(player, 6962, true);
				player.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);

				player.SpawnCorpseBones(false);
			}

			_ResurrectQueue.Clear();
		}

		// For changing graveyard control
		public void GiveControlTo(uint team)
		{
			// Guide switching
			// Note: Visiblity changes are made by phasing
			/*if (_SpiritGuide[1 - team])
			    _SpiritGuide[1 - team].SetVisible(false);
			if (_SpiritGuide[team])
			    _SpiritGuide[team].SetVisible(true);*/

			_ControlTeam = team;
			// Teleport to other graveyard, player witch were on this graveyard
			RelocateDeadPlayers();
		}

		private void RelocateDeadPlayers()
		{
			WorldSafeLocsEntry closestGrave = null;

			foreach (var guid in _ResurrectQueue)
			{
				Player player = Global.ObjAccessor.FindPlayer(guid);

				if (!player)
					continue;

				if (closestGrave != null)
				{
					player.TeleportTo(closestGrave.Loc);
				}
				else
				{
					closestGrave = _Bf.GetClosestGraveYard(player);

					if (closestGrave != null)
						player.TeleportTo(closestGrave.Loc);
				}
			}
		}

		public bool HasNpc(ObjectGuid guid)
		{
			if (_SpiritGuide[TeamId.Alliance].IsEmpty() ||
			    _SpiritGuide[TeamId.Horde].IsEmpty())
				return false;

			if (!_Bf.GetCreature(_SpiritGuide[TeamId.Alliance]) ||
			    !_Bf.GetCreature(_SpiritGuide[TeamId.Horde]))
				return false;

			return (_SpiritGuide[TeamId.Alliance] == guid || _SpiritGuide[TeamId.Horde] == guid);
		}

		// Check if a player is in this graveyard's ressurect queue
		public bool HasPlayer(ObjectGuid guid)
		{
			return _ResurrectQueue.Contains(guid);
		}

		// Get the graveyard's ID.
		public uint GetGraveyardId()
		{
			return _GraveyardId;
		}

		public uint GetControlTeamId()
		{
			return _ControlTeam;
		}
	}

	public class BfCapturePoint
	{
		// active Players in the area of the objective, 0 - alliance, 1 - horde
		private HashSet<ObjectGuid>[] _activePlayers = new HashSet<ObjectGuid>[SharedConst.PvpTeamsCount];

		// Battlefield this objective belongs to
		protected BattleField _Bf;

		// Capture point entry
		private uint _capturePointEntry;

		// Gameobject related to that capture point
		private ObjectGuid _capturePointGUID;

		// Maximum speed of capture
		private float _maxSpeed;

		// Total shift needed to capture the objective
		private float _maxValue;
		private float _minValue;

		// Neutral value on capture bar
		private uint _neutralValuePct;

		// Objective states
		private BattleFieldObjectiveStates _OldState;
		private BattleFieldObjectiveStates _State;
		protected uint _team;

		// The status of the objective
		private float _value;

		public BfCapturePoint(BattleField battlefield)
		{
			_Bf                = battlefield;
			_capturePointGUID  = ObjectGuid.Empty;
			_team              = TeamId.Neutral;
			_value             = 0;
			_minValue          = 0.0f;
			_maxValue          = 0.0f;
			_State             = BattleFieldObjectiveStates.Neutral;
			_OldState          = BattleFieldObjectiveStates.Neutral;
			_capturePointEntry = 0;
			_neutralValuePct   = 0;
			_maxSpeed          = 0;

			_activePlayers[0] = new HashSet<ObjectGuid>();
			_activePlayers[1] = new HashSet<ObjectGuid>();
		}

		public virtual bool HandlePlayerEnter(Player player)
		{
			if (!_capturePointGUID.IsEmpty())
			{
				GameObject capturePoint = _Bf.GetGameObject(_capturePointGUID);

				if (capturePoint)
				{
					player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
					player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)(Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f)));
					player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
				}
			}

			return _activePlayers[player.GetTeamId()].Add(player.GetGUID());
		}

		public virtual void HandlePlayerLeave(Player player)
		{
			if (!_capturePointGUID.IsEmpty())
			{
				GameObject capturePoint = _Bf.GetGameObject(_capturePointGUID);

				if (capturePoint)
					player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 0);
			}

			_activePlayers[player.GetTeamId()].Remove(player.GetGUID());
		}

		public virtual void SendChangePhase()
		{
			if (_capturePointGUID.IsEmpty())
				return;

			GameObject capturePoint = _Bf.GetGameObject(_capturePointGUID);

			if (capturePoint)
			{
				// send this too, sometimes the slider disappears, dunno why :(
				SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
				// send these updates to only the ones in this objective
				SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
				// send this too, sometimes it resets :S
				SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
			}
		}

		public bool SetCapturePointData(GameObject capturePoint)
		{
			Cypher.Assert(capturePoint);

			Log.outError(LogFilter.Battlefield, "Creating capture point {0}", capturePoint.GetEntry());

			_capturePointGUID  = capturePoint.GetGUID();
			_capturePointEntry = capturePoint.GetEntry();

			// check info existence
			GameObjectTemplate goinfo = capturePoint.GetGoInfo();

			if (goinfo.type != GameObjectTypes.ControlZone)
			{
				Log.outError(LogFilter.Server, "OutdoorPvP: GO {0} is not capture point!", capturePoint.GetEntry());

				return false;
			}

			// get the needed values from goinfo
			_maxValue        = goinfo.ControlZone.maxTime;
			_maxSpeed        = _maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
			_neutralValuePct = goinfo.ControlZone.neutralPercent;
			_minValue        = _maxValue * goinfo.ControlZone.neutralPercent / 100;

			if (_team == TeamId.Alliance)
			{
				_value = _maxValue;
				_State = BattleFieldObjectiveStates.Alliance;
			}
			else
			{
				_value = -_maxValue;
				_State = BattleFieldObjectiveStates.Horde;
			}

			return true;
		}

		private GameObject GetCapturePointGo()
		{
			return _Bf.GetGameObject(_capturePointGUID);
		}

		private bool DelCapturePoint()
		{
			if (!_capturePointGUID.IsEmpty())
			{
				GameObject capturePoint = _Bf.GetGameObject(_capturePointGUID);

				if (capturePoint)
				{
					capturePoint.SetRespawnTime(0); // not save respawn time
					capturePoint.Delete();
					capturePoint.Dispose();
				}

				_capturePointGUID.Clear();
			}

			return true;
		}

		public virtual bool Update(uint diff)
		{
			if (_capturePointGUID.IsEmpty())
				return false;

			GameObject capturePoint = _Bf.GetGameObject(_capturePointGUID);

			if (capturePoint)
			{
				float radius = capturePoint.GetGoInfo().ControlZone.radius;

				for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
					foreach (var guid in _activePlayers[team])
					{
						Player player = Global.ObjAccessor.FindPlayer(guid);

						if (player)
							if (!capturePoint.IsWithinDistInMap(player, radius) ||
							    !player.IsOutdoorPvPActive())
								HandlePlayerLeave(player);
					}

				List<Unit> players  = new();
				var        checker  = new AnyPlayerInObjectRangeCheck(capturePoint, radius);
				var        searcher = new PlayerListSearcher(capturePoint, players, checker);
				Cell.VisitWorldObjects(capturePoint, searcher, radius);

				foreach (Player player in players)
					if (player.IsOutdoorPvPActive())
						if (_activePlayers[player.GetTeamId()].Add(player.GetGUID()))
							HandlePlayerEnter(player);
			}

			// get the difference of numbers
			float fact_diff = ((float)_activePlayers[TeamId.Alliance].Count - _activePlayers[TeamId.Horde].Count) * diff / 1000;

			if (MathFunctions.fuzzyEq(fact_diff, 0.0f))
				return false;

			Team  Challenger;
			float maxDiff = _maxSpeed * diff;

			if (fact_diff < 0)
			{
				// horde is in majority, but it's already horde-controlled . no change
				if (_State == BattleFieldObjectiveStates.Horde &&
				    _value <= -_maxValue)
					return false;

				if (fact_diff < -maxDiff)
					fact_diff = -maxDiff;

				Challenger = Team.Horde;
			}
			else
			{
				// ally is in majority, but it's already ally-controlled . no change
				if (_State == BattleFieldObjectiveStates.Alliance &&
				    _value >= _maxValue)
					return false;

				if (fact_diff > maxDiff)
					fact_diff = maxDiff;

				Challenger = Team.Alliance;
			}

			float oldValue = _value;
			uint  oldTeam  = _team;

			_OldState = _State;

			_value += fact_diff;

			if (_value < -_minValue) // red
			{
				if (_value < -_maxValue)
					_value = -_maxValue;

				_State = BattleFieldObjectiveStates.Horde;
				_team  = TeamId.Horde;
			}
			else if (_value > _minValue) // blue
			{
				if (_value > _maxValue)
					_value = _maxValue;

				_State = BattleFieldObjectiveStates.Alliance;
				_team  = TeamId.Alliance;
			}
			else if (oldValue * _value <= 0) // grey, go through mid point
			{
				// if challenger is ally, then n.a challenge
				if (Challenger == Team.Alliance)
					_State = BattleFieldObjectiveStates.NeutralAllianceChallenge;
				// if challenger is horde, then n.h challenge
				else if (Challenger == Team.Horde)
					_State = BattleFieldObjectiveStates.NeutralHordeChallenge;

				_team = TeamId.Neutral;
			}
			else // grey, did not go through mid point
			{
				// old phase and current are on the same side, so one team challenges the other
				if (Challenger == Team.Alliance &&
				    (_OldState == BattleFieldObjectiveStates.Horde || _OldState == BattleFieldObjectiveStates.NeutralHordeChallenge))
					_State = BattleFieldObjectiveStates.HordeAllianceChallenge;
				else if (Challenger == Team.Horde &&
				         (_OldState == BattleFieldObjectiveStates.Alliance || _OldState == BattleFieldObjectiveStates.NeutralAllianceChallenge))
					_State = BattleFieldObjectiveStates.AllianceHordeChallenge;

				_team = TeamId.Neutral;
			}

			if (MathFunctions.fuzzyNe(_value, oldValue))
				SendChangePhase();

			if (_OldState != _State)
			{
				if (oldTeam != _team)
					ChangeTeam(oldTeam);

				return true;
			}

			return false;
		}

		private void SendUpdateWorldState(uint field, uint value)
		{
			for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
				foreach (var guid in _activePlayers[team]) // send to all players present in the area
				{
					Player player = Global.ObjAccessor.FindPlayer(guid);

					if (player)
						player.SendUpdateWorldState(field, value);
				}
		}

		private void SendObjectiveComplete(uint id, ObjectGuid guid)
		{
			uint team;

			switch (_State)
			{
				case BattleFieldObjectiveStates.Alliance:
					team = TeamId.Alliance;

					break;
				case BattleFieldObjectiveStates.Horde:
					team = TeamId.Horde;

					break;
				default:
					return;
			}

			// send to all players present in the area
			foreach (var _guid in _activePlayers[team])
			{
				Player player = Global.ObjAccessor.FindPlayer(_guid);

				if (player)
					player.KilledMonsterCredit(id, guid);
			}
		}

		private bool IsInsideObjective(Player player)
		{
			return _activePlayers[player.GetTeamId()].Contains(player.GetGUID());
		}

		public virtual void ChangeTeam(uint oldTeam)
		{
		}

		public uint GetCapturePointEntry()
		{
			return _capturePointEntry;
		}

		private uint GetTeamId()
		{
			return _team;
		}
	}
}