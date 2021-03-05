﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Cache;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.PvP;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Player
    {
        //PvP
        public void UpdateHonorFields()
        {
            // called when rewarding honor and at each save
            var now = Time.UnixTime;
            var today = (Time.UnixTime / Time.Day) * Time.Day;

            if (m_lastHonorUpdateTime < today)
            {
                var yesterday = today - Time.Day;

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
                if (!victim || victim == this || !victim.IsTypeId(TypeId.Player))
                    return false;

                if (GetBGTeam() == victim.ToPlayer().GetBGTeam())
                    return false;

                return true;
            }

            // 'Inactive' this aura prevents the player from gaining honor points and BattlegroundTokenizer
            if (HasAura(BattlegroundConst.SpellAuraPlayerInactive))
                return false;

            var victim_guid = ObjectGuid.Empty;
            uint victim_rank = 0;

            // need call before fields update to have chance move yesterday data to appropriate fields before today data change.
            UpdateHonorFields();

            // do not reward honor in arenas, but return true to enable onkill spellproc
            if (InBattleground() && GetBattleground() && GetBattleground().IsArena())
                return true;

            // Promote to float for calculations
            float honor_f = honor;

            if (honor_f <= 0)
            {
                if (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                    return false;

                victim_guid = victim.GetGUID();
                var plrVictim = victim.ToPlayer();
                if (plrVictim)
                {
                    if (GetTeam() == plrVictim.GetTeam() && !Global.WorldMgr.IsFFAPvPRealm())
                        return false;

                    var k_level = (byte)GetLevel();
                    var k_grey = (byte)Formulas.GetGrayLevel(k_level);
                    var v_level = (byte)victim.GetLevelForTarget(this);

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
                    UpdateCriteria(CriteriaTypes.EarnHonorableKill);
                    UpdateCriteria(CriteriaTypes.HkClass, (uint)victim.GetClass());
                    UpdateCriteria(CriteriaTypes.HkRace, (uint)victim.GetRace());
                    UpdateCriteria(CriteriaTypes.HonorableKillAtArea, GetAreaId());
                    UpdateCriteria(CriteriaTypes.HonorableKill, 1, 0, 0, victim);
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
            var data = new PvPCredit();
            data.Honor = honor;
            data.OriginalHonor = honor;
            data.Target = victim_guid;
            data.Rank = victim_rank;

            SendPacket(data);

            AddHonorXP((uint)honor);

            if (InBattleground() && honor > 0)
            {
                var bg = GetBattleground();
                if (bg != null)
                {
                    bg.UpdatePlayerScore(this, ScoreType.BonusHonor, (uint)honor, false); //false: prevent looping
                }
            }

            if (WorldConfig.GetBoolValue(WorldCfg.PvpTokenEnable) && pvptoken)
            {
                if (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                    return true;

                if (victim.IsTypeId(TypeId.Player))
                {
                    // Check if allowed to receive it in current map
                    var MapType = WorldConfig.GetIntValue(WorldCfg.PvpTokenMapType);
                    if ((MapType == 1 && !InBattleground() && !IsFFAPvP())
                        || (MapType == 2 && !IsFFAPvP())
                        || (MapType == 3 && !InBattleground()))
                        return true;

                    var itemId = WorldConfig.GetUIntValue(WorldCfg.PvpTokenId);
                    var count = WorldConfig.GetUIntValue(WorldCfg.PvpTokenCount);

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

        private void _InitHonorLevelOnLoadFromDB(uint honor, uint honorLevel)
        {
            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.HonorLevel), honorLevel);
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

            var charTitlesEntry = CliDB.CharTitlesStorage.LookupByKey(rewardPackEntry.CharTitleID);
            if (charTitlesEntry != null)
                SetTitle(charTitlesEntry);

            ModifyMoney(rewardPackEntry.Money);

            var rewardCurrencyTypes = Global.DB2Mgr.GetRewardPackCurrencyTypesByRewardID(rewardPackEntry.Id);
            foreach (var currency in rewardCurrencyTypes)
                ModifyCurrency((CurrencyTypes)currency.CurrencyTypeID, currency.Quantity);

            var rewardPackXItems = Global.DB2Mgr.GetRewardPackItemsByRewardID(rewardPackEntry.Id);
            foreach (var rewardPackXItem in rewardPackXItems)
                AddItem(rewardPackXItem.ItemID, rewardPackXItem.ItemQuantity);
        }

        public void AddHonorXP(uint xp)
        {
            uint currentHonorXP = m_activePlayerData.Honor;
            uint nextHonorLevelXP = m_activePlayerData.HonorNextLevel;
            var newHonorXP = currentHonorXP + xp;
            var honorLevel = GetHonorLevel();

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

        private void SetHonorLevel(byte level)
        {
            var oldHonorLevel = (byte)GetHonorLevel();
            if (level == oldHonorLevel)
                return;

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.HonorLevel), level);
            UpdateHonorNextLevel();

            UpdateCriteria(CriteriaTypes.HonorLevelReached);
        }

        private void UpdateHonorNextLevel()
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

        private void ResetPvpTalents()
        {
            foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
            {
                if (talentInfo == null)
                    continue;

                RemovePvpTalent(talentInfo);
            }

            var trans = new SQLTransaction();
            _SaveTalents(trans);
            _SaveSpells(trans);
            DB.Characters.CommitTransaction(trans);
        }

        public TalentLearnResult LearnPvpTalent(uint talentID, byte slot, ref uint spellOnCooldown)
        {
            if (slot >= PlayerConst.MaxPvpTalentSlots)
                return TalentLearnResult.FailedUnknown;

            if (IsInCombat())
                return TalentLearnResult.FailedAffectingCombat;

            if (IsDead())
                return TalentLearnResult.FailedCantDoThatRightNow;

            var talentInfo = CliDB.PvpTalentStorage.LookupByKey(talentID);
            if (talentInfo == null)
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.SpecID != GetPrimarySpecialization())
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.LevelRequired > GetLevel())
                return TalentLearnResult.FailedUnknown;

            if (Global.DB2Mgr.GetRequiredLevelForPvpTalentSlot(slot, GetClass()) > GetLevel())
                return TalentLearnResult.FailedUnknown;

            var talentCategory = CliDB.PvpTalentCategoryStorage.LookupByKey(talentInfo.PvpTalentCategoryID);
            if (talentCategory != null)
                if (!Convert.ToBoolean(talentCategory.TalentSlotMask & (1 << slot)))
                    return TalentLearnResult.FailedUnknown;

            // Check if player doesn't have this talent in other slot
            if (HasPvpTalent(talentID, GetActiveTalentGroup()))
                return TalentLearnResult.FailedUnknown;

            var talent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);
            if (talent != null)
            {
                if (!HasPlayerFlag(PlayerFlags.Resting) && !HasUnitFlag2(UnitFlags2.AllowChangingTalents))
                    return TalentLearnResult.FailedRestArea;

                if (GetSpellHistory().HasCooldown(talent.SpellID))
                {
                    spellOnCooldown = talent.SpellID;
                    return TalentLearnResult.FailedCantRemoveTalent;
                }

                RemovePvpTalent(talent);
            }

            if (!AddPvpTalent(talentInfo, GetActiveTalentGroup(), slot))
                return TalentLearnResult.FailedUnknown;

            return TalentLearnResult.LearnOk;
        }

        private bool AddPvpTalent(PvpTalentRecord talent, byte activeTalentGroup, byte slot)
        {
            //ASSERT(talent);
            var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, $"Player.AddPvpTalent: Spell (ID: {talent.SpellID}) does not exist.");
                return false;
            }

            if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
            {
                Log.outError(LogFilter.Spells, $"Player.AddPvpTalent: Spell (ID: {talent.SpellID}) is invalid");
                return false;
            }

            if (HasPvpRulesEnabled())
                LearnSpell(talent.SpellID, false);

            // Move this to toggle ?
            if (talent.OverridesSpellID != 0)
                AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);

            GetPvpTalentMap(activeTalentGroup)[slot] = talent.Id;

            return true;
        }

        private void RemovePvpTalent(PvpTalentRecord talent)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell(talent.SpellID, true);

            // Move this to toggle ?
            if (talent.OverridesSpellID != 0)
                RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

            // if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
            var talents = GetPvpTalentMap(GetActiveTalentGroup());
            for (var i = 0; i < PlayerConst.MaxPvpTalentSlots; ++i)
            {
                if (talents[i] == talent.Id)
                    talents[i] = 0;
            }
        }

        public void TogglePvpTalents(bool enable)
        {
            var pvpTalents = GetPvpTalentMap(GetActiveTalentGroup());
            foreach (var pvpTalentId in pvpTalents)
            {
                var pvpTalentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalentId);
                if (pvpTalentInfo != null)
                {
                    if (enable)
                        LearnSpell(pvpTalentInfo.SpellID, false);
                    else
                        RemoveSpell(pvpTalentInfo.SpellID, true);
                }
            }
        }

        private bool HasPvpTalent(uint talentID, byte activeTalentGroup)
        {
            return GetPvpTalentMap(activeTalentGroup).Contains(talentID);
        }

        public void EnablePvpRules(bool dueToCombat = false)
        {
            if (HasPvpRulesEnabled())
                return;

            if (!HasSpell(195710)) // Honorable Medallion
                CastSpell(this, 208682); // Learn Gladiator's Medallion

            CastSpell(this, PlayerConst.SpellPvpRulesEnabled);
            if (!dueToCombat)
            {
                var aura = GetAura(PlayerConst.SpellPvpRulesEnabled);
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

            if (GetCombatTimer() == 0)
            { 
                RemoveAurasDueToSpell(PlayerConst.SpellPvpRulesEnabled);
                UpdateItemLevelAreaBasedScaling();
            }
            else
            {
                var aura = GetAura(PlayerConst.SpellPvpRulesEnabled);
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

            var area = CliDB.AreaTableStorage.LookupByKey(areaID);
            if (area != null)
            {
                do
                {
                    if (area.IsSanctuary())
                        return false;

                    if (area.Flags[0].HasAnyFlag(AreaFlags.Arena))
                        return true;

                    if (Global.BattleFieldMgr.GetBattlefieldToZoneId(area.Id) != null)
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

        public bool InBattlegroundQueue()
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId != default)
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

        public void SetBattlegroundId(uint val, BattlegroundTypeId bgTypeId)
        {
            m_bgData.bgInstanceID = val;
            m_bgData.bgTypeID = bgTypeId;
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
            if (gameobject)
            {
                var playerFaction = GetFactionTemplateEntry();
                var faction = CliDB.FactionTemplateStorage.LookupByKey(gameobject.GetFaction());

                if (playerFaction != null && faction != null && !playerFaction.IsFriendlyTo(faction))
                    return false;
            }

            // BUG: sometimes when player clicks on flag in AB - client won't send gameobject_use, only gameobject_report_use packet
            // Note: Mount, stealth and invisibility will be removed when used
            return (!IsTotalImmune() &&                            // Damage immune
            !HasAura(BattlegroundConst.SpellRecentlyDroppedFlag) &&       // Still has recently held flag debuff
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
                    var entry = Global.ObjectMgr.GetClosestGraveYard(this, GetTeam(), this);
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
            m_bgData.bgTeam = (uint)team;
            SetArenaFaction((byte)(team == Team.Alliance ? 1 : 0));
        }

        public Team GetBGTeam()
        {
            return m_bgData.bgTeam != 0 ? (Team)m_bgData.bgTeam : GetTeam();
        }

        public void LeaveBattleground(bool teleportToEntryPoint = true)
        {
            var bg = GetBattleground();
            if (bg)
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

        public bool CanJoinToBattleground(Battleground bg)
        {
            // check Deserter debuff
            if (HasAura(26013))
                return false;

            if (bg.IsArena() && !GetSession().HasPermission(RBACPermissions.JoinArenas))
                return false;

            if (bg.IsRandom() && !GetSession().HasPermission(RBACPermissions.JoinRandomBg))
                return false;

            if (!GetSession().HasPermission(RBACPermissions.JoinNormalBg))
                return false;

            return true;
        }

        public void ClearAfkReports() { m_bgData.bgAfkReporter.Clear(); }

        private bool CanReportAfkDueToLimit()
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
            var reportAfkResult = new ReportPvPPlayerAFKResult();
            reportAfkResult.Offender = GetGUID();
            var bg = GetBattleground();
            // Battleground also must be in progress!
            if (!bg || bg != reporter.GetBattleground() || GetTeam() != reporter.GetTeam() || bg.GetStatus() != BattlegroundStatus.InProgress)
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
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BATTLEGROUND_RANDOM);
                stmt.AddValue(0, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public bool GetBGAccessByLevel(BattlegroundTypeId bgTypeId)
        {
            // get a template bg instead of running one
            var bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
            if (!bg)
                return false;

            // limit check leel to dbc compatible level range
            var level = GetLevel();
            if (level > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                level = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);

            if (level < bg.GetMinLevel() || level > bg.GetMaxLevel())
                return false;

            return true;
        }

        private void SendBGWeekendWorldStates()
        {
            foreach (var bl in CliDB.BattlemasterListStorage.Values)
            {
                if (bl.HolidayWorldState != 0)
                {
                    if (Global.BattlegroundMgr.IsBGWeekend((BattlegroundTypeId)bl.Id))
                        SendUpdateWorldState(bl.HolidayWorldState, 1);
                    else
                        SendUpdateWorldState(bl.HolidayWorldState, 0);
                }
            }
        }

        public void SendPvpRewards()
        {
            //WorldPacket packet(SMSG_REQUEST_PVP_REWARDS_RESPONSE, 24);
            //SendPacket(packet);
        }

        //Battlefields
        private void SendBattlefieldWorldStates()
        {
            // Send misc stuff that needs to be sent on every login, like the battle timers.
            if (WorldConfig.GetBoolValue(WorldCfg.WintergraspEnable))
            {
                var wg = Global.BattleFieldMgr.GetBattlefieldByBattleId(1);//Wintergrasp battle
                if (wg != null)
                {
                    SendUpdateWorldState(3801, (uint)(wg.IsWarTime() ? 0 : 1));
                    var timer = wg.IsWarTime() ? 0 : (wg.GetTimer() / 1000); // 0 - Time to next battle
                    SendUpdateWorldState(4354, (uint)(Time.UnixTime + timer));
                }
            }
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
            var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);
            if (characterInfo == null)
                return;

            for (byte i = 0; i < SharedConst.MaxArenaSlot; ++i)
            {
                var arenaTeamId = characterInfo.ArenaTeamId[i];
                if (arenaTeamId != 0)
                {
                    var arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);
                    if (arenaTeam != null)
                        arenaTeam.DelMember(guid, true);
                }
            }
        }
        public uint GetArenaTeamId(byte slot) { return 0; }
        public uint GetArenaPersonalRating(byte slot) { return m_activePlayerData.PvpInfo[slot].Rating; }
        public void SetArenaTeamIdInvited(uint ArenaTeamId) { m_ArenaTeamIdInvited = ArenaTeamId; }
        public uint GetArenaTeamIdInvited() { return m_ArenaTeamIdInvited; }
        public uint GetRBGPersonalRating() { return m_activePlayerData.PvpInfo[3].Rating; }

        //OutdoorPVP
        public bool IsOutdoorPvPActive()
        {
            return IsAlive() && !HasInvisibilityAura() && !HasStealthAura() && IsPvP() && !HasUnitMovementFlag(MovementFlag.Flying) && !IsInFlight();
        }
        public OutdoorPvP GetOutdoorPvP()
        {
            return Global.OutdoorPvPMgr.GetOutdoorPvPToZoneId(GetZoneId());
        }
    }
}
