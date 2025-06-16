// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleGrounds;
using Game.Cache;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.PvP;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        //PvP
        public void UpdateHonorFields()
        {
            // called when rewarding honor and at each save
            long now = GameTime.GetGameTime();
            long today = (GameTime.GetGameTime() / Time.Day) * Time.Day;

            if (m_lastHonorUpdateTime < today)
            {
                long yesterday = today - Time.Day;

                // update yesterday's contribution
                if (m_lastHonorUpdateTime >= yesterday)
                {
                    // this is the first update today, reset today's contribution
                    SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.YesterdayHonorableKills), m_activePlayerData.TodayHonorableKills);
                }
                else
                {
                    // no honor/kills yesterday or today, reset
                    SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.YesterdayHonorableKills), (ushort)0);
                }

                SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TodayHonorableKills), (ushort)0);
            }

            m_lastHonorUpdateTime = now;
        }
        public bool RewardHonor(Unit victim, uint groupsize, int honor = -1, bool pvptoken = false)
        {
            // do not reward honor in arenas, but enable onkill spellproc
            if (InArena())
            {
                if (victim == null || victim == this || !victim.IsTypeId(TypeId.Player))
                    return false;

                if (GetBGTeam() == victim.ToPlayer().GetBGTeam())
                    return false;

                return true;
            }

            // 'Inactive' this aura prevents the player from gaining honor points and BattlegroundTokenizer
            if (HasAura(BattlegroundConst.SpellAuraPlayerInactive))
                return false;

            ObjectGuid victim_guid = ObjectGuid.Empty;
            uint victim_rank = 0;

            // need call before fields update to have chance move yesterday data to appropriate fields before today data change.
            UpdateHonorFields();

            // do not reward honor in arenas, but return true to enable onkill spellproc
            if (InBattleground() && GetBattleground() != null && GetBattleground().IsArena())
                return true;

            // Promote to float for calculations
            float honor_f = honor;

            if (honor_f <= 0)
            {
                if (victim == null || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                    return false;

                victim_guid = victim.GetGUID();
                Player plrVictim = victim.ToPlayer();
                if (plrVictim != null)
                {
                    if (GetEffectiveTeam() == plrVictim.GetEffectiveTeam() && !Global.WorldMgr.IsFFAPvPRealm())
                        return false;

                    byte k_level = (byte)GetLevel();
                    byte k_grey = (byte)Formulas.GetGrayLevel(k_level);
                    byte v_level = (byte)victim.GetLevelForTarget(this);

                    if (v_level <= k_grey)
                        return false;

                    // PLAYER_CHOSEN_TITLE VALUES DESCRIPTION
                    //  [0]      Just name
                    //  [1..14]  Alliance honor titles and player name
                    //  [15..28] Horde honor titles and player name
                    //  [29..38] Other title and player name
                    //  [39+]    Nothing
                    // this is all wrong, should be going off PvpTitle, not PlayerTitle
                    uint victim_title = plrVictim.m_playerData.PlayerTitle;
                    // Get Killer titles, CharTitlesEntry.bit_index
                    // Ranks:
                    //  title[1..14]  . rank[5..18]
                    //  title[15..28] . rank[5..18]
                    //  title[other]  . 0
                    if (victim_title == 0)
                        victim_guid.Clear();                        // Don't show HK: <rank> message, only log.
                    else if (victim_title < 15)
                        victim_rank = victim_title + 4;
                    else if (victim_title < 29)
                        victim_rank = victim_title - 14 + 4;
                    else
                        victim_guid.Clear();                        // Don't show HK: <rank> message, only log.

                    honor_f = (float)Math.Ceiling(Formulas.HKHonorAtLevelF(k_level) * (v_level - k_grey) / (k_level - k_grey));

                    // count the number of playerkills in one day
                    ApplyModUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TodayHonorableKills), (ushort)1, true);
                    // and those in a lifetime
                    ApplyModUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.LifetimeHonorableKills), 1u, true);
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

                    honor_f = 100.0f;                               // ??? need more info
                    victim_rank = 19;                               // HK: Leader
                }
            }

            if (victim != null)
            {
                if (groupsize > 1)
                    honor_f /= groupsize;

                // apply honor multiplier from aura (not stacking-get highest)
                MathFunctions.AddPct(ref honor_f, GetMaxPositiveAuraModifier(AuraType.ModHonorGainPct));
                honor_f += _restMgr.GetRestBonusFor(RestTypes.Honor, (uint)honor_f);
            }

            honor_f *= WorldConfig.GetFloatValue(WorldCfg.RateHonor);
            // Back to int now
            honor = (int)honor_f;
            // honor - for show honor points in log
            // victim_guid - for show victim name in log
            // victim_rank [1..4]  HK: <dishonored rank>
            // victim_rank [5..19] HK: <alliance\horde rank>
            // victim_rank [0, 20+] HK: <>
            PvPCredit data = new();
            data.Honor = honor;
            data.OriginalHonor = honor;
            data.Target = victim_guid;
            data.Rank = (sbyte)victim_rank;

            SendPacket(data);

            AddHonorXP((uint)honor);

            if (InBattleground() && honor > 0)
            {
                Battleground bg = GetBattleground();
                if (bg != null)
                {
                    bg.UpdatePlayerScore(this, ScoreType.BonusHonor, (uint)honor, false); //false: prevent looping
                }
            }

            if (WorldConfig.GetBoolValue(WorldCfg.PvpTokenEnable) && pvptoken)
            {
                if (victim == null || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                    return true;

                if (victim.IsTypeId(TypeId.Player))
                {
                    // Check if allowed to receive it in current map
                    int MapType = WorldConfig.GetIntValue(WorldCfg.PvpTokenMapType);
                    if ((MapType == 1 && !InBattleground() && !IsFFAPvP())
                        || (MapType == 2 && !IsFFAPvP())
                        || (MapType == 3 && !InBattleground()))
                        return true;

                    uint itemId = WorldConfig.GetUIntValue(WorldCfg.PvpTokenId);
                    uint count = WorldConfig.GetUIntValue(WorldCfg.PvpTokenCount);

                    if (AddItem(itemId, count))
                        SendSysMessage("You have been awarded a token for slaying another player.");
                }
            }

            return true;
        }

        public void ResetHonorStats()
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TodayHonorableKills), (ushort)0);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.YesterdayHonorableKills), (ushort)0);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.LifetimeHonorableKills), 0u);
        }

        void _InitHonorLevelOnLoadFromDB(uint honor, uint honorLevel)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.HonorLevel), honorLevel);
            UpdateHonorNextLevel();

            AddHonorXP(honor);
        }

        void RewardPlayerWithRewardPack(uint rewardPackID)
        {
            RewardPlayerWithRewardPack(CliDB.RewardPackStorage.LookupByKey(rewardPackID));
        }

        void RewardPlayerWithRewardPack(RewardPackRecord rewardPackEntry)
        {
            if (rewardPackEntry == null)
                return;

            CharTitlesRecord charTitlesEntry = CliDB.CharTitlesStorage.LookupByKey(rewardPackEntry.CharTitleID);
            if (charTitlesEntry != null)
                SetTitle(charTitlesEntry);

            ModifyMoney(rewardPackEntry.Money);

            var rewardCurrencyTypes = Global.DB2Mgr.GetRewardPackCurrencyTypesByRewardID(rewardPackEntry.Id);
            foreach (RewardPackXCurrencyTypeRecord currency in rewardCurrencyTypes)
                AddCurrency(currency.CurrencyTypeID, (uint)currency.Quantity/* TODO: CurrencyGainSource */);

            var rewardPackXItems = Global.DB2Mgr.GetRewardPackItemsByRewardID(rewardPackEntry.Id);
            foreach (RewardPackXItemRecord rewardPackXItem in rewardPackXItems)
                AddItem(rewardPackXItem.ItemID, rewardPackXItem.ItemQuantity);
        }

        public void AddHonorXP(uint xp)
        {
            uint currentHonorXP = m_activePlayerData.Honor;
            uint nextHonorLevelXP = m_activePlayerData.HonorNextLevel;
            uint newHonorXP = currentHonorXP + xp;
            uint honorLevel = GetHonorLevel();

            if (xp < 1 || GetLevel() < PlayerConst.LevelMinHonor || IsMaxHonorLevel())
                return;

            while (newHonorXP >= nextHonorLevelXP)
            {
                newHonorXP -= nextHonorLevelXP;

                if (honorLevel < PlayerConst.MaxHonorLevel)
                    SetHonorLevel((byte)(honorLevel + 1));

                honorLevel = GetHonorLevel();
                nextHonorLevelXP = m_activePlayerData.HonorNextLevel;
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.Honor), IsMaxHonorLevel() ? 0 : newHonorXP);
        }

        void SetHonorLevel(byte level)
        {
            byte oldHonorLevel = (byte)GetHonorLevel();
            if (level == oldHonorLevel)
                return;

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.HonorLevel), level);
            UpdateHonorNextLevel();

            UpdateCriteria(CriteriaType.HonorLevelIncrease);
        }

        void UpdateHonorNextLevel()
        {
            // 5500 at honor level 1
            // no idea what between here
            // 8800 at honor level ~14 (never goes above 8800)
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.HonorNextLevel), 8800u);
        }

        public uint GetHonorLevel() { return m_playerData.HonorLevel; }
        public bool IsMaxHonorLevel() { return GetHonorLevel() == PlayerConst.MaxHonorLevel; }

        public void ActivatePvpItemLevels(bool activate) { _usePvpItemLevels = activate; }
        public bool IsUsingPvpItemLevels() { return _usePvpItemLevels; }

        public void EnablePvpRules(bool dueToCombat = false)
        {
            if (HasPvpRulesEnabled())
                return;

            if (!HasSpell(195710)) // Honorable Medallion
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

        void DisablePvpRules()
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

        bool HasPvpRulesEnabled()
        {
            return HasAura(PlayerConst.SpellPvpRulesEnabled);
        }

        bool IsInAreaThatActivatesPvpTalents()
        {
            return IsAreaThatActivatesPvpTalents(GetAreaId());
        }

        bool IsAreaThatActivatesPvpTalents(uint areaID)
        {
            if (InBattleground())
                return true;

            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaID);
            if (area != null)
            {
                do
                {
                    if (area.IsSanctuary())
                        return false;

                    if (area.HasFlag(AreaFlags.FreeForAllPvP))
                        return true;

                    if (Global.BattleFieldMgr.IsWorldPvpArea(area.Id))
                        return true;

                    area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

                } while (area != null);
            }

            return false;
        }

        public uint[] GetPvpTalentMap(byte spec) { return _specializationInfo.PvpTalents[spec]; }

        //BGs
        public Battleground GetBattleground()
        {
            if (GetBattlegroundId() == 0)
                return null;

            return Global.BattlegroundMgr.GetBattleground(GetBattlegroundId(), m_bgData.bgTypeID);
        }

        public bool InBattlegroundQueue(bool ignoreArena = false)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId != default && (!ignoreArena || m_bgBattlegroundQueueID[i].bgQueueTypeId.BattlemasterListId != (ushort)BattlegroundTypeId.AA))
                    return true;
            return false;
        }

        public BattlegroundQueueTypeId GetBattlegroundQueueTypeId(uint index)
        {
            if (index < SharedConst.MaxPlayerBGQueues)
                return m_bgBattlegroundQueueID[index].bgQueueTypeId;

            return default;
        }

        public uint GetBattlegroundQueueIndex(BattlegroundQueueTypeId bgQueueTypeId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    return i;
            return SharedConst.MaxPlayerBGQueues;
        }

        public bool IsInvitedForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    return m_bgBattlegroundQueueID[i].invitedToInstance != 0;
            return false;
        }

        public bool InBattlegroundQueueForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
        {
            return GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues;
        }

        public void SetBattlegroundId(uint val, BattlegroundTypeId bgTypeId, BattlegroundQueueTypeId queueId = default)
        {
            m_bgData.bgInstanceID = val;
            m_bgData.bgTypeID = bgTypeId;
            m_bgData.queueId = queueId;
        }

        public uint AddBattlegroundQueueId(BattlegroundQueueTypeId val)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == default || m_bgBattlegroundQueueID[i].bgQueueTypeId == val)
                {
                    m_bgBattlegroundQueueID[i].bgQueueTypeId = val;
                    m_bgBattlegroundQueueID[i].invitedToInstance = 0;
                    m_bgBattlegroundQueueID[i].joinTime = (uint)GameTime.GetGameTime();
                    m_bgBattlegroundQueueID[i].mercenary = HasAura(BattlegroundConst.SpellMercenaryContractHorde) || HasAura(BattlegroundConst.SpellMercenaryContractAlliance);
                    return i;
                }
            }
            return SharedConst.MaxPlayerBGQueues;
        }

        public bool HasFreeBattlegroundQueueId()
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == default)
                    return true;
            return false;
        }

        public void RemoveBattlegroundQueueId(BattlegroundQueueTypeId val)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == val)
                {
                    m_bgBattlegroundQueueID[i].bgQueueTypeId = default;
                    m_bgBattlegroundQueueID[i].invitedToInstance = 0;
                    m_bgBattlegroundQueueID[i].joinTime = 0;
                    m_bgBattlegroundQueueID[i].mercenary = false;
                    return;
                }
            }
        }

        public void SetInviteForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, uint instanceId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    m_bgBattlegroundQueueID[i].invitedToInstance = instanceId;
        }

        public bool IsInvitedForBattlegroundInstance(uint instanceId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].invitedToInstance == instanceId)
                    return true;
            return false;
        }

        void SetMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, bool mercenary)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    m_bgBattlegroundQueueID[i].mercenary = mercenary;
        }

        public bool IsMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    return m_bgBattlegroundQueueID[i].mercenary;
            return false;
        }

        public WorldLocation GetBattlegroundEntryPoint() { return m_bgData.joinPos; }

        public bool InBattleground() { return m_bgData.bgInstanceID != 0; }
        public uint GetBattlegroundId() { return m_bgData.bgInstanceID; }
        public BattlegroundTypeId GetBattlegroundTypeId() { return m_bgData.bgTypeID; }

        public uint GetBattlegroundQueueJoinTime(BattlegroundQueueTypeId bgQueueTypeId)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == bgQueueTypeId)
                    return m_bgBattlegroundQueueID[i].joinTime;
            return 0;
        }

        public bool CanUseBattlegroundObject(GameObject gameobject)
        {
            // It is possible to call this method with a null pointer, only skipping faction check.
            if (gameobject != null)
            {
                FactionTemplateRecord playerFaction = GetFactionTemplateEntry();
                FactionTemplateRecord faction = CliDB.FactionTemplateStorage.LookupByKey(gameobject.GetFaction());

                if (playerFaction != null && faction != null && !playerFaction.IsFriendlyTo(faction))
                    return false;
            }

            bool hasRecentlyDroppedFlagDebuff = HasAura(aura =>
            {
                if (aura.GetSpellInfo().Id == BattlegroundConst.SpellRecentlyDroppedAllianceFlag)
                    return true;
                else if (aura.GetSpellInfo().Id == BattlegroundConst.SpellRecentlyDroppedHordeFlag)
                    return true;
                else if (aura.GetSpellInfo().Id == BattlegroundConst.SpellRecentlyDroppedNeutralFlag)
                    return true;
                return false;
            });

            // BUG: sometimes when player clicks on flag in AB - client won't send gameobject_use, only gameobject_report_use packet
            // Note: Mount, stealth and invisibility will be removed when used
            return (!IsTotalImmune() &&                            // Damage immune
            !hasRecentlyDroppedFlagDebuff &&       // Still has recently held flag debuff
            IsAlive());                                    // Alive
        }

        public bool CanCaptureTowerPoint()
        {
            return (!HasStealthAura() &&                            // not stealthed
                    !HasInvisibilityAura() &&                       // not invisible
                    IsAlive());                                     // live player
        }

        public void SetBattlegroundEntryPoint()
        {
            // Taxi path store
            if (!m_taxi.Empty())
            {
                m_bgData.mountSpell = 0;
                m_bgData.taxiPath[0] = m_taxi.GetTaxiSource();
                m_bgData.taxiPath[1] = m_taxi.GetTaxiDestination();

                // On taxi we don't need check for dungeon
                m_bgData.joinPos = new WorldLocation(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
            }
            else
            {
                m_bgData.ClearTaxiPath();

                // Mount spell id storing
                if (IsMounted())
                {
                    var auras = GetAuraEffectsByType(AuraType.Mounted);
                    if (!auras.Empty())
                        m_bgData.mountSpell = auras[0].GetId();
                }
                else
                    m_bgData.mountSpell = 0;

                // If map is dungeon find linked graveyard
                if (GetMap().IsDungeon())
                {
                    WorldSafeLocsEntry entry = Global.ObjectMgr.GetClosestGraveyard(this, GetTeam(), this);
                    if (entry != null)
                        m_bgData.joinPos = entry.Loc;
                    else
                        Log.outError(LogFilter.Player, "SetBattlegroundEntryPoint: Dungeon map {0} has no linked graveyard, setting home location as entry point.", GetMapId());
                }
                // If new entry point is not BG or arena set it
                else if (!GetMap().IsBattlegroundOrArena())
                    m_bgData.joinPos = new WorldLocation(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
            }

            if (m_bgData.joinPos.GetMapId() == 0xFFFFFFFF) // In error cases use homebind position
                m_bgData.joinPos = new WorldLocation(GetHomebind());
        }

        public void SetBGTeam(Team team)
        {
            m_bgData.bgTeam = team;
            SetArenaFaction((byte)(team == Team.Alliance ? 1 : 0));
        }

        public Team GetBGTeam()
        {
            return m_bgData.bgTeam != 0 ? m_bgData.bgTeam : GetTeam();
        }

        public void LeaveBattleground(bool teleportToEntryPoint = true)
        {
            Battleground bg = GetBattleground();
            if (bg != null)
            {
                bg.RemovePlayerAtLeave(GetGUID(), teleportToEntryPoint, true);

                // call after remove to be sure that player resurrected for correct cast
                if (bg.IsBattleground() && !IsGameMaster() && WorldConfig.GetBoolValue(WorldCfg.BattlegroundCastDeserter))
                {
                    if (bg.GetStatus() == BattlegroundStatus.InProgress || bg.GetStatus() == BattlegroundStatus.WaitJoin)
                    {
                        //lets check if player was teleported from BG and schedule delayed Deserter spell cast
                        if (IsBeingTeleportedFar())
                        {
                            ScheduleDelayedOperation(PlayerDelayedOperations.SpellCastDeserter);
                            return;
                        }

                        CastSpell(this, 26013, true);               // Deserter
                    }
                }
            }
        }

        public bool IsDeserter() { return HasAura(26013); }

        public bool CanJoinToBattleground(BattlegroundTemplate bg)
        {
            RBACPermissions perm = RBACPermissions.JoinNormalBg;
            if (bg.IsArena())
                perm = RBACPermissions.JoinArenas;
            else if (Global.BattlegroundMgr.IsRandomBattleground(bg.Id))
                perm = RBACPermissions.JoinRandomBg;

            return GetSession().HasPermission(perm);
        }

        public void ClearAfkReports() { m_bgData.bgAfkReporter.Clear(); }

        bool CanReportAfkDueToLimit()
        {
            // a player can complain about 15 people per 5 minutes
            if (m_bgData.bgAfkReportedCount++ >= 15)
                return false;

            return true;
        }

        /// <summary>
        /// This player has been blamed to be inactive in a Battleground
        /// </summary>
        /// <param name="reporter"></param>
        public void ReportedAfkBy(Player reporter)
        {
            ReportPvPPlayerAFKResult reportAfkResult = new();
            reportAfkResult.Offender = GetGUID();
            Battleground bg = GetBattleground();
            // Battleground also must be in progress!
            if (bg == null || bg != reporter.GetBattleground() || GetEffectiveTeam() != reporter.GetEffectiveTeam() || bg.GetStatus() != BattlegroundStatus.InProgress)
            {
                reporter.SendPacket(reportAfkResult);
                return;
            }

            // check if player has 'Idle' or 'Inactive' debuff
            if (!m_bgData.bgAfkReporter.Contains(reporter.GetGUID()) && !HasAura(43680) && !HasAura(43681) && reporter.CanReportAfkDueToLimit())
            {
                m_bgData.bgAfkReporter.Add(reporter.GetGUID());
                // by default 3 players have to complain to apply debuff
                if (m_bgData.bgAfkReporter.Count >= WorldConfig.GetIntValue(WorldCfg.BattlegroundReportAfk))
                {
                    // cast 'Idle' spell
                    CastSpell(this, 43680, true);
                    m_bgData.bgAfkReporter.Clear();
                    reportAfkResult.NumBlackMarksOnOffender = (byte)m_bgData.bgAfkReporter.Count;
                    reportAfkResult.NumPlayersIHaveReported = reporter.m_bgData.bgAfkReportedCount;
                    reportAfkResult.Result = ReportPvPPlayerAFKResult.ResultCode.Success;
                }
            }

            reporter.SendPacket(reportAfkResult);
        }

        public bool GetRandomWinner() { return m_IsBGRandomWinner; }
        public void SetRandomWinner(bool isWinner)
        {
            m_IsBGRandomWinner = isWinner;
            if (m_IsBGRandomWinner)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_BATTLEGROUND_RANDOM);
                stmt.AddValue(0, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public bool GetBGAccessByLevel(BattlegroundTypeId bgTypeId)
        {
            // get a template bg instead of running one
            BattlegroundTemplate bg = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bg == null)
                return false;

            // limit check leel to dbc compatible level range
            uint level = GetLevel();
            if (level > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                level = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);

            if (level < bg.GetMinLevel() || level > bg.GetMaxLevel())
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
        public uint GetArenaTeamId(byte slot) { return 0; }
        public void SetArenaTeamIdInvited(uint ArenaTeamId) { m_ArenaTeamIdInvited = ArenaTeamId; }
        public uint GetArenaTeamIdInvited() { return m_ArenaTeamIdInvited; }
        public uint GetRBGPersonalRating() { return GetArenaPersonalRating(3); }

        public uint GetArenaPersonalRating(byte slot)
        {
            PVPInfo pvpInfo = GetPvpInfoForBracket(slot);
            if (pvpInfo != null)
                return pvpInfo.Rating;

            return 0;
        }

        public PVPInfo GetPvpInfoForBracket(byte bracket)
        {
            int index = m_activePlayerData.PvpInfo.FindIndexIf(pvpInfo =>
            {
                return pvpInfo.Bracket == bracket && !pvpInfo.Disqualified;
            });
            if (index >= 0)
                return m_activePlayerData.PvpInfo[index];

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
