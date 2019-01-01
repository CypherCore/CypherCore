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

using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Network.Packets;
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
            long now = Time.UnixTime;
            long today = (Time.UnixTime / Time.Day) * Time.Day;

            if (m_lastHonorUpdateTime < today)
            {
                long yesterday = today - Time.Day;

                // update yesterday's contribution
                if (m_lastHonorUpdateTime >= yesterday)
                {
                    // this is the first update today, reset today's contribution
                    ushort killsToday = GetUInt16Value(ActivePlayerFields.Kills, 0);
                    SetUInt16Value(ActivePlayerFields.Kills, 0, 0);
                    SetUInt16Value(ActivePlayerFields.Kills, 1, killsToday);

                }
                else
                {
                    // no honor/kills yesterday or today, reset
                    SetUInt32Value(ActivePlayerFields.Kills, 0);
                }
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

            ObjectGuid victim_guid = ObjectGuid.Empty;
            uint victim_rank = 0;

            // need call before fields update to have chance move yesterday data to appropriate fields before today data change.
            UpdateHonorFields();

            // do not reward honor in arenas, but return true to enable onkill spellproc
            if (InBattleground() && GetBattleground() && GetBattleground().isArena())
                return true;

            // Promote to float for calculations
            float honor_f = honor;

            if (honor_f <= 0)
            {
                if (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                    return false;

                victim_guid = victim.GetGUID();
                Player plrVictim = victim.ToPlayer();
                if (plrVictim)
                {
                    if (GetTeam() == plrVictim.GetTeam() && !Global.WorldMgr.IsFFAPvPRealm())
                        return false;

                    byte k_level = (byte)getLevel();
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
                    uint victim_title = victim.GetUInt32Value(PlayerFields.ChosenTitle);
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

                    honor_f = (float)Math.Ceiling(Formulas.hk_honor_at_level_f(k_level) * (v_level - k_grey) / (k_level - k_grey));

                    // count the number of playerkills in one day
                    ApplyModUInt16Value(ActivePlayerFields.Kills, 0, 1, true);
                    // and those in a lifetime
                    ApplyModUInt32Value(ActivePlayerFields.LifetimeHonorableKills, 1, true);
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
            PvPCredit data = new PvPCredit();
            data.Honor = honor;
            data.OriginalHonor = honor;
            data.Target = victim_guid;
            data.Rank = victim_rank;

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
                if (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
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

        void _InitHonorLevelOnLoadFromDB(uint honor, uint honorLevel)
        {
            SetUInt32Value(PlayerFields.HonorLevel, honorLevel);
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
                ModifyCurrency((CurrencyTypes)currency.CurrencyTypeID, currency.Quantity);

            var rewardPackXItems = Global.DB2Mgr.GetRewardPackItemsByRewardID(rewardPackEntry.Id);
            foreach (RewardPackXItemRecord rewardPackXItem in rewardPackXItems)
                AddItem(rewardPackXItem.ItemID, rewardPackXItem.ItemQuantity);
        }

        public void AddHonorXP(uint xp)
        {
            uint currentHonorXP = GetUInt32Value(ActivePlayerFields.Honor);
            uint nextHonorLevelXP = GetUInt32Value(ActivePlayerFields.HonorNextLevel);
            uint newHonorXP = currentHonorXP + xp;
            uint honorLevel = GetHonorLevel();

            if (xp < 1 || getLevel() < PlayerConst.LevelMinHonor || IsMaxHonorLevel())
                return;

            while (newHonorXP >= nextHonorLevelXP)
            {
                newHonorXP -= nextHonorLevelXP;

                if (honorLevel < PlayerConst.MaxHonorLevel)
                    SetHonorLevel((byte)(honorLevel + 1));

                honorLevel = GetHonorLevel();
                nextHonorLevelXP = GetUInt32Value(ActivePlayerFields.HonorNextLevel);
            }

            SetUInt32Value(ActivePlayerFields.Honor, IsMaxHonorLevel() ? 0 : newHonorXP);
        }

        void SetHonorLevel(byte level)
        {
            byte oldHonorLevel = (byte)GetHonorLevel();
            if (level == oldHonorLevel)
                return;

            SetUInt32Value(PlayerFields.HonorLevel, level);
            UpdateHonorNextLevel();

            UpdateCriteria(CriteriaTypes.HonorLevelReached);
        }

        void UpdateHonorNextLevel()
        {
            // 5500 at honor level 1
            // no idea what between here
            // 8800 at honor level ~14 (never goes above 8800)
            SetUInt32Value(ActivePlayerFields.HonorNextLevel, 8800);
        }

        public uint GetHonorLevel() { return GetUInt32Value(PlayerFields.HonorLevel); }
        public bool IsMaxHonorLevel() { return GetHonorLevel() == PlayerConst.MaxHonorLevel; }

        public void ActivatePvpItemLevels(bool activate) { _usePvpItemLevels = activate; }
        public bool IsUsingPvpItemLevels() { return _usePvpItemLevels; }

        void ResetPvpTalents()
        {
            foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
            {
                if (talentInfo == null)
                    continue;

                RemovePvpTalent(talentInfo);
            }

            SQLTransaction trans = new SQLTransaction();
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

            PvpTalentRecord talentInfo = CliDB.PvpTalentStorage.LookupByKey(talentID);
            if (talentInfo == null)
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.SpecID != GetInt32Value(PlayerFields.CurrentSpecId))
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.LevelRequired > getLevel())
                return TalentLearnResult.FailedUnknown;

            if (Global.DB2Mgr.GetRequiredLevelForPvpTalentSlot(slot, GetClass()) > getLevel())
                return TalentLearnResult.FailedUnknown;

            PvpTalentCategoryRecord talentCategory = CliDB.PvpTalentCategoryStorage.LookupByKey(talentInfo.PvpTalentCategoryID);
            if (talentCategory != null)
                if (!Convert.ToBoolean(talentCategory.TalentSlotMask & (1 << slot)))
                    return TalentLearnResult.FailedUnknown;

            // Check if player doesn't have this talent in other slot
            if (HasPvpTalent(talentID, GetActiveTalentGroup()))
                return TalentLearnResult.FailedUnknown;

            PvpTalentRecord talent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);
            if (talent != null)
            {
                if (!HasFlag(PlayerFields.Flags, PlayerFlags.Resting) && !HasFlag(UnitFields.Flags2, UnitFlags2.AllowChangingTalents))
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

        bool AddPvpTalent(PvpTalentRecord talent, byte activeTalentGroup, byte slot)
        {
            //ASSERT(talent);
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID);
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

        void RemovePvpTalent(PvpTalentRecord talent)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID);
            if (spellInfo == null)
                return;

            RemoveSpell(talent.SpellID, true);

            // Move this to toggle ?
            if (talent.OverridesSpellID != 0)
                RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

            //todo check this
            // if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
            GetPvpTalentMap(GetActiveTalentGroup()).Remove(talent.Id);
        }

        public void TogglePvpTalents(bool enable)
        {
            var pvpTalents = GetPvpTalentMap(GetActiveTalentGroup());
            foreach (uint pvpTalentId in pvpTalents)
            {
                PvpTalentRecord pvpTalentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalentId);
                if (pvpTalentInfo != null)
                {
                    if (enable)
                        LearnSpell(pvpTalentInfo.SpellID, false);
                    else
                        RemoveSpell(pvpTalentInfo.SpellID, true);
                }
            }
        }

        bool HasPvpTalent(uint talentID, byte activeTalentGroup)
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

            if (GetCombatTimer() == 0)
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

                    if (area.Flags[0].HasAnyFlag(AreaFlags.Arena))
                        return true;

                    if (Global.BattleFieldMgr.GetBattlefieldToZoneId(area.Id) != null)
                        return true;

                    area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

                } while (area != null);
            }

            return false;
        }

        public Array<uint> GetPvpTalentMap(byte spec) { return _specializationInfo.PvpTalents[spec]; }

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
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId != BattlegroundQueueTypeId.None)
                    return true;
            return false;
        }

        public BattlegroundQueueTypeId GetBattlegroundQueueTypeId(uint index)
        {
            if (index < SharedConst.MaxPlayerBGQueues)
                return m_bgBattlegroundQueueID[index].bgQueueTypeId;

            return BattlegroundQueueTypeId.None;
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
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == BattlegroundQueueTypeId.None || m_bgBattlegroundQueueID[i].bgQueueTypeId == val)
                {
                    m_bgBattlegroundQueueID[i].bgQueueTypeId = val;
                    m_bgBattlegroundQueueID[i].invitedToInstance = 0;
                    m_bgBattlegroundQueueID[i].joinTime = Time.GetMSTime();
                    return i;
                }
            }
            return SharedConst.MaxPlayerBGQueues;
        }

        public bool HasFreeBattlegroundQueueId()
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == BattlegroundQueueTypeId.None)
                    return true;
            return false;
        }

        public void RemoveBattlegroundQueueId(BattlegroundQueueTypeId val)
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                if (m_bgBattlegroundQueueID[i].bgQueueTypeId == val)
                {
                    m_bgBattlegroundQueueID[i].bgQueueTypeId = BattlegroundQueueTypeId.None;
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
                FactionTemplateRecord playerFaction = GetFactionTemplateEntry();
                FactionTemplateRecord faction = CliDB.FactionTemplateStorage.LookupByKey(gameobject.GetUInt32Value(GameObjectFields.Faction));

                if (playerFaction != null && faction != null && !playerFaction.IsFriendlyTo(faction))
                    return false;
            }

            // BUG: sometimes when player clicks on flag in AB - client won't send gameobject_use, only gameobject_report_use packet
            // Note: Mount, stealth and invisibility will be removed when used
            return (!isTotalImmune() &&                            // Damage immune
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
            if (!m_taxi.empty())
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
                    WorldSafeLocsRecord entry = Global.ObjectMgr.GetClosestGraveYard(this, GetTeam(), this);
                    if (entry != null)
                        m_bgData.joinPos = new WorldLocation(entry.MapID, entry.Loc.X, entry.Loc.Y, entry.Loc.Z, 0.0f);
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
            SetByteValue(PlayerFields.Bytes4, PlayerFieldOffsets.Bytes4OffsetArenaFaction, (byte)(team == Team.Alliance ? 1 : 0));
        }

        public Team GetBGTeam()
        {
            return m_bgData.bgTeam != 0 ? (Team)m_bgData.bgTeam : GetTeam();
        }

        public void LeaveBattleground(bool teleportToEntryPoint = true)
        {
            Battleground bg = GetBattleground();
            if (bg)
            {
                bg.RemovePlayerAtLeave(GetGUID(), teleportToEntryPoint, true);

                // call after remove to be sure that player resurrected for correct cast
                if (bg.isBattleground() && !IsGameMaster() && WorldConfig.GetBoolValue(WorldCfg.BattlegroundCastDeserter))
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

            if (bg.isArena() && !GetSession().HasPermission(RBACPermissions.JoinArenas))
                return false;

            if (bg.IsRandom() && !GetSession().HasPermission(RBACPermissions.JoinRandomBg))
                return false;

            if (!GetSession().HasPermission(RBACPermissions.JoinNormalBg))
                return false;

            return true;
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
            ReportPvPPlayerAFKResult reportAfkResult = new ReportPvPPlayerAFKResult();
            reportAfkResult.Offender = GetGUID();
            Battleground bg = GetBattleground();
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
            uint level = getLevel();
            if (level > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                level = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);

            if (level < bg.GetMinLevel() || level > bg.GetMaxLevel())
                return false;

            return true;
        }

        void SendBGWeekendWorldStates()
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
        void SendBattlefieldWorldStates()
        {
            // Send misc stuff that needs to be sent on every login, like the battle timers.
            if (WorldConfig.GetBoolValue(WorldCfg.WintergraspEnable))
            {
                BattleField wg = Global.BattleFieldMgr.GetBattlefieldByBattleId(1);//Wintergrasp battle
                if (wg != null)
                {
                    SendUpdateWorldState(3801, (uint)(wg.IsWarTime() ? 0 : 1));
                    uint timer = wg.IsWarTime() ? 0 : (wg.GetTimer() / 1000); // 0 - Time to next battle
                    SendUpdateWorldState(4354, (uint)(Time.UnixTime + timer));
                }
            }
        }

        //Arenas
        public void SetArenaTeamInfoField(byte slot, ArenaTeamInfoType type, uint value)
        {
            SetUInt32Value(ActivePlayerFields.ArenaTeamInfo + (slot * (int)ArenaTeamInfoType.End) + (int)type, value);
        }

        public void SetInArenaTeam(uint ArenaTeamId, byte slot, byte type)
        {
            SetArenaTeamInfoField(slot, ArenaTeamInfoType.Id, ArenaTeamId);
            SetArenaTeamInfoField(slot, ArenaTeamInfoType.Type, type);
        }

        public static uint GetArenaTeamIdFromDB(ObjectGuid guid, byte type)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ARENA_TEAM_ID_BY_PLAYER_GUID);
            stmt.AddValue(0, guid.GetCounter());
            stmt.AddValue(1, type);
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            return result.Read<uint>(0);
        }

        public static void LeaveAllArenaTeams(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PLAYER_ARENA_TEAMS);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return;

            do
            {
                uint arenaTeamId = result.Read<uint>(0);
                if (arenaTeamId != 0)
                {
                    ArenaTeam arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);
                    if (arenaTeam != null)
                        arenaTeam.DelMember(guid, true);
                }
            }
            while (result.NextRow());
        }
        public uint GetArenaTeamId(byte slot) { return GetUInt32Value(ActivePlayerFields.ArenaTeamInfo + (slot * (int)ArenaTeamInfoType.End) + (int)ArenaTeamInfoType.Id); }
        public uint GetArenaPersonalRating(byte slot) { return GetUInt32Value(ActivePlayerFields.ArenaTeamInfo + (slot * (int)ArenaTeamInfoType.End) + (int)ArenaTeamInfoType.PersonalRating); }
        public void SetArenaTeamIdInvited(uint ArenaTeamId) { m_ArenaTeamIdInvited = ArenaTeamId; }
        public uint GetArenaTeamIdInvited() { return m_ArenaTeamIdInvited; }
        public uint GetRBGPersonalRating() { return 0; }

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
