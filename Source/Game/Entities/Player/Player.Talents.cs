/*
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
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using static Game.AI.SmartTarget;

namespace Game.Entities
{
    public partial class Player
    {
        public void InitTalentForLevel()
        {
            uint level = GetLevel();
            // talents base at level diff (talents = level - 9 but some can be used already)
            if (level < PlayerConst.MinSpecializationLevel)
            {
                // Remove all talent points
                if (GetUsedTalentCount() > 0)                           // Free any used talents
                {
                    ResetTalents(true);
                    SetFreeTalentPoints(0);
                }
            }
            else
            {
                if (level < WorldConfig.GetIntValue(WorldCfg.MinDualspecLevel) || GetTalentGroupCount() == 0)
                {
                    SetTalentGroupCount(1);
                    SetActiveTalentGroup(0);
                }

                uint talentPointsForLevel = GetNumTalentsAtLevel(GetLevel());

                // if used more that have then reset
                if (GetUsedTalentCount() > talentPointsForLevel)
                {
                    if (!GetSession().HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
                        ResetTalents(true);
                    else
                        SetFreeTalentPoints(0);
                }
                // else update amount of free points
                else
                    SetFreeTalentPoints(talentPointsForLevel - GetUsedTalentCount());
            }

            if (!GetSession().PlayerLoading())
                SendTalentsInfoData(false);   // update at client
        }

        public bool AddTalent(uint spellId, byte spec, bool learning)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "Player.AddTalent: Spell (ID: {0}) does not exist.", spellId);
                return false;
            }

            if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
            {
                Log.outError(LogFilter.Spells, "Player.AddTalent: Spell (ID: {0}) is invalid", spellId);
                return false;
            }

            var itr = GetTalentMap(spec);
            if (itr.ContainsKey(spellId))
            {
                itr[spellId].state = PlayerSpellState.Unchanged;
            }
            else if (Global.DB2Mgr.GetTalentSpellPos(spellId) is TalentSpellPos talentPos)
            {
                CliDB.TalentStorage.TryGetValue(talentPos.TalentID, out TalentRecord talentInfo);
                if (talentInfo != null)
                {
                    for (byte rank = 0; rank < PlayerConst.MaxTalentRank; ++rank)
                    {
                        // skip learning spell and no rank spell case
                        uint rankSpellId = (uint)talentInfo.SpellRank[rank];
                        if (rankSpellId == 0 || rankSpellId == spellId)
                            continue;

                        if (itr.ContainsKey(rankSpellId))
                            itr[rankSpellId].state = PlayerSpellState.Removed;
                    }
                }

                itr[spellId].state = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;
                
                if (learning)
                    RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeTalent);

                return true;
            }
            return false;
        }

        public void RemoveTalent(uint spellId)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell(spellId, true);

            // search for spells that the talent teaches and unlearn them
            foreach (var spellEffectInfo in spellInfo.GetEffects())
                if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                    RemoveSpell(spellEffectInfo.TriggerSpell, true);            

            var talentMap = GetTalentMap(GetActiveTalentGroup());
            // if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
            if (talentMap.ContainsKey(spellId))
                talentMap[spellId].state = PlayerSpellState.Removed;
        }

        public void LearnTalent(uint talentId, uint talentRank)
        {
            uint CurTalentPoints = GetFreeTalentPoints();

            if (CurTalentPoints == 0)
                return;

            if (talentRank >= PlayerConst.MaxTalentRank)
                return;
                        
            TalentRecord talentInfo = CliDB.TalentStorage.LookupByKey(talentId);
            if (talentInfo == null)
                return;

            TalentTabRecord talentTabInfo = CliDB.TalentTabStorage.LookupByKey(talentInfo.TabID);
            if (talentTabInfo == null)
                return;

            // prevent learn talent for different class (cheating)
            if((GetClassMask() & talentTabInfo.ClassMask) == 0)
                return;

            // find current max talent rank (0~5)
            byte curtalent_maxrank = 0; // 0 = not learned any rank
            for (byte rank = PlayerConst.MaxTalentRank - 1; rank >= 0; --rank)
            {
                if (talentInfo.SpellRank[rank] != 0 && HasSpell((uint)talentInfo.SpellRank[rank]))
                {
                    curtalent_maxrank = (byte)(rank + 1);
                    break;
                }
            }

            // we already have same or higher talent rank learned
            if (curtalent_maxrank >= (talentRank + 1))
                return;

            // check if we have enough talent points
            if (CurTalentPoints < (talentRank - curtalent_maxrank + 1))
                return;

            // Check if it requires another talent
            if (talentInfo.PrereqTalent[0] > 0)
            {
                TalentRecord depTalentInfo = CliDB.TalentStorage.LookupByKey(talentInfo.PrereqTalent[0]);
                if (depTalentInfo != null)
                {
                    bool hasEnoughRank = false;
                    for (byte rank = (byte)talentInfo.PrereqRank[0]; rank < PlayerConst.MaxTalentRank; rank++)
                    {
                        if (depTalentInfo.SpellRank[rank] != 0)
                            if (HasSpell((uint)depTalentInfo.SpellRank[rank]))
                                hasEnoughRank = true;
                    }
                    if (!hasEnoughRank)
                        return;
                }
            }

            // Find out how many points we have in this field
            uint spentPoints = 0;

            uint tTab = talentInfo.TabID;
            if (talentInfo.TierID > 0)
                for (uint i = 0; i < CliDB.TalentStorage.GetNumRows(); i++)          // Loop through all talents.
                {
                    TalentRecord tmpTalent = CliDB.TalentStorage.LookupByKey(i);
                    if (tmpTalent != null)                                  // the way talents are tracked
                        if (tmpTalent.TabID == tTab)
                            for (byte rank = 0; rank < PlayerConst.MaxTalentRank; rank++)
                                if (tmpTalent.SpellRank[rank] != 0)
                                    if (HasSpell((uint)tmpTalent.SpellRank[rank]))
                                        spentPoints += (uint)(rank + 1);
                }
                    

            // not have required min points spent in talent tree
            if (spentPoints < (talentInfo.TierID * PlayerConst.MaxTalentRank))
                return;

            // spell not set in talent.dbc
            uint spellid = (uint)talentInfo.SpellRank[talentRank];
            if (spellid == 0)
            {
                Log.outError(LogFilter.Player, "Player::LearnTalent: Talent.dbc has no spellInfo for talent: {0} (spell id = 0)", talentId);
                return;
            }

            // already known
            if (HasSpell(spellid))
                return;

            // learn! (other talent ranks will unlearned at learning)
            LearnSpell(spellid, false);
            AddTalent(spellid, GetActiveTalentGroup(), true);

            Log.outDebug(LogFilter.Misc, "Player::LearnTalent: TalentID: {0} Spell: {1} Group: {2}\n", talentId, spellid, GetActiveTalentGroup());

            // update free talent points
            SetFreeTalentPoints(CurTalentPoints - (talentRank - curtalent_maxrank + 1));
        }

        public void ResetTalentSpecialization()
        {
            // Reset only talents that have different spells for each spec
            Class class_ = GetClass();
            for (uint t = 0; t < PlayerConst.MaxTalentTiers; ++t)
            {
                for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                {
                    if (Global.DB2Mgr.GetTalentsByPosition(class_, t, c).Count > 1)
                    {
                        foreach (TalentRecord talent in Global.DB2Mgr.GetTalentsByPosition(class_, t, c))
                            RemoveTalent((uint)talent.SpellID);
                    }
                }
            }

            ResetPvpTalents();
            RemoveSpecializationSpells();

            ChrSpecializationRecord defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass());
            SetPrimarySpecialization(defaultSpec.Id);
            SetActiveTalentGroup(defaultSpec.OrderIndex);

            LearnSpecializationSpells();

            SendTalentsInfoData(false);
            UpdateItemSetAuras(false);
        }

        bool HasTalent(uint talentId, byte group)
        {
            GetTalentMap(group).TryGetValue(talentId, out PlayerTalent itr);            
                return itr != null && itr.state != PlayerSpellState.Removed;
        }

        uint GetTalentResetCost() { return _specializationInfo.ResetTalentsCost; }
        void SetTalentResetCost(uint cost) { _specializationInfo.ResetTalentsCost = cost; }
        long GetTalentResetTime() { return _specializationInfo.ResetTalentsTime; }
        void SetTalentResetTime(long time_) { _specializationInfo.ResetTalentsTime = time_; }
        public uint GetPrimarySpecialization() { return m_playerData.CurrentSpecID; }
        void SetPrimarySpecialization(uint spec) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.CurrentSpecID), spec); }
        public byte GetActiveTalentGroup() { return _specializationInfo.ActiveGroup; }
        void SetActiveTalentGroup(byte group) { _specializationInfo.ActiveGroup = group; }

        // Loot Spec
        public void SetLootSpecId(uint id) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.LootSpecID), (ushort)id); }
        public uint GetLootSpecId() { return m_activePlayerData.LootSpecID; }

        public uint GetDefaultSpecId()
        {
            return Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass()).Id;
        }

        public void ActivateTalentGroup(ChrSpecializationRecord spec)
        {
            if (GetActiveTalentGroup() == spec.OrderIndex)
                return;

            if (IsNonMeleeSpellCast(false))
                InterruptNonMeleeSpells(false);

            SQLTransaction trans = new();
            _SaveActions(trans);
            DB.Characters.CommitTransaction(trans);

            // TO-DO: We need more research to know what happens with warlock's reagent
            Pet pet = GetPet();
            if (pet)
                RemovePet(pet, PetSaveMode.NotInSlot);

            ClearAllReactives();
            UnsummonAllTotems();
            ExitVehicle();
            RemoveAllControlled();

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeSpec);

            // remove single target auras at other targets
            var scAuras = GetSingleCastAuras();
            foreach (var aura in scAuras)
            {
                if (aura.GetUnitOwner() != this)
                    aura.Remove();
            }

            // Let client clear his current Actions
            SendActionButtons(2);
            foreach (var talentInfo in CliDB.TalentStorage.Values)
            {
                // unlearn only talents for character class
                // some spell learned by one class as normal spells or know at creation but another class learn it as talent,
                // to prevent unexpected lost normal learned spell skip another class talents
                if (talentInfo.ClassID != (int)GetClass())
                    continue;

                if (talentInfo.SpellID == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell((uint)talentInfo.SpellID, true);

                // search for spells that the talent teaches and unlearn them
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                        RemoveSpell(spellEffectInfo.TriggerSpell, true);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell((uint)talentInfo.OverridesSpellID, (uint)talentInfo.SpellID);
            }

            foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell((uint)talentInfo.SpellID, true);

                // search for spells that the talent teaches and unlearn them
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                        RemoveSpell(spellEffectInfo.TriggerSpell, true);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell((uint)talentInfo.OverridesSpellID, (uint)talentInfo.SpellID);
            }

            // Remove spec specific spells
            RemoveSpecializationSpells();

            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
                RemoveAurasDueToSpell(CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID);

            SetActiveTalentGroup(spec.OrderIndex);
            SetPrimarySpecialization(spec.Id);

            foreach (var talentInfo in CliDB.TalentStorage.Values)
            {
                // learn only talents for character class
                if (talentInfo.ClassID != (int)GetClass())
                    continue;

                if (talentInfo.SpellID == 0)
                    continue;

                if (HasTalent(talentInfo.Id, GetActiveTalentGroup()))
                {
                    LearnSpell((uint)talentInfo.SpellID, true);      // add the talent to the PlayerSpellMap
                    if (talentInfo.OverridesSpellID != 0)
                        AddOverrideSpell((uint)talentInfo.OverridesSpellID, (uint)talentInfo.SpellID);
                }
            }

            for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
            {
                PvpTalentRecord talentInfo = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);
                if (talentInfo == null)
                    continue;

                if (talentInfo.SpellID == 0)
                    continue;

                AddPvpTalent(talentInfo, GetActiveTalentGroup(), slot);
            }

            LearnSpecializationSpells();

            if (CanUseMastery())
            {
                for (uint i = 0; i < PlayerConst.MaxMasterySpells; ++i)
                {
                    uint mastery = spec.MasterySpellID[i];
                    if (mastery != 0)
                        LearnSpell(mastery, true);
                }
            }

            InitTalentForLevel();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACTIONS_SPEC);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, GetActiveTalentGroup());

            WorldSession mySess = GetSession();
            mySess.GetQueryProcessor().AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(result =>
            {
                // in case player logs out before db response (player would be deleted in that case)
                Player thisPlayer = mySess.GetPlayer();
                if (thisPlayer != null)
                    thisPlayer.LoadActions(result);
            }));

            UpdateDisplayPower();
            PowerType pw = GetPowerType();
            if (pw != PowerType.Mana)
                SetPower(PowerType.Mana, 0); // Mana must be 0 even if it isn't the active power type.

            SetPower(pw, 0);
            UpdateItemSetAuras(false);

            // update visible transmog
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                Item equippedItem = GetItemByPos(InventorySlots.Bag0, i);
                if (equippedItem)
                    SetVisibleItemSlot(i, equippedItem);
            }

            foreach (uint glyphId in GetGlyphs(spec.OrderIndex))
                CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);

            ActiveGlyphs activeGlyphs = new();
            foreach (uint glyphId in GetGlyphs(spec.OrderIndex))
            {
                List<uint> bindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);
                foreach (uint bindableSpell in bindableSpells)
                    if (HasSpell(bindableSpell) && !m_overrideSpells.ContainsKey(bindableSpell))
                        activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
            }

            activeGlyphs.IsFullUpdate = true;
            SendPacket(activeGlyphs);

            var shapeshiftAuras = GetAuraEffectsByType(AuraType.ModShapeshift);
            foreach (AuraEffect aurEff in shapeshiftAuras)
            {
                aurEff.HandleShapeshiftBoosts(this, false);
                aurEff.HandleShapeshiftBoosts(this, true);
            }
        }

        public Dictionary<uint, PlayerTalent> GetTalentMap(uint spec) { return _specializationInfo.Talents[spec]; }
        public List<ushort> GetGlyphs(byte spec) { return _specializationInfo.Glyphs[spec]; }

        public uint GetNextResetTalentsCost()
        {
            // The first time reset costs 1 gold
            if (GetTalentResetCost() < 1 * MoneyConstants.Gold)
                return 1 * MoneyConstants.Gold;
            // then 5 gold
            else if (GetTalentResetCost() < 5 * MoneyConstants.Gold)
                return 5 * MoneyConstants.Gold;
            // After that it increases in increments of 5 gold
            else if (GetTalentResetCost() < 10 * MoneyConstants.Gold)
                return 10 * MoneyConstants.Gold;
            else
            {
                ulong months = (ulong)(GameTime.GetGameTime() - GetTalentResetTime()) / Time.Month;
                if (months > 0)
                {
                    // This cost will be reduced by a rate of 5 gold per month
                    uint new_cost = (uint)(GetTalentResetCost() - 5 * MoneyConstants.Gold * months);
                    // to a minimum of 10 gold.
                    return new_cost < 10 * MoneyConstants.Gold ? 10 * MoneyConstants.Gold : new_cost;
                }
                else
                {
                    // After that it increases in increments of 5 gold
                    uint new_cost = GetTalentResetCost() + 5 * MoneyConstants.Gold;
                    // until it hits a cap of 50 gold.
                    if (new_cost > 50 * MoneyConstants.Gold)
                        new_cost = 50 * MoneyConstants.Gold;
                    return new_cost;
                }
            }
        }

        public bool ResetTalents(bool noCost = false)
        {
            Global.ScriptMgr.OnPlayerTalentsReset(this, noCost);

            // not need after this call
            if (HasAtLoginFlag(AtLoginFlags.ResetTalents))
                RemoveAtLoginFlag(AtLoginFlags.ResetTalents, true);

            uint talentPointsForLevel = GetNumTalentsAtLevel(GetLevel());

            if (GetUsedTalentCount() == 0)
            {
                SetFreeTalentPoints(talentPointsForLevel);
                return false;
            }

            uint cost = 0;
            if (!noCost && !WorldConfig.GetBoolValue(WorldCfg.NoResetTalentCost))
            {
                cost = GetNextResetTalentsCost();

                if (!HasEnoughMoney(cost))
                {
                    SendBuyError(BuyResult.NotEnoughtMoney, null, 0);
                    return false;
                }
            }

            RemovePet(null, PetSaveMode.NotInSlot, true);

            foreach (var talentInfo in CliDB.TalentStorage.Values)
            {
                CliDB.TalentTabStorage.TryGetValue(talentInfo.TabID, out TalentTabRecord talentTabInfo);
                if (talentTabInfo == null)
                    continue;

                // unlearn only talents for character class
                // some spell learned by one class as normal spells or know at creation but another class learn it as talent,
                // to prevent unexpected lost normal learned spell skip another class talents
                if ((GetClassMask() & (uint)talentTabInfo.ClassMask) == 0)
                    continue;

                for (byte rank = PlayerConst.MaxTalentRank - 1; rank >= 0; --rank)
                {
                    // skip non-existing talent ranks
                    if (talentInfo.SpellRank[rank] == 0)
                        continue;

                    RemoveTalent((uint)talentInfo.SpellRank[rank]);
                }                
            }

            SQLTransaction trans = new();
            _SaveTalents(trans);
            _SaveSpells(trans);
            DB.Characters.CommitTransaction(trans);

            SetFreeTalentPoints(talentPointsForLevel);

            if (!noCost)
            {
                ModifyMoney(-cost);
                UpdateCriteria(CriteriaType.MoneySpentOnRespecs, cost);
                UpdateCriteria(CriteriaType.TotalRespecs, 1);

                SetTalentResetCost(cost);
                SetTalentResetTime(GameTime.GetGameTime());
            }

            return true;
        }

        public void SendTalentsInfoData(bool pet)
        {
            UpdateTalentData packet = new();
            packet.UnspentTalentPoints = GetFreeTalentPoints();
            packet.ActiveGroup = 0;

            for (byte specIdx = 0; specIdx < GetTalentGroupCount(); ++specIdx)
            {
                TalentGroupInfo groupInfoPkt = new();
                groupInfoPkt.SpecID = 0;


                // find class talent tabs (all players have 3 talent tabs)
                uint[] talentTabIds = Global.DB2Mgr.GetTalentTabPages((uint)GetClass());

                for (byte i = 0; i < PlayerConst.MaxTalentTabs; ++i)
                {
                    uint talentTabId = talentTabIds[i];

                    for (uint talentId = 0; talentId < CliDB.TalentStorage.GetNumRows(); ++talentId)
                    {
                        TalentRecord talent = CliDB.TalentStorage.LookupByKey(talentId);
                        if (talent == null)
                            continue;

                        // skip another tab talents
                        if (talent.TabID != talentTabId)
                            continue;

                        // find max talent rank (0~4)
                        int curtalent_maxrank = -1;
                        for (int rank = PlayerConst.MaxTalentRank - 1; rank >= 0; --rank)
                        {
                            if (talent.SpellRank[rank] != 0 && HasTalent((uint)talent.SpellRank[rank], specIdx))
                            {
                                curtalent_maxrank = rank;
                                break;
                            }
                        }

                        // not learned talent
                        if (curtalent_maxrank < 0)
                            continue;

                        TalentInfo talentInfo;
                        talentInfo.TalentID = talent.Id;
                        talentInfo.Rank = (byte)curtalent_maxrank;

                        groupInfoPkt.TalentInfos.Add(talentInfo);
                    }
                }

                groupInfoPkt.GlyphInfo = GetGlyphs(specIdx);

                packet.TalentGroupInfos.Add(groupInfoPkt);
            }

            SendPacket(packet);
        }

        public void SendRespecWipeConfirm(ObjectGuid guid, uint cost, SpecResetType respecType)
        {
            RespecWipeConfirm respecWipeConfirm = new();
            respecWipeConfirm.RespecMaster = guid;
            respecWipeConfirm.Cost = cost;
            respecWipeConfirm.RespecType = respecType;
            SendPacket(respecWipeConfirm);
        }

        //Pvp
        void ResetPvpTalents()
        {
            for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
            {
                foreach (uint talentId in GetPvpTalentMap(spec))
                {
                    var talentInfo = CliDB.PvpTalentStorage.LookupByKey(talentId);
                    if (talentInfo != null)
                        RemovePvpTalent(talentInfo, spec);
                }
            }
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

            if (talentInfo.SpecID != GetPrimarySpecialization())
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.LevelRequired > GetLevel())
                return TalentLearnResult.FailedUnknown;

            if (Global.DB2Mgr.GetRequiredLevelForPvpTalentSlot(slot, GetClass()) > GetLevel())
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
                if (!HasPlayerFlag(PlayerFlags.Resting) && !HasUnitFlag2(UnitFlags2.AllowChangingTalents))
                    return TalentLearnResult.FailedRestArea;

                if (GetSpellHistory().HasCooldown((uint)talent.SpellID))
                {
                    spellOnCooldown = (uint)talent.SpellID;
                    return TalentLearnResult.FailedCantRemoveTalent;
                }

                RemovePvpTalent(talent, GetActiveTalentGroup());
            }

            if (!AddPvpTalent(talentInfo, GetActiveTalentGroup(), slot))
                return TalentLearnResult.FailedUnknown;

            return TalentLearnResult.LearnOk;
        }

        bool AddPvpTalent(PvpTalentRecord talent, byte activeTalentGroup, byte slot)
        {
            //ASSERT(talent);
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)talent.SpellID, Difficulty.None);
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

            if (activeTalentGroup == GetActiveTalentGroup() && HasAuraType(AuraType.PvpTalents))
            {
                LearnSpell((uint)talent.SpellID, true);

                // Move this to toggle ?
                if (talent.OverridesSpellID != 0)
                    AddOverrideSpell((uint)talent.OverridesSpellID, (uint)talent.SpellID);
            }

            GetPvpTalentMap(activeTalentGroup)[slot] = talent.Id;

            return true;
        }

        void RemovePvpTalent(PvpTalentRecord talent, byte activeTalentGroup)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)talent.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell((uint)talent.SpellID, true);

            // Move this to toggle ?
            if (talent.OverridesSpellID != 0)
                RemoveOverrideSpell((uint)talent.OverridesSpellID, (uint)talent.SpellID);

            // if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
            var talents = GetPvpTalentMap(activeTalentGroup);
            for (var i = 0; i < PlayerConst.MaxPvpTalentSlots; ++i)
            {
                if (talents[i] == talent.Id)
                    talents[i] = 0;
            }
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
                    {
                        LearnSpell((uint)pvpTalentInfo.SpellID, false);
                        if (pvpTalentInfo.OverridesSpellID != 0)
                            AddOverrideSpell((uint)pvpTalentInfo.OverridesSpellID, (uint)pvpTalentInfo.SpellID);
                    }
                    else
                    {
                        if (pvpTalentInfo.OverridesSpellID != 0)
                            RemoveOverrideSpell((uint)pvpTalentInfo.OverridesSpellID, (uint)pvpTalentInfo.SpellID);
                        RemoveSpell((uint)pvpTalentInfo.SpellID, true);
                    }
                }
            }
        }

        bool HasPvpTalent(uint talentID, byte activeTalentGroup)
        {
            return GetPvpTalentMap(activeTalentGroup).Contains(talentID);
        }
                
        uint GetUsedTalentCount() { return _specializationInfo.UsedTalentCount; }
        void SetUsedTalentCount(uint count) { _specializationInfo.UsedTalentCount = count; }
        uint GetQuestRewardTalentCount() { return _specializationInfo.QuestRewardTalentCount; }
        void SetQuestRewardTalentCount(uint count) { _specializationInfo.QuestRewardTalentCount = count; }
        byte GetTalentGroupCount() { return _specializationInfo.TalentGroupCount; }
        void SetTalentGroupCount(byte count) { _specializationInfo.TalentGroupCount = count; }
        uint GetFreeTalentPoints() { return m_activePlayerData.CharacterPoints; }
        public void SetFreeTalentPoints(uint points)
        {
            Global.ScriptMgr.OnPlayerFreeTalentPointsChanged(this, points);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.CharacterPoints), points);
        }

        public uint GetNumTalentsAtLevel(uint level)
        {
            var talentsAtLevel = CliDB.NumTalentsAtLevelStorage;
            talentsAtLevel.TryGetValue(level, out NumTalentsAtLevelRecord numTalentsAtLevel);

            if (numTalentsAtLevel == null)
                talentsAtLevel.TryGetValue(talentsAtLevel.GetNumRows() - 1, out numTalentsAtLevel);

            if (numTalentsAtLevel != null)
            {
                switch (GetClass())
                {
                    case Class.Deathknight:
                        {
                            uint talentPointsForLevel = numTalentsAtLevel.NumTalentsDeathKnight;
                            talentPointsForLevel += GetQuestRewardTalentCount();

                            if (talentPointsForLevel > numTalentsAtLevel.NumTalents)
                                talentPointsForLevel = numTalentsAtLevel.NumTalents;

                            return talentPointsForLevel * (uint)WorldCfg.RateTalent;
                        }
                    case Class.DemonHunter:
                        return numTalentsAtLevel.NumTalentsDemonHunter;
                    default:
                        return numTalentsAtLevel.NumTalents * (uint)WorldCfg.RateTalent;
                }
            }

            return 0;
        }

        public void SetGlyphSlot(byte slotIndex, uint slotType) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.GlyphInfos, slotIndex).ModifyValue(m_activePlayerData.GlyphInfos[slotIndex].GlyphSlot), slotType); }
        public uint GetGlyphSlot(byte slotIndex) { return m_activePlayerData.GlyphInfos[slotIndex].GlyphSlot; }
        public void SetGlyph(byte slotIndex, uint glyph)
        {
            GetGlyphs(GetActiveTalentGroup())[slotIndex] = (ushort)glyph;
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.GlyphInfos, slotIndex).ModifyValue(m_activePlayerData.GlyphInfos[slotIndex].Glyph), glyph);
        }
    
        public uint GetGlyph(byte slotIndex) { return _specializationInfo.Glyphs[GetActiveTalentGroup()][slotIndex]; }

        // Only sent on CreateObject
        void InitGlyphsForLevel()
        {
            foreach (GlyphSlotRecord gs in CliDB.GlyphSlotStorage.Values)
            {
                if (gs.ToolTip != 0 && (gs.ToolTip <= PlayerConst.MaxGlyphSlotIndex))
                    SetGlyphSlot((byte)(gs.ToolTip - 1), gs.Id);
            }

            uint level = GetLevel();
            byte value = 0;

            // 0x3F = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 for 80 level
            if (level >= 15)
                value |= (0x01 | 0x02);
            if (level >= 30)
                value |= 0x08;
            if (level >= 50)
                value |= 0x04;
            if (level >= 70)
                value |= 0x10;
            if (level >= 80)
                value |= 0x20;

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.GlyphsEnabled), value);
        }

        void UpdateGlyphsEnabled()
        {
            uint level = GetLevel();
            byte value = 0;

            // 0x3F = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 for 80 level
            if (level >= 15)
                value |= (0x01 | 0x02);
            if (level >= 30)
                value |= 0x08;
            if (level >= 50)
                value |= 0x04;
            if (level >= 70)
                value |= 0x10;
            if (level >= 80)
                value |= 0x20;

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.GlyphsEnabled), value);
        }        
    }
}
