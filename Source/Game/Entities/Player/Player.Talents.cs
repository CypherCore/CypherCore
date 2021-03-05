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
using Game.DataStorage;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public void InitTalentForLevel()
        {
            var level = GetLevel();
            // talents base at level diff (talents = level - 9 but some can be used already)
            if (level < PlayerConst.MinSpecializationLevel)
                ResetTalentSpecialization();

            var talentTiers = Global.DB2Mgr.GetNumTalentsAtLevel(level, GetClass());
            if (level < 15)
            {
                // Remove all talent points
                ResetTalents(true);
            }
            else
            {
                if (!GetSession().HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
                {
                    for (var t = talentTiers; t < PlayerConst.MaxTalentTiers; ++t)
                        for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                            foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), t, c))
                                RemoveTalent(talent);
                }
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.MaxTalentTiers), talentTiers);

            if (!GetSession().PlayerLoading())
                SendTalentsInfoData();   // update at client
        }

        public bool AddTalent(TalentRecord talent, byte spec, bool learning)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "Player.AddTalent: Spell (ID: {0}) does not exist.", talent.SpellID);
                return false;
            }

            if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
            {
                Log.outError(LogFilter.Spells, "Player.AddTalent: Spell (ID: {0}) is invalid", talent.SpellID);
                return false;
            }

            if (talent.OverridesSpellID != 0)
                AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);

            if (GetTalentMap(spec).ContainsKey(talent.Id))
                GetTalentMap(spec)[talent.Id] = PlayerSpellState.Unchanged;
            else
                GetTalentMap(spec)[talent.Id] = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

            return true;
        }

        public void RemoveTalent(TalentRecord talent)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell(talent.SpellID, true);

            // search for spells that the talent teaches and unlearn them
            foreach (var effect in spellInfo.GetEffects())
                if (effect != null && effect.TriggerSpell > 0 && effect.Effect == SpellEffectName.LearnSpell)
                    RemoveSpell(effect.TriggerSpell, true);

            if (talent.OverridesSpellID != 0)
                RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

            var talentMap = GetTalentMap(GetActiveTalentGroup());
            // if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
            if (talentMap.ContainsKey(talent.Id))
                talentMap[talent.Id] = PlayerSpellState.Removed;
        }

        public TalentLearnResult LearnTalent(uint talentId, ref int spellOnCooldown)
        {
            if (IsInCombat())
                return TalentLearnResult.FailedAffectingCombat;

            if (IsDead())
                return TalentLearnResult.FailedCantDoThatRightNow;

            if (GetPrimarySpecialization() == 0)
                return TalentLearnResult.FailedNoPrimaryTreeSelected;

            var talentInfo = CliDB.TalentStorage.LookupByKey(talentId);
            if (talentInfo == null)
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.SpecID != 0 && talentInfo.SpecID != GetPrimarySpecialization())
                return TalentLearnResult.FailedUnknown;

            // prevent learn talent for different class (cheating)
            if (talentInfo.ClassID != (byte)GetClass())
                return TalentLearnResult.FailedUnknown;

            // check if we have enough talent points
            if (talentInfo.TierID >= m_activePlayerData.MaxTalentTiers)
                return TalentLearnResult.FailedUnknown;

            // TODO: prevent changing talents that are on cooldown

            // Check if there is a different talent for us to learn in selected slot
            // Example situation:
            // Warrior talent row 2 slot 0
            // Talent.dbc has an entry for each specialization
            // but only 2 out of 3 have SpecID != 0
            // We need to make sure that if player is in one of these defined specs he will not learn the other choice
            TalentRecord bestSlotMatch = null;
            foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), talentInfo.TierID, talentInfo.ColumnIndex))
            {
                if (talent.SpecID == 0)
                    bestSlotMatch = talent;

                else if (talent.SpecID == GetPrimarySpecialization())
                {
                    bestSlotMatch = talent;
                    break;
                }
            }

            if (talentInfo != bestSlotMatch)
                return TalentLearnResult.FailedUnknown;

            // Check if player doesn't have any talent in current tier
            for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
            {
                foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), talentInfo.TierID, c))
                {
                    if (talent.SpecID != 0 && talent.SpecID != GetPrimarySpecialization())
                        continue;

                    if (!HasTalent(talent.Id, GetActiveTalentGroup()))
                        continue;
                    
                    if (!HasPlayerFlag(PlayerFlags.Resting) && HasUnitFlag2(UnitFlags2.AllowChangingTalents))
                        return TalentLearnResult.FailedRestArea;

                    if (GetSpellHistory().HasCooldown(talent.SpellID))
                    {
                        spellOnCooldown = (int)talent.SpellID;
                        return TalentLearnResult.FailedCantRemoveTalent;
                    }

                    RemoveTalent(talent);
                }
            }

            // spell not set in talent.dbc
            var spellid = talentInfo.SpellID;
            if (spellid == 0)
            {
                Log.outError(LogFilter.Player, "Player.LearnTalent: Talent.dbc has no spellInfo for talent: {0} (spell id = 0)", talentId);
                return TalentLearnResult.FailedUnknown;
            }

            // already known
            if (HasTalent(talentId, GetActiveTalentGroup()) || HasSpell(spellid))
                return TalentLearnResult.FailedUnknown;

            if (!AddTalent(talentInfo, GetActiveTalentGroup(), true))
                return TalentLearnResult.FailedUnknown;

            LearnSpell(spellid, false);

            Log.outDebug(LogFilter.Misc, "Player.LearnTalent: TalentID: {0} Spell: {1} Group: {2}", talentId, spellid, GetActiveTalentGroup());

            return TalentLearnResult.LearnOk;
        }

        public void ResetTalentSpecialization()
        {
            // Reset only talents that have different spells for each spec
            var class_ = GetClass();
            for (uint t = 0; t < PlayerConst.MaxTalentTiers; ++t)
            {
                for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                {
                    if (Global.DB2Mgr.GetTalentsByPosition(class_, t, c).Count > 1)
                    {
                        foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(class_, t, c))
                            RemoveTalent(talent);
                    }
                }
            }

            RemoveSpecializationSpells();

            var defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass());
            SetPrimarySpecialization(defaultSpec.Id);
            SetActiveTalentGroup(defaultSpec.OrderIndex);

            LearnSpecializationSpells();

            SendTalentsInfoData();
            UpdateItemSetAuras(false);
        }

        private bool HasTalent(uint talentId, byte group)
        {
            return GetTalentMap(group).ContainsKey(talentId) && GetTalentMap(group)[talentId] != PlayerSpellState.Removed;
        }

        private uint GetTalentResetCost() { return _specializationInfo.ResetTalentsCost; }
        private void SetTalentResetCost(uint cost) { _specializationInfo.ResetTalentsCost = cost; }
        private long GetTalentResetTime() { return _specializationInfo.ResetTalentsTime; }
        private void SetTalentResetTime(long time_) { _specializationInfo.ResetTalentsTime = time_; }
        public uint GetPrimarySpecialization() { return m_playerData.CurrentSpecID; }
        private void SetPrimarySpecialization(uint spec) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.CurrentSpecID), spec); }
        public byte GetActiveTalentGroup() { return _specializationInfo.ActiveGroup; }
        private void SetActiveTalentGroup(byte group) { _specializationInfo.ActiveGroup = group; }

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

            var trans = new SQLTransaction();
            _SaveActions(trans);
            DB.Characters.CommitTransaction(trans);

            // TO-DO: We need more research to know what happens with warlock's reagent
            var pet = GetPet();
            if (pet)
                RemovePet(pet, PetSaveMode.NotInSlot);

            ClearAllReactives();
            UnsummonAllTotems();
            ExitVehicle();
            RemoveAllControlled();

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

                var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell(talentInfo.SpellID, true);

                // search for spells that the talent teaches and unlearn them
                foreach (var effect in spellInfo.GetEffects())
                    if (effect != null && effect.TriggerSpell > 0 && effect.Effect == SpellEffectName.LearnSpell)
                        RemoveSpell(effect.TriggerSpell, true);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
            }

            foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell(talentInfo.SpellID, true);

                // search for spells that the talent teaches and unlearn them
                foreach (var effect in spellInfo.GetEffects())
                    if (effect != null && effect.TriggerSpell > 0 && effect.Effect == SpellEffectName.LearnSpell)
                        RemoveSpell(effect.TriggerSpell, true);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
            }

            // Remove spec specific spells
            RemoveSpecializationSpells();

            foreach (var glyphId in GetGlyphs(GetActiveTalentGroup()))
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
                    LearnSpell(talentInfo.SpellID, false);      // add the talent to the PlayerSpellMap
                    if (talentInfo.OverridesSpellID != 0)
                        AddOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
                }
            }

            for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
            {
                var talentInfo = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);
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
                    var mastery = spec.MasterySpellID[i];
                    if (mastery != 0)
                        LearnSpell(mastery, false);
                }
            }

            InitTalentForLevel();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACTIONS_SPEC);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, GetActiveTalentGroup());

            var mySess = GetSession();
            mySess.GetQueryProcessor().AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(result =>
            {
                // in case player logs out before db response (player would be deleted in that case)
                var thisPlayer = mySess.GetPlayer();
                if (thisPlayer != null)
                    thisPlayer.LoadActions(result);
            }));

            UpdateDisplayPower();
            var pw = GetPowerType();
            if (pw != PowerType.Mana)
                SetPower(PowerType.Mana, 0); // Mana must be 0 even if it isn't the active power type.

            SetPower(pw, 0);
            UpdateItemSetAuras(false);

            // update visible transmog
            for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                Item equippedItem = GetItemByPos(InventorySlots.Bag0, i);
                if (equippedItem)
                    SetVisibleItemSlot(i, equippedItem);
            }

            foreach (var glyphId in GetGlyphs(spec.OrderIndex))
                CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);

            var activeGlyphs = new ActiveGlyphs();
            foreach (var glyphId in GetGlyphs(spec.OrderIndex))
            {
                var bindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);
                foreach (var bindableSpell in bindableSpells)
                    if (HasSpell(bindableSpell) && !m_overrideSpells.ContainsKey(bindableSpell))
                        activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
            }

            activeGlyphs.IsFullUpdate = true;
            SendPacket(activeGlyphs);

            Item item = GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (item != null)
            {
                var azeriteItem = item.ToAzeriteItem();
                if (azeriteItem != null)
                {
                    if (azeriteItem.IsEquipped())
                    {
                        ApplyAllAzeriteEmpoweredItemMods(false);
                        ApplyAzeritePowers(azeriteItem, false);
                    }

                    azeriteItem.SetSelectedAzeriteEssences(spec.Id);

                    if (azeriteItem.IsEquipped())
                    {
                        ApplyAzeritePowers(azeriteItem, true);
                        ApplyAllAzeriteEmpoweredItemMods(true);
                    }

                    azeriteItem.SetState(ItemUpdateState.Changed, this);
                }
            }

            var shapeshiftAuras = GetAuraEffectsByType(AuraType.ModShapeshift);
            foreach (var aurEff in shapeshiftAuras)
            {
                aurEff.HandleShapeshiftBoosts(this, false);
                aurEff.HandleShapeshiftBoosts(this, true);
            }
        }

        public Dictionary<uint, PlayerSpellState> GetTalentMap(uint spec) { return _specializationInfo.Talents[spec]; }
        public List<uint> GetGlyphs(byte spec) { return _specializationInfo.Glyphs[spec]; }

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
                var months = (ulong)(GameTime.GetGameTime() - GetTalentResetTime()) / Time.Month;
                if (months > 0)
                {
                    // This cost will be reduced by a rate of 5 gold per month
                    var new_cost = (uint)(GetTalentResetCost() - 5 * MoneyConstants.Gold * months);
                    // to a minimum of 10 gold.
                    return new_cost < 10 * MoneyConstants.Gold ? 10 * MoneyConstants.Gold : new_cost;
                }
                else
                {
                    // After that it increases in increments of 5 gold
                    var new_cost = GetTalentResetCost() + 5 * MoneyConstants.Gold;
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
                // unlearn only talents for character class
                // some spell learned by one class as normal spells or know at creation but another class learn it as talent,
                // to prevent unexpected lost normal learned spell skip another class talents
                if (talentInfo.ClassID != (uint)GetClass())
                    continue;

                // skip non-existant talent ranks
                if (talentInfo.SpellID == 0)
                    continue;

                RemoveTalent(talentInfo);
            }            

            var trans = new SQLTransaction();
            _SaveTalents(trans);
            _SaveSpells(trans);
            DB.Characters.CommitTransaction(trans);

            if (!noCost)
            {
                ModifyMoney(-cost);
                UpdateCriteria(CriteriaTypes.GoldSpentForTalents, cost);
                UpdateCriteria(CriteriaTypes.NumberOfTalentResets, 1);

                SetTalentResetCost(cost);
                SetTalentResetTime(Time.UnixTime);
            }

            return true;
        }

        public void SendTalentsInfoData()
        {
            var packet = new UpdateTalentData();
            packet.Info.PrimarySpecialization = GetPrimarySpecialization();

            for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                var spec = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), i);
                if (spec == null)
                    continue;

                var talents = GetTalentMap(i);
                var pvpTalents = GetPvpTalentMap(i);

                var groupInfoPkt = new UpdateTalentData.TalentGroupInfo();
                groupInfoPkt.SpecID = spec.Id;

                foreach (var pair in talents)
                {
                    if (pair.Value == PlayerSpellState.Removed)
                        continue;

                    var talentInfo = CliDB.TalentStorage.LookupByKey(pair.Key);
                    if (talentInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Player {0} has unknown talent id: {1}", GetName(), pair.Key);
                        continue;
                    }

                    var spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                    if (spellEntry == null)
                    {
                        Log.outError(LogFilter.Player, "Player {0} has unknown talent spell: {1}", GetName(), talentInfo.SpellID);
                        continue;
                    }

                    groupInfoPkt.TalentIDs.Add((ushort)pair.Key);
                }

                for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                {
                    if (pvpTalents[slot] == 0)
                        continue;

                    var talentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalents[slot]);
                    if (talentInfo == null)
                    {
                        Log.outError(LogFilter.Player, $"Player.SendTalentsInfoData: Player '{GetName()}' ({GetGUID()}) has unknown pvp talent id: {pvpTalents[slot]}");
                        continue;
                    }

                    var spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                    if (spellEntry == null)
                    {
                        Log.outError(LogFilter.Player, $"Player.SendTalentsInfoData: Player '{GetName()}' ({GetGUID()}) has unknown pvp talent spell: {talentInfo.SpellID}");
                        continue;
                    }

                    var pvpTalent = new PvPTalent();
                    pvpTalent.PvPTalentID = (ushort)pvpTalents[slot];
                    pvpTalent.Slot = slot;
                    groupInfoPkt.PvPTalents.Add(pvpTalent);
                }

                if (i == GetActiveTalentGroup())
                    packet.Info.ActiveGroup = (byte)packet.Info.TalentGroups.Count;

                if (!groupInfoPkt.TalentIDs.Empty() || !groupInfoPkt.PvPTalents.Empty() || i == GetActiveTalentGroup())
                    packet.Info.TalentGroups.Add(groupInfoPkt);
            }

            SendPacket(packet);
        }

        public void SendRespecWipeConfirm(ObjectGuid guid, uint cost)
        {
            var respecWipeConfirm = new RespecWipeConfirm();
            respecWipeConfirm.RespecMaster = guid;
            respecWipeConfirm.Cost = cost;
            respecWipeConfirm.RespecType = SpecResetType.Talents;
            SendPacket(respecWipeConfirm);
        }
    }
}
