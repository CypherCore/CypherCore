// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleGrounds;
using Game.Cache;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.PvP;
using Game.Spells;

namespace Game.Entities
{
	public partial class Player
	{
		//PvP
		public void UpdateHonorFields()
		{
			// called when rewarding honor and at each save
			long now   = GameTime.GetGameTime();
			long today = (GameTime.GetGameTime() / Time.Day) * Time.Day;

			if (_lastHonorUpdateTime < today)
			{
				long yesterday = today - Time.Day;

				// update yesterday's contribution
				if (_lastHonorUpdateTime >= yesterday)
					// this is the first update today, reset today's contribution
					SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), ActivePlayerData.TodayHonorableKills);
				else
					// no honor/kills yesterday or today, reset
					SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), (ushort)0);

				SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)0);
			}

			_lastHonorUpdateTime = now;
		}

		public bool RewardHonor(Unit victim, uint groupsize, int honor = -1, bool pvptoken = false)
		{
			// do not reward honor in arenas, but enable onkill spellproc
			if (InArena())
			{
				if (!victim ||
				    victim == this ||
				    !victim.IsTypeId(TypeId.Player))
					return false;

				if (GetBGTeam() == victim.ToPlayer().GetBGTeam())
					return false;

				return true;
			}

			// 'Inactive' this aura prevents the player from gaining honor points and BattlegroundTokenizer
			if (HasAura(BattlegroundConst.SpellAuraPlayerInactive))
				return false;

			ObjectGuid victim_guid = ObjectGuid.Empty;
			uint       victim_rank = 0;

			// need call before fields update to have chance move yesterday _data to appropriate fields before today _data change.
			UpdateHonorFields();

			// do not reward honor in arenas, but return true to enable onkill spellproc
			if (InBattleground() &&
			    GetBattleground() &&
			    GetBattleground().IsArena())
				return true;

			// Promote to float for calculations
			float honor_f = honor;

			if (honor_f <= 0)
			{
				if (!victim ||
				    victim == this ||
				    victim.HasAuraType(AuraType.NoPvpCredit))
					return false;

				victim_guid = victim.GetGUID();
				Player plrVictim = victim.ToPlayer();

				if (plrVictim)
				{
					if (GetEffectiveTeam() == plrVictim.GetEffectiveTeam() &&
					    !Global.WorldMgr.IsFFAPvPRealm())
						return false;

					byte k_level = (byte)GetLevel();
					byte k_grey  = (byte)Formulas.GetGrayLevel(k_level);
					byte v_level = (byte)victim.GetLevelForTarget(this);

					if (v_level <= k_grey)
						return false;

					// PLAYER_CHOSEN_TITLE VALUES DESCRIPTION
					//  [0]      Just Name
					//  [1..14]  Alliance honor titles and player Name
					//  [15..28] Horde honor titles and player Name
					//  [29..38] Other title and player Name
					//  [39+]    Nothing
					// this is all wrong, should be going off PvpTitle, not PlayerTitle
					uint victim_title = plrVictim.PlayerData.PlayerTitle;

					// Get Killer titles, CharTitlesEntry.bit_index
					// Ranks:
					//  title[1..14]  . rank[5..18]
					//  title[15..28] . rank[5..18]
					//  title[other]  . 0
					if (victim_title == 0)
						victim_guid.Clear(); // Don't show HK: <rank> message, only log.
					else if (victim_title < 15)
						victim_rank = victim_title + 4;
					else if (victim_title < 29)
						victim_rank = victim_title - 14 + 4;
					else
						victim_guid.Clear(); // Don't show HK: <rank> message, only log.

					honor_f = (float)Math.Ceiling(Formulas.HKHonorAtLevelF(k_level) * (v_level - k_grey) / (k_level - k_grey));

					// Count the number of playerkills in one day
					ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)1, true);
					// and those in a Lifetime
					ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), 1u, true);
					UpdateCriteria(CriteriaType.HonorableKills);
					UpdateCriteria(CriteriaType.DeliverKillingBlowToClass, (uint)victim.GetClass());
					UpdateCriteria(CriteriaType.DeliverKillingBlowToRace, (uint)victim.GetRace());
					UpdateCriteria(CriteriaType.PVPKillInArea, GetAreaId());
					UpdateCriteria(CriteriaType.EarnHonorableKill, 1, 0, 0, victim);
					UpdateCriteria(CriteriaType.KillPlayer, 1, 0, 0, victim);
				}
				else
				{
					if (!victim.ToCreature().IsRacialLeader())
						return false;

					honor_f     = 100.0f; // ??? need more info
					victim_rank = 19;     // HK: Leader
				}
			}

			if (victim != null)
			{
				if (groupsize > 1)
					honor_f /= groupsize;

				// apply honor Multiplier from aura (not stacking-get highest)
				MathFunctions.AddPct(ref honor_f, GetMaxPositiveAuraModifier(AuraType.ModHonorGainPct));
				honor_f += _restMgr.GetRestBonusFor(RestTypes.Honor, (uint)honor_f);
			}

			honor_f *= WorldConfig.GetFloatValue(WorldCfg.RateHonor);
			// Back to int now
			honor = (int)honor_f;
			// honor - for show honor points in log
			// victim_guid - for show victim Name in log
			// victim_rank [1..4]  HK: <dishonored rank>
			// victim_rank [5..19] HK: <alliance\horde rank>
			// victim_rank [0, 20+] HK: <>
			PvPCredit data = new();
			data.Honor         = honor;
			data.OriginalHonor = honor;
			data.Target        = victim_guid;
			data.Rank          = victim_rank;

			SendPacket(data);

			AddHonorXP((uint)honor);

			if (InBattleground() &&
			    honor > 0)
			{
				Battleground bg = GetBattleground();

				if (bg != null)
					bg.UpdatePlayerScore(this, ScoreType.BonusHonor, (uint)honor, false); //false: prevent looping
			}

			if (WorldConfig.GetBoolValue(WorldCfg.PvpTokenEnable) && pvptoken)
			{
				if (!victim ||
				    victim == this ||
				    victim.HasAuraType(AuraType.NoPvpCredit))
					return true;

				if (victim.IsTypeId(TypeId.Player))
				{
					// Check if allowed to receive it in current map
					int MapType = WorldConfig.GetIntValue(WorldCfg.PvpTokenMapType);

					if ((MapType == 1 && !InBattleground() && !IsFFAPvP()) ||
					    (MapType == 2 && !IsFFAPvP()) ||
					    (MapType == 3 && !InBattleground()))
						return true;

					uint itemId = WorldConfig.GetUIntValue(WorldCfg.PvpTokenId);
					uint count  = WorldConfig.GetUIntValue(WorldCfg.PvpTokenCount);

					if (AddItem(itemId, count))
						SendSysMessage("You have been awarded a token for slaying another player.");
				}
			}

			return true;
		}

		public void ResetHonorStats()
		{
			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)0);
			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), (ushort)0);
			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), 0u);
		}

		private void _InitHonorLevelOnLoadFromDB(uint honor, uint honorLevel)
		{
			SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.HonorLevel), honorLevel);
			UpdateHonorNextLevel();

			AddHonorXP(honor);
		}

		private void RewardPlayerWithRewardPack(uint rewardPackID)
		{
			RewardPlayerWithRewardPack(CliDB.RewardPackStorage.LookupByKey(rewardPackID));
		}

		private void RewardPlayerWithRewardPack(RewardPackRecord rewardPackEntry)
		{
			if (rewardPackEntry == null)
				return;

			CharTitlesRecord charTitlesEntry = CliDB.CharTitlesStorage.LookupByKey(rewardPackEntry.CharTitleID);

			if (charTitlesEntry != null)
				SetTitle(charTitlesEntry);

			ModifyMoney(rewardPackEntry.Money);

			var rewardCurrencyTypes = Global.DB2Mgr.GetRewardPackCurrencyTypesByRewardID(rewardPackEntry.Id);

			foreach (RewardPackXCurrencyTypeRecord currency in rewardCurrencyTypes)
				ModifyCurrency(currency.CurrencyTypeID, currency.Quantity);

			var rewardPackXItems = Global.DB2Mgr.GetRewardPackItemsByRewardID(rewardPackEntry.Id);

			foreach (RewardPackXItemRecord rewardPackXItem in rewardPackXItems)
				AddItem(rewardPackXItem.ItemID, rewardPackXItem.ItemQuantity);
		}

		public void AddHonorXP(uint xp)
		{
			uint currentHonorXP   = ActivePlayerData.Honor;
			uint nextHonorLevelXP = ActivePlayerData.HonorNextLevel;
			uint newHonorXP       = currentHonorXP + xp;
			uint honorLevel       = GetHonorLevel();

			if (xp < 1 ||
			    GetLevel() < PlayerConst.LevelMinHonor ||
			    IsMaxHonorLevel())
				return;

			while (newHonorXP >= nextHonorLevelXP)
			{
				newHonorXP -= nextHonorLevelXP;

				if (honorLevel < PlayerConst.MaxHonorLevel)
					SetHonorLevel((byte)(honorLevel + 1));

				honorLevel       = GetHonorLevel();
				nextHonorLevelXP = ActivePlayerData.HonorNextLevel;
			}

			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Honor), IsMaxHonorLevel() ? 0 : newHonorXP);
		}

		private void SetHonorLevel(byte level)
		{
			byte oldHonorLevel = (byte)GetHonorLevel();

			if (level == oldHonorLevel)
				return;

			SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.HonorLevel), level);
			UpdateHonorNextLevel();

			UpdateCriteria(CriteriaType.HonorLevelIncrease);
		}

		private void UpdateHonorNextLevel()
		{
			// 5500 at honor level 1
			// no idea what between here
			// 8800 at honor level ~14 (never goes above 8800)
			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HonorNextLevel), 8800u);
		}

		public uint GetHonorLevel()
		{
			return PlayerData.HonorLevel;
		}

		public bool IsMaxHonorLevel()
		{
			return GetHonorLevel() == PlayerConst.MaxHonorLevel;
		}

		public void ActivatePvpItemLevels(bool activate)
		{
			_usePvpItemLevels = activate;
		}

		public bool IsUsingPvpItemLevels()
		{
			return _usePvpItemLevels;
		}

		public void EnablePvpRules(bool dueToCombat = false)
		{
			if (HasPvpRulesEnabled())
				return;

			if (!HasSpell(195710))       // Honorable Medallion
				CastSpell(this, 208682); // Learn Gladiator's Medallion

			CastSpell(this, PlayerConst.SpellPvpRulesEnabled);

			if (!dueToCombat)
			{
				Aura aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

				if (aura != null)
				{
					aura.SetMaxDuration(-1);
					aura.SetDuration(-1);
				}
			}

			UpdateItemLevelAreaBasedScaling();
		}

		private void DisablePvpRules()
		{
			// Don't disable pvp rules when in pvp zone.
			if (IsInAreaThatActivatesPvpTalents())
				return;

			if (!GetCombatManager().HasPvPCombat())
			{
				RemoveAurasDueToSpell(PlayerConst.SpellPvpRulesEnabled);
				UpdateItemLevelAreaBasedScaling();
			}
			else
			{
				Aura aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

				if (aura != null)
					aura.SetDuration(aura.GetSpellInfo().GetMaxDuration());
			}
		}

		private bool HasPvpRulesEnabled()
		{
			return HasAura(PlayerConst.SpellPvpRulesEnabled);
		}

		private bool IsInAreaThatActivatesPvpTalents()
		{
			return IsAreaThatActivatesPvpTalents(GetAreaId());
		}

		private bool IsAreaThatActivatesPvpTalents(uint areaID)
		{
			if (InBattleground())
				return true;

			AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaID);

			if (area != null)
				do
				{
					if (area.IsSanctuary())
						return false;

					if (area.HasFlag(AreaFlags.Arena))
						return true;

					if (Global.BattleFieldMgr.IsWorldPvpArea(area.Id))
						return true;

					area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
				} while (area != null);

			return false;
		}

		public uint[] GetPvpTalentMap(byte spec)
		{
			return _specializationInfo.PvpTalents[spec];
		}

		//BGs
		public Battleground GetBattleground()
		{
			if (GetBattlegroundId() == 0)
				return null;

			return Global.BattlegroundMgr.GetBattleground(GetBattlegroundId(), _bgData.TypeID);
		}

		public bool InBattlegroundQueue(bool ignoreArena = false)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId != default &&
				    (!ignoreArena || _bgBattlegroundQueueID[i].BGQueueTypeId.BattlemasterListId != (ushort)BattlegroundTypeId.AA))
					return true;

			return false;
		}

		public BattlegroundQueueTypeId GetBattlegroundQueueTypeId(uint index)
		{
			if (index < SharedConst.MaxPlayerBGQueues)
				return _bgBattlegroundQueueID[index].BGQueueTypeId;

			return default;
		}

		public uint GetBattlegroundQueueIndex(BattlegroundQueueTypeId bgQueueTypeId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					return i;

			return SharedConst.MaxPlayerBGQueues;
		}

		public bool IsInvitedForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					return _bgBattlegroundQueueID[i].InvitedToInstance != 0;

			return false;
		}

		public bool InBattlegroundQueueForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
		{
			return GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues;
		}

		public void SetBattlegroundId(uint val, BattlegroundTypeId bgTypeId)
		{
			_bgData.InstanceID = val;
			_bgData.TypeID     = bgTypeId;
		}

		public uint AddBattlegroundQueueId(BattlegroundQueueTypeId val)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == default ||
				    _bgBattlegroundQueueID[i].BGQueueTypeId == val)
				{
					_bgBattlegroundQueueID[i].BGQueueTypeId     = val;
					_bgBattlegroundQueueID[i].InvitedToInstance = 0;
					_bgBattlegroundQueueID[i].JoinTime          = (uint)GameTime.GetGameTime();
					_bgBattlegroundQueueID[i].Mercenary         = HasAura(BattlegroundConst.SpellMercenaryContractHorde) || HasAura(BattlegroundConst.SpellMercenaryContractAlliance);

					return i;
				}

			return SharedConst.MaxPlayerBGQueues;
		}

		public bool HasFreeBattlegroundQueueId()
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == default)
					return true;

			return false;
		}

		public void RemoveBattlegroundQueueId(BattlegroundQueueTypeId val)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == val)
				{
					_bgBattlegroundQueueID[i].BGQueueTypeId     = default;
					_bgBattlegroundQueueID[i].InvitedToInstance = 0;
					_bgBattlegroundQueueID[i].JoinTime          = 0;
					_bgBattlegroundQueueID[i].Mercenary         = false;

					return;
				}
		}

		public void SetInviteForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, uint instanceId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					_bgBattlegroundQueueID[i].InvitedToInstance = instanceId;
		}

		public bool IsInvitedForBattlegroundInstance(uint instanceId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].InvitedToInstance == instanceId)
					return true;

			return false;
		}

		private void SetMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, bool mercenary)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					_bgBattlegroundQueueID[i].Mercenary = mercenary;
		}

		public bool IsMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					return _bgBattlegroundQueueID[i].Mercenary;

			return false;
		}

		public WorldLocation GetBattlegroundEntryPoint()
		{
			return _bgData.JoinPos;
		}

		public bool InBattleground()
		{
			return _bgData.InstanceID != 0;
		}

		public uint GetBattlegroundId()
		{
			return _bgData.InstanceID;
		}

		public BattlegroundTypeId GetBattlegroundTypeId()
		{
			return _bgData.TypeID;
		}

		public uint GetBattlegroundQueueJoinTime(BattlegroundQueueTypeId bgQueueTypeId)
		{
			for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
				if (_bgBattlegroundQueueID[i].BGQueueTypeId == bgQueueTypeId)
					return _bgBattlegroundQueueID[i].JoinTime;

			return 0;
		}

		public bool CanUseBattlegroundObject(GameObject gameobject)
		{
			// It is possible to call this method with a null pointer, only skipping faction check.
			if (gameobject)
			{
				FactionTemplateRecord playerFaction = GetFactionTemplateEntry();
				FactionTemplateRecord faction       = CliDB.FactionTemplateStorage.LookupByKey(gameobject.GetFaction());

				if (playerFaction != null &&
				    faction != null &&
				    !playerFaction.IsFriendlyTo(faction))
					return false;
			}

			// BUG: sometimes when player clicks on flag in AB - client won't send gameobject_use, only gameobject_report_use packet
			// Note: Mount, stealth and invisibility will be removed when used
			return (!IsTotalImmune() &&                                     // Damage immune
			        !HasAura(BattlegroundConst.SpellRecentlyDroppedFlag) && // Still has recently held flag debuff
			        IsAlive());                                             // Alive
		}

		public bool CanCaptureTowerPoint()
		{
			return (!HasStealthAura() &&      // not stealthed
			        !HasInvisibilityAura() && // not invisible
			        IsAlive());               // live player
		}

		public void SetBattlegroundEntryPoint()
		{
			// Taxi path store
			if (!Taxi.Empty())
			{
				_bgData.MountSpell  = 0;
				_bgData.TaxiPath[0] = Taxi.GetTaxiSource();
				_bgData.TaxiPath[1] = Taxi.GetTaxiDestination();

				// On taxi we don't need check for dungeon
				_bgData.JoinPos = new WorldLocation(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
			}
			else
			{
				_bgData.ClearTaxiPath();

				// Mount spell Id storing
				if (IsMounted())
				{
					var auras = GetAuraEffectsByType(AuraType.Mounted);

					if (!auras.Empty())
						_bgData.MountSpell = auras[0].GetId();
				}
				else
				{
					_bgData.MountSpell = 0;
				}

				// If map is dungeon find linked graveyard
				if (GetMap().IsDungeon())
				{
					WorldSafeLocsEntry entry = Global.ObjectMgr.GetClosestGraveYard(this, GetTeam(), this);

					if (entry != null)
						_bgData.JoinPos = entry.Loc;
					else
						Log.outError(LogFilter.Player, "SetBattlegroundEntryPoint: Dungeon map {0} has no linked graveyard, setting home location as entry point.", GetMapId());
				}
				// If new entry point is not BG or arena set it
				else if (!GetMap().IsBattlegroundOrArena())
				{
					_bgData.JoinPos = new WorldLocation(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
				}
			}

			if (_bgData.JoinPos.GetMapId() == 0xFFFFFFFF) // In error cases use _homebind position
				_bgData.JoinPos = new WorldLocation(GetHomebind());
		}

		public void SetBGTeam(Team team)
		{
			_bgData.Team = (uint)team;
			SetArenaFaction((byte)(team == Team.Alliance ? 1 : 0));
		}

		public Team GetBGTeam()
		{
			return _bgData.Team != 0 ? (Team)_bgData.Team : GetTeam();
		}

		public void LeaveBattleground(bool teleportToEntryPoint = true)
		{
			Battleground bg = GetBattleground();

			if (bg)
			{
				bg.RemovePlayerAtLeave(GetGUID(), teleportToEntryPoint, true);

				// call after remove to be sure that player resurrected for correct cast
				if (bg.IsBattleground() &&
				    !IsGameMaster() &&
				    WorldConfig.GetBoolValue(WorldCfg.BattlegroundCastDeserter))
					if (bg.GetStatus() == BattlegroundStatus.InProgress ||
					    bg.GetStatus() == BattlegroundStatus.WaitJoin)
					{
						//lets check if player was teleported from BG and schedule delayed Deserter spell cast
						if (IsBeingTeleportedFar())
						{
							ScheduleDelayedOperation(PlayerDelayedOperations.SpellCastDeserter);

							return;
						}

						CastSpell(this, 26013, true); // Deserter
					}
			}
		}

		public bool IsDeserter()
		{
			return HasAura(26013);
		}

		public bool CanJoinToBattleground(Battleground bg)
		{
			RBACPermissions perm = RBACPermissions.JoinNormalBg;

			if (bg.IsArena())
				perm = RBACPermissions.JoinArenas;
			else if (bg.IsRandom())
				perm = RBACPermissions.JoinRandomBg;

			return GetSession().HasPermission(perm);
		}

		public void ClearAfkReports()
		{
			_bgData.AfkReporter.Clear();
		}

		private bool CanReportAfkDueToLimit()
		{
			// a player can complain about 15 people per 5 minutes
			if (_bgData.AfkReportedCount++ >= 15)
				return false;

			return true;
		}

        /// <summary>
        ///  This player has been blamed to be inactive in a Battleground
        /// </summary>
        /// <param Name="reporter"></param>
        public void ReportedAfkBy(Player reporter)
		{
			ReportPvPPlayerAFKResult reportAfkResult = new();
			reportAfkResult.Offender = GetGUID();
			Battleground bg = GetBattleground();

			// Battleground also must be in progress!
			if (!bg ||
			    bg != reporter.GetBattleground() ||
			    GetEffectiveTeam() != reporter.GetEffectiveTeam() ||
			    bg.GetStatus() != BattlegroundStatus.InProgress)
			{
				reporter.SendPacket(reportAfkResult);

				return;
			}

			// check if player has 'Idle' or 'Inactive' debuff
			if (!_bgData.AfkReporter.Contains(reporter.GetGUID()) &&
			    !HasAura(43680) &&
			    !HasAura(43681) &&
			    reporter.CanReportAfkDueToLimit())
			{
				_bgData.AfkReporter.Add(reporter.GetGUID());

				// by default 3 players have to complain to apply debuff
				if (_bgData.AfkReporter.Count >= WorldConfig.GetIntValue(WorldCfg.BattlegroundReportAfk))
				{
					// cast 'Idle' spell
					CastSpell(this, 43680, true);
					_bgData.AfkReporter.Clear();
					reportAfkResult.NumBlackMarksOnOffender = (byte)_bgData.AfkReporter.Count;
					reportAfkResult.NumPlayersIHaveReported = reporter._bgData.AfkReportedCount;
					reportAfkResult.Result                  = ReportPvPPlayerAFKResult.ResultCode.Success;
				}
			}

			reporter.SendPacket(reportAfkResult);
		}

		public bool GetRandomWinner()
		{
			return _isBGRandomWinner;
		}

		public void SetRandomWinner(bool isWinner)
		{
			_isBGRandomWinner = isWinner;

			if (_isBGRandomWinner)
			{
				PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BATTLEGROUND_RANDOM);
				stmt.AddValue(0, GetGUID().GetCounter());
				DB.Characters.Execute(stmt);
			}
		}

		public bool GetBGAccessByLevel(BattlegroundTypeId bgTypeId)
		{
			// get a template bg instead of running one
			Battleground bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);

			if (!bg)
				return false;

			// limit check leel to dbc compatible level range
			uint level = GetLevel();

			if (level > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
				level = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);

			if (level < bg.GetMinLevel() ||
			    level > bg.GetMaxLevel())
				return false;

			return true;
		}

		public void SendPvpRewards()
		{
			//WorldPacket packet(SMSG_REQUEST_PVP_REWARDS_RESPONSE, 24);
			//SendPacket(packet);
		}

		//Arenas
		public void SetArenaTeamInfoField(byte slot, ArenaTeamInfoType type, uint value)
		{
		}

		public void SetInArenaTeam(uint ArenaTeamId, byte slot, byte type)
		{
			SetArenaTeamInfoField(slot, ArenaTeamInfoType.Id, ArenaTeamId);
			SetArenaTeamInfoField(slot, ArenaTeamInfoType.Type, type);
		}

		public static void LeaveAllArenaTeams(ObjectGuid guid)
		{
			CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);

			if (characterInfo == null)
				return;

			for (byte i = 0; i < SharedConst.MaxArenaSlot; ++i)
			{
				uint arenaTeamId = characterInfo.ArenaTeamId[i];

				if (arenaTeamId != 0)
				{
					ArenaTeam arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);

					if (arenaTeam != null)
						arenaTeam.DelMember(guid, true);
				}
			}
		}

		public uint GetArenaTeamId(byte slot)
		{
			return 0;
		}

		public void SetArenaTeamIdInvited(uint ArenaTeamId)
		{
			_arenaTeamIdInvited = ArenaTeamId;
		}

		public uint GetArenaTeamIdInvited()
		{
			return _arenaTeamIdInvited;
		}

		public uint GetRBGPersonalRating()
		{
			return GetArenaPersonalRating(3);
		}

		public uint GetArenaPersonalRating(byte slot)
		{
			PVPInfo pvpInfo = GetPvpInfoForBracket(slot);

			if (pvpInfo != null)
				return pvpInfo.Rating;

			return 0;
		}

		public PVPInfo GetPvpInfoForBracket(byte bracket)
		{
			int index = ActivePlayerData.PvpInfo.FindIndexIf(pvpInfo => { return pvpInfo.Bracket == bracket && !pvpInfo.Disqualified; });

			if (index >= 0)
				return ActivePlayerData.PvpInfo[index];

			return null;
		}

		//OutdoorPVP
		public bool IsOutdoorPvPActive()
		{
			return IsAlive() && !HasInvisibilityAura() && !HasStealthAura() && IsPvP() && !HasUnitMovementFlag(MovementFlag.Flying) && !IsInFlight();
		}

		public OutdoorPvP GetOutdoorPvP()
		{
			return Global.OutdoorPvPMgr.GetOutdoorPvPToZoneId(GetMap(), GetZoneId());
		}
	}
}