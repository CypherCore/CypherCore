// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Player
    {
        public void InitTalentForLevel()
        {
            uint level = GetLevel();
            // talents base at level diff (talents = level - 9 but some can be used already)
            if (level < PlayerConst.MinSpecializationLevel)
                ResetTalentSpecialization();

            uint talentTiers = Global.DB2Mgr.GetNumTalentsAtLevel(level, GetClass());
            if (level < 15)
            {
                // Remove all talent points
                ResetTalents(true);
            }
            else
            {
                if (!GetSession().HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
                {
                    for (uint t = talentTiers; t < PlayerConst.MaxTalentTiers; ++t)
                        for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                            foreach (TalentRecord talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), t, c))
                                RemoveTalent(talent);
                }
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.MaxTalentTiers), talentTiers);

            if (!GetSession().HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
            {
                for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
                {
                    for (int slot = Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(level, GetClass()); slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                    {
                        var pvpTalent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(spec)[slot]);
                        if (pvpTalent != null)
                            RemovePvpTalent(pvpTalent, spec);
                    }
                }
            }

            if (!GetSession().PlayerLoading())
                SendTalentsInfoData();   // update at client
        }

        public bool AddTalent(TalentRecord talent, byte spec, bool learning)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
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

            if (GetTalentMap(spec).ContainsKey(talent.Id))
                GetTalentMap(spec)[talent.Id] = PlayerSpellState.Unchanged;
            else
                GetTalentMap(spec)[talent.Id] = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

            if (spec == GetActiveTalentGroup())
            {
                LearnSpell(talent.SpellID, true);
                if (talent.OverridesSpellID != 0)
                    AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);
            }

            if (learning)
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeTalent);

            return true;
        }

        public void RemoveTalent(TalentRecord talent)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell(talent.SpellID);

            // search for spells that the talent teaches and unlearn them
            foreach (var spellEffectInfo in spellInfo.GetEffects())
                if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                    RemoveSpell(spellEffectInfo.TriggerSpell);

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

            if (GetPrimarySpecialization() == ChrSpecialization.None)
                return TalentLearnResult.FailedNoPrimaryTreeSelected;

            TalentRecord talentInfo = CliDB.TalentStorage.LookupByKey(talentId);
            if (talentInfo == null)
                return TalentLearnResult.FailedUnknown;

            if (talentInfo.SpecID != 0 && (ChrSpecialization)talentInfo.SpecID != GetPrimarySpecialization())
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
            foreach (TalentRecord talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), talentInfo.TierID, talentInfo.ColumnIndex))
            {
                if (talent.SpecID == 0)
                    bestSlotMatch = talent;

                else if ((ChrSpecialization)talent.SpecID == GetPrimarySpecialization())
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
                foreach (TalentRecord talent in Global.DB2Mgr.GetTalentsByPosition(GetClass(), talentInfo.TierID, c))
                {
                    if (talent.SpecID != 0 && (ChrSpecialization)talent.SpecID != GetPrimarySpecialization())
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
            uint spellid = talentInfo.SpellID;
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

            Log.outDebug(LogFilter.Misc, "Player.LearnTalent: TalentID: {0} Spell: {1} Group: {2}", talentId, spellid, GetActiveTalentGroup());

            return TalentLearnResult.LearnOk;
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
                            RemoveTalent(talent);
                    }
                }
            }

            ResetPvpTalents();
            RemoveSpecializationSpells();

            ChrSpecializationRecord defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(GetClass());
            SetPrimarySpecialization(defaultSpec.Id);
            SetActiveTalentGroup(defaultSpec.OrderIndex);

            LearnSpecializationSpells();

            SendTalentsInfoData();
            Item.UpdateItemSetAuras(this, false);
        }

        bool HasTalent(uint talentId, byte group)
        {
            return GetTalentMap(group).ContainsKey(talentId) && GetTalentMap(group)[talentId] != PlayerSpellState.Removed;
        }

        uint GetTalentResetCost() { return _specializationInfo.ResetTalentsCost; }

        void SetTalentResetCost(uint cost) { _specializationInfo.ResetTalentsCost = cost; }

        long GetTalentResetTime() { return _specializationInfo.ResetTalentsTime; }

        void SetTalentResetTime(long time_) { _specializationInfo.ResetTalentsTime = time_; }

        public ChrSpecialization GetPrimarySpecialization() { return (ChrSpecialization)m_playerData.CurrentSpecID.GetValue(); }

        void SetPrimarySpecialization(uint spec) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.CurrentSpecID), spec); }

        public ChrSpecializationRecord GetPrimarySpecializationEntry()
        {
            return CliDB.ChrSpecializationStorage.LookupByKey((uint)GetPrimarySpecialization());
        }

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
            if (pet != null)
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

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell(talentInfo.SpellID);

                // search for spells that the talent teaches and unlearn them
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                        RemoveSpell(spellEffectInfo.TriggerSpell);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
            }

            foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                if (spellInfo == null)
                    continue;

                RemoveSpell(talentInfo.SpellID);

                // search for spells that the talent teaches and unlearn them
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
                        RemoveSpell(spellEffectInfo.TriggerSpell);

                if (talentInfo.OverridesSpellID != 0)
                    RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
            }

            ApplyTraitConfig((int)(uint)m_activePlayerData.ActiveCombatTraitConfigID, false);

            // Remove spec specific spells
            RemoveSpecializationSpells();

            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
                RemoveAurasDueToSpell(CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID);

            SetActiveTalentGroup(spec.OrderIndex);
            SetPrimarySpecialization(spec.Id);
            int specTraitConfigIndex = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat
                    && traitConfig.ChrSpecializationID == spec.Id
                    && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != TraitCombatConfigFlags.None;
            });
            if (specTraitConfigIndex >= 0)
                SetActiveCombatTraitConfigID(m_activePlayerData.TraitConfigs[specTraitConfigIndex].ID);
            else
                SetActiveCombatTraitConfigID(0);

            foreach (var talentInfo in CliDB.TalentStorage.Values)
            {
                // learn only talents for character class
                if (talentInfo.ClassID != (int)GetClass())
                    continue;

                if (talentInfo.SpellID == 0)
                    continue;

                if (HasTalent(talentInfo.Id, GetActiveTalentGroup()))
                {
                    LearnSpell(talentInfo.SpellID, true);      // add the talent to the PlayerSpellMap
                    if (talentInfo.OverridesSpellID != 0)
                        AddOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
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

            ApplyTraitConfig((int)(uint)m_activePlayerData.ActiveCombatTraitConfigID, true);

            InitTalentForLevel();

            StartLoadingActionButtons();

            UpdateDisplayPower();
            PowerType pw = GetPowerType();
            if (pw != PowerType.Mana)
                SetPower(PowerType.Mana, 0); // Mana must be 0 even if it isn't the active power type.

            SetPower(pw, 0);
            Item.UpdateItemSetAuras(this, false);

            // update visible transmog
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                Item equippedItem = GetItemByPos(InventorySlots.Bag0, i);
                if (equippedItem != null)
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

            Item item = GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (item != null)
            {
                AzeriteItem azeriteItem = item.ToAzeriteItem();
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
            foreach (AuraEffect aurEff in shapeshiftAuras)
            {
                aurEff.HandleShapeshiftBoosts(this, false);
                aurEff.HandleShapeshiftBoosts(this, true);
            }
        }

        void StartLoadingActionButtons(Action callback = null)
        {
            uint traitConfigId = 0;

            TraitConfig traitConfig = GetTraitConfig((int)(uint)m_activePlayerData.ActiveCombatTraitConfigID);
            if (traitConfig != null)
            {
                int usedSavedTraitConfigIndex = m_activePlayerData.TraitConfigs.FindIndexIf(savedConfig =>
                {
                    return (TraitConfigType)(int)savedConfig.Type == TraitConfigType.Combat
                    && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None
                    && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.SharedActionBars) == TraitCombatConfigFlags.None
                    && savedConfig.LocalIdentifier == traitConfig.LocalIdentifier;
                });

                if (usedSavedTraitConfigIndex >= 0)
                    traitConfigId = (uint)(int)m_activePlayerData.TraitConfigs[usedSavedTraitConfigIndex].ID;
            }

            // load them asynchronously
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACTIONS_SPEC);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, GetActiveTalentGroup());
            stmt.AddValue(2, traitConfigId);

            var myGuid = GetGUID();

            WorldSession mySess = GetSession();
            mySess.GetQueryProcessor().AddCallback(DB.Characters.AsyncQuery(stmt)
                .WithCallback(result =>
                {
                    // safe callback, we can't pass this pointer directly
                    // in case player logs out before db response (player would be deleted in that case)
                    Player thisPlayer = mySess.GetPlayer();
                    if (thisPlayer != null && thisPlayer.GetGUID() == myGuid)
                        thisPlayer.LoadActions(result);

                    if (callback != null)
                        callback();
                }));
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

            SQLTransaction trans = new();
            _SaveTalents(trans);
            _SaveSpells(trans);
            DB.Characters.CommitTransaction(trans);

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

        public void SendTalentsInfoData()
        {
            UpdateTalentData packet = new();
            packet.Info.PrimarySpecialization = (uint)GetPrimarySpecialization();

            for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                ChrSpecializationRecord spec = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), i);
                if (spec == null)
                    continue;

                var talents = GetTalentMap(i);
                var pvpTalents = GetPvpTalentMap(i);

                UpdateTalentData.TalentGroupInfo groupInfoPkt = new();
                groupInfoPkt.SpecID = spec.Id;

                foreach (var pair in talents)
                {
                    if (pair.Value == PlayerSpellState.Removed)
                        continue;

                    TalentRecord talentInfo = CliDB.TalentStorage.LookupByKey(pair.Key);
                    if (talentInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Player {0} has unknown talent id: {1}", GetName(), pair.Key);
                        continue;
                    }

                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
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

                    PvpTalentRecord talentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalents[slot]);
                    if (talentInfo == null)
                    {
                        Log.outError(LogFilter.Player, $"Player.SendTalentsInfoData: Player '{GetName()}' ({GetGUID()}) has unknown pvp talent id: {pvpTalents[slot]}");
                        continue;
                    }

                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                    if (spellEntry == null)
                    {
                        Log.outError(LogFilter.Player, $"Player.SendTalentsInfoData: Player '{GetName()}' ({GetGUID()}) has unknown pvp talent spell: {talentInfo.SpellID}");
                        continue;
                    }

                    PvPTalent pvpTalent = new();
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

        public void SendRespecWipeConfirm(ObjectGuid guid, uint cost, SpecResetType respecType)
        {
            switch (respecType)
            {
                case SpecResetType.Talents:
                    PlayerTalkClass.GetInteractionData().StartInteraction(guid, PlayerInteractionType.TalentMaster);
                    break;
                case SpecResetType.Specialization:
                    PlayerTalkClass.GetInteractionData().StartInteraction(guid, PlayerInteractionType.SpecializationMaster);
                    break;
                default:
                    break;
            }

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

            if ((ChrSpecialization)talentInfo.SpecID != GetPrimarySpecialization())
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

            if (!ConditionManager.IsPlayerMeetingCondition(this, (uint)talentInfo.PlayerConditionID))
                return TalentLearnResult.FailedCantDoThatRightNow;

            PvpTalentRecord talent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);
            if (talent != null)
            {
                if (!HasPlayerFlag(PlayerFlags.Resting) && !HasUnitFlag2(UnitFlags2.AllowChangingTalents))
                    return TalentLearnResult.FailedRestArea;

                if (GetSpellHistory().HasCooldown(talent.SpellID))
                {
                    spellOnCooldown = talent.SpellID;
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
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
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
                LearnSpell(talent.SpellID, true);

                // Move this to toggle ?
                if (talent.OverridesSpellID != 0)
                    AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);
            }

            GetPvpTalentMap(activeTalentGroup)[slot] = talent.Id;

            return true;
        }

        void RemovePvpTalent(PvpTalentRecord talent, byte activeTalentGroup)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            RemoveSpell(talent.SpellID);

            // Move this to toggle ?
            if (talent.OverridesSpellID != 0)
                RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

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
                        LearnSpell(pvpTalentInfo.SpellID, true);
                        if (pvpTalentInfo.OverridesSpellID != 0)
                            AddOverrideSpell(pvpTalentInfo.OverridesSpellID, pvpTalentInfo.SpellID);
                    }
                    else
                    {
                        if (pvpTalentInfo.OverridesSpellID != 0)
                            RemoveOverrideSpell(pvpTalentInfo.OverridesSpellID, pvpTalentInfo.SpellID);
                        RemoveSpell(pvpTalentInfo.SpellID);
                    }
                }
            }
        }

        bool HasPvpTalent(uint talentID, byte activeTalentGroup)
        {
            return GetPvpTalentMap(activeTalentGroup).Contains(talentID);
        }

        //Traits
        public void CreateTraitConfig(TraitConfigPacket traitConfig)
        {
            int configId = TraitMgr.GenerateNewTraitConfigId();
            bool hasConfigId(int id)
            {
                return m_activePlayerData.TraitConfigs.FindIndexIf(config => config.ID == id) >= 0;
            }

            while (hasConfigId(configId))
                configId = TraitMgr.GenerateNewTraitConfigId();

            traitConfig.ID = configId;

            int traitConfigIndex = m_activePlayerData.TraitConfigs.Size();
            AddTraitConfig(traitConfig);

            foreach (TraitEntry grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
            {
                var entryIndex = traitConfig.Entries.Find(entry => entry.TraitNodeID == grantedEntry.TraitNodeID && entry.TraitNodeEntryID == grantedEntry.TraitNodeEntryID);
                if (entryIndex == null)
                {
                    TraitConfig value = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, traitConfigIndex);
                    AddDynamicUpdateFieldValue(value.ModifyValue(value.Entries), grantedEntry);
                }
            }

            m_traitConfigStates[(int)configId] = PlayerSpellState.Changed;
        }

        void AddTraitConfig(TraitConfigPacket traitConfig)
        {
            var setter = new TraitConfig();
            setter.ModifyValue(setter.ID).SetValue(traitConfig.ID);
            setter.ModifyValue(setter.Name).SetValue(traitConfig.Name);
            setter.ModifyValue(setter.Type).SetValue((int)traitConfig.Type);
            setter.ModifyValue(setter.SkillLineID).SetValue((int)traitConfig.SkillLineID);
            setter.ModifyValue(setter.ChrSpecializationID).SetValue(traitConfig.ChrSpecializationID);
            setter.ModifyValue(setter.CombatConfigFlags).SetValue((int)traitConfig.CombatConfigFlags);
            setter.ModifyValue(setter.LocalIdentifier).SetValue(traitConfig.LocalIdentifier);
            setter.ModifyValue(setter.TraitSystemID).SetValue(traitConfig.TraitSystemID);

            AddDynamicUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs), setter);

            foreach (TraitEntryPacket traitEntry in traitConfig.Entries)
            {
                TraitEntry newEntry = new();
                newEntry.TraitNodeID = traitEntry.TraitNodeID;
                newEntry.TraitNodeEntryID = traitEntry.TraitNodeEntryID;
                newEntry.Rank = traitEntry.Rank;
                newEntry.GrantedRanks = traitEntry.GrantedRanks;
                AddDynamicUpdateFieldValue(setter.ModifyValue(setter.Entries), newEntry);
            }

            foreach (var traitSubTree in traitConfig.SubTrees)
            {
                TraitSubTreeCache newSubTree = new();
                newSubTree.TraitSubTreeID = traitSubTree.TraitSubTreeID;
                newSubTree.Active = traitSubTree.Active ? 1 : 0u;

                foreach (var traitEntry in traitSubTree.Entries)
                {
                    TraitEntry newEntry = new();
                    newEntry.TraitNodeID = traitEntry.TraitNodeID;
                    newEntry.TraitNodeEntryID = traitEntry.TraitNodeEntryID;
                    newEntry.Rank = traitEntry.Rank;
                    newEntry.GrantedRanks = traitEntry.GrantedRanks;
                    newSubTree.Entries.Add(newEntry);
                }

                AddDynamicUpdateFieldValue(setter.ModifyValue(setter.SubTrees), newSubTree);
            }
        }

        public TraitConfig GetTraitConfig(int configId)
        {
            int index = m_activePlayerData.TraitConfigs.FindIndexIf(config => config.ID == configId);
            if (index < 0)
                return null;

            return m_activePlayerData.TraitConfigs[index];
        }

        public void UpdateTraitConfig(TraitConfigPacket newConfig, int savedConfigId, bool withCastTime)
        {
            int index = m_activePlayerData.TraitConfigs.FindIndexIf(config => config.ID == newConfig.ID);
            if (index < 0)
                return;

            if (withCastTime)
            {
                CastSpell(this, TraitMgr.COMMIT_COMBAT_TRAIT_CONFIG_CHANGES_SPELL_ID, new CastSpellExtraArgs(SpellValueMod.BasePoint0, savedConfigId).SetCustomArg(newConfig));
                return;
            }

            bool isActiveConfig = true;
            bool loadActionButtons = false;
            switch ((TraitConfigType)(int)m_activePlayerData.TraitConfigs[index].Type)
            {
                case TraitConfigType.Combat:
                    isActiveConfig = newConfig.ID == m_activePlayerData.ActiveCombatTraitConfigID;
                    loadActionButtons = m_activePlayerData.TraitConfigs[index].LocalIdentifier != newConfig.LocalIdentifier;
                    break;
                case TraitConfigType.Profession:
                    isActiveConfig = HasSkill((uint)(int)m_activePlayerData.TraitConfigs[index].SkillLineID);
                    break;
                default:
                    break;
            }

            Action finalizeTraitConfigUpdate = () =>
            {
                TraitConfig newTraitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, index);
                SetUpdateFieldValue(newTraitConfig.ModifyValue(newTraitConfig.LocalIdentifier), newConfig.LocalIdentifier);

                ApplyTraitEntryChanges(newConfig.ID, newConfig, isActiveConfig, true);

                if (savedConfigId != 0)
                    ApplyTraitEntryChanges(savedConfigId, newConfig, false, false);

                if (((TraitCombatConfigFlags)(int)newConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.StarterBuild))
                    SetTraitConfigUseStarterBuild(newConfig.ID, true);
            };

            if (loadActionButtons)
            {
                SQLTransaction trans = new SQLTransaction();
                _SaveActions(trans);
                DB.Characters.CommitTransaction(trans);

                StartLoadingActionButtons(finalizeTraitConfigUpdate);
            }
            else
                finalizeTraitConfigUpdate();
        }

        void ApplyTraitEntryChanges(int editedConfigId, TraitConfigPacket newConfig, bool applyTraits, bool consumeCurrencies)
        {
            int editedIndex = m_activePlayerData.TraitConfigs.FindIndexIf(config => config.ID == editedConfigId);
            if (editedIndex < 0)
                return;

            TraitConfig editedConfig = m_activePlayerData.TraitConfigs[editedIndex];

            // remove traits not found in new config
            SortedSet<int> entryIndicesToRemove = new(Comparer<int>.Create((a, b) => -a.CompareTo(b)));
            for (int i = 0; i < editedConfig.Entries.Size(); ++i)
            {
                TraitEntry oldEntry = editedConfig.Entries[i];
                var entryItr = newConfig.Entries.Find(ufEntry => ufEntry.TraitNodeID == oldEntry.TraitNodeID && ufEntry.TraitNodeEntryID == oldEntry.TraitNodeEntryID);
                if (entryItr != null)
                    continue;

                if (applyTraits)
                    ApplyTraitEntry(oldEntry.TraitNodeEntryID, 0, 0, false);

                entryIndicesToRemove.Add(i);
            }

            foreach (int indexToRemove in entryIndicesToRemove)
            {
                TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
                RemoveDynamicUpdateFieldValue(traitConfig.ModifyValue(traitConfig.Entries), indexToRemove);
            }

            List<TraitEntryPacket> costEntries = new();

            // apply new traits
            foreach (TraitEntryPacket newEntry in newConfig.Entries)
            {
                int oldEntryIndex = editedConfig.Entries.FindIndexIf(ufEntry => ufEntry.TraitNodeID == newEntry.TraitNodeID && ufEntry.TraitNodeEntryID == newEntry.TraitNodeEntryID);
                if (oldEntryIndex < 0)
                {
                    if (consumeCurrencies)
                        costEntries.Add(newEntry);

                    TraitConfig newTraitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
                    TraitEntry newUfEntry = new();
                    newUfEntry.TraitNodeID = newEntry.TraitNodeID;
                    newUfEntry.TraitNodeEntryID = newEntry.TraitNodeEntryID;
                    newUfEntry.Rank = newEntry.Rank;
                    newUfEntry.GrantedRanks = newEntry.GrantedRanks;

                    AddDynamicUpdateFieldValue(newTraitConfig.ModifyValue(newTraitConfig.Entries), newUfEntry);

                    if (applyTraits)
                    {
                        ApplyTraitEntry(newEntry.TraitNodeEntryID, 0, 0, false);
                        ApplyTraitEntry(newUfEntry.TraitNodeEntryID, newUfEntry.Rank, 0, true);
                    }
                }
                else if (newEntry.Rank != editedConfig.Entries[oldEntryIndex].Rank || newEntry.GrantedRanks != editedConfig.Entries[oldEntryIndex].GrantedRanks)
                {
                    if (consumeCurrencies && newEntry.Rank > editedConfig.Entries[oldEntryIndex].Rank)
                    {
                        TraitEntryPacket costEntry = new();
                        costEntry.Rank -= editedConfig.Entries[oldEntryIndex].Rank;
                        costEntries.Add(newEntry);
                    }

                    TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
                    TraitEntry traitEntry = traitConfig.ModifyValue(traitConfig.Entries, oldEntryIndex);
                    traitEntry.Rank = newEntry.Rank;
                    traitEntry.GrantedRanks = newEntry.GrantedRanks;
                    SetUpdateFieldValue(traitConfig.Entries, oldEntryIndex, traitEntry);

                    if (applyTraits)
                        ApplyTraitEntry(newEntry.TraitNodeEntryID, newEntry.Rank, newEntry.GrantedRanks, true);
                }
            }

            if (consumeCurrencies)
            {
                Dictionary<int, int> currencies = new();
                TraitMgr.FillSpentCurrenciesMap(costEntries, currencies);

                foreach (var (traitCurrencyId, amount) in currencies)
                {
                    TraitCurrencyRecord traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(traitCurrencyId);
                    if (traitCurrency == null)
                        continue;

                    switch (traitCurrency.GetCurrencyType())
                    {
                        case TraitCurrencyType.Gold:
                            ModifyMoney(-amount);
                            break;
                        case TraitCurrencyType.CurrencyTypesBased:
                            RemoveCurrency((uint)traitCurrency.CurrencyTypesID, amount /* TODO: CurrencyDestroyReason */);
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (TraitSubTreeCachePacket newSubTree in newConfig.SubTrees)
            {
                int oldSubTreeIndex = editedConfig.SubTrees.FindIndexIf(ufSubTree => ufSubTree.TraitSubTreeID == newSubTree.TraitSubTreeID);

                List<TraitEntry> subTreeEntries = new();
                for (int j = 0; j < newSubTree.Entries.Count; ++j)
                {
                    TraitEntry newUfEntry = subTreeEntries[j];
                    newUfEntry.TraitNodeID = newSubTree.Entries[j].TraitNodeID;
                    newUfEntry.TraitNodeEntryID = newSubTree.Entries[j].TraitNodeEntryID;
                    newUfEntry.Rank = newSubTree.Entries[j].Rank;
                    newUfEntry.GrantedRanks = newSubTree.Entries[j].GrantedRanks;
                }

                if (oldSubTreeIndex < 0)
                {
                    TraitSubTreeCache newUfSubTree = new();
                    newUfSubTree.TraitSubTreeID = newSubTree.TraitSubTreeID;
                    newUfSubTree.Active = newSubTree.Active ? 1 : 0u;
                    newUfSubTree.Entries = subTreeEntries;

                    TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
                    AddDynamicUpdateFieldValue(traitConfig.ModifyValue(traitConfig.SubTrees), newUfSubTree);
                }
                else
                {
                    bool wasActive = m_activePlayerData.TraitConfigs[editedIndex].SubTrees[oldSubTreeIndex].Active != 0;

                    TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
                    TraitSubTreeCache traitSubTreeCache = traitConfig.ModifyValue(traitConfig.SubTrees, oldSubTreeIndex);

                    traitSubTreeCache.Active = newSubTree.Active ? 1 : 0u;
                    traitSubTreeCache.Entries = subTreeEntries;
                    SetUpdateFieldValue(traitConfig.ModifyValue(traitConfig.SubTrees, oldSubTreeIndex), traitSubTreeCache);


                    if (applyTraits && wasActive != newSubTree.Active)
                        foreach (var subTreeEntry in newSubTree.Entries)
                            ApplyTraitEntry(subTreeEntry.TraitNodeEntryID, subTreeEntry.Rank, subTreeEntry.GrantedRanks, newSubTree.Active);
                }
            }

            m_traitConfigStates[editedConfigId] = PlayerSpellState.Changed;
        }

        public void RenameTraitConfig(int editedConfigId, string newName)
        {
            int editedIndex = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return traitConfig.ID == editedConfigId
                    && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat
                    && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None;
            });
            if (editedIndex < 0)
                return;

            TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, editedIndex);
            SetUpdateFieldValue(traitConfig.ModifyValue(traitConfig.Name), newName);

            m_traitConfigStates[editedConfigId] = PlayerSpellState.Changed;
        }

        public void DeleteTraitConfig(int deletedConfigId)
        {
            int deletedIndex = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return traitConfig.ID == deletedConfigId
                    && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat
                    && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None;
            });
            if (deletedIndex < 0)
                return;

            RemoveDynamicUpdateFieldValue(m_values.ModifyValue(m_activePlayerData)
                .ModifyValue(m_activePlayerData.TraitConfigs), deletedIndex);

            m_traitConfigStates[deletedConfigId] = PlayerSpellState.Removed;
        }

        void ApplyTraitConfig(int configId, bool apply)
        {
            TraitConfig traitConfig = GetTraitConfig(configId);
            if (traitConfig == null)
                return;

            foreach (TraitEntry traitEntry in traitConfig.Entries)
                if (!apply || TraitMgr.CanApplyTraitNode(traitConfig, traitEntry))
                    ApplyTraitEntry(traitEntry.TraitNodeEntryID, traitEntry.Rank, traitEntry.GrantedRanks, apply);
        }

        void ApplyTraitEntry(int traitNodeEntryId, int rank, int grantedRanks, bool apply)
        {
            TraitNodeEntryRecord traitNodeEntry = CliDB.TraitNodeEntryStorage.LookupByKey(traitNodeEntryId);
            if (traitNodeEntry == null)
                return;

            TraitDefinitionRecord traitDefinition = CliDB.TraitDefinitionStorage.LookupByKey(traitNodeEntry.TraitDefinitionID);
            if (traitDefinition == null)
                return;

            if (traitDefinition.SpellID != 0)
            {
                Cypher.Assert(traitNodeEntry.TraitDefinitionID <= 0xFFFFFF && rank + grantedRanks <= 0xFF);

                if (apply)
                    LearnSpell(traitDefinition.SpellID, true, 0, false, new PlayerSpellTrait(traitNodeEntry.TraitDefinitionID, rank + grantedRanks));
                else
                    RemoveSpell(traitDefinition.SpellID);
            }
        }

        public PlayerSpellTrait GetTraitInfoForSpell(uint spellId)
        {
            PlayerSpell spell = m_spells.LookupByKey(spellId);
            if (spell != null)
                return spell.Trait;

            return null;
        }

        public void SetTraitConfigUseStarterBuild(int traitConfigId, bool useStarterBuild)
        {
            int configIndex = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return traitConfig.ID == traitConfigId
                    && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat
                    && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != TraitCombatConfigFlags.None;
            });
            if (configIndex < 0)
                return;

            bool currentlyUsesStarterBuild = ((TraitCombatConfigFlags)(int)m_activePlayerData.TraitConfigs[configIndex].CombatConfigFlags).HasFlag(TraitCombatConfigFlags.StarterBuild);
            if (currentlyUsesStarterBuild == useStarterBuild)
                return;

            if (useStarterBuild)
            {
                TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, configIndex);
                SetUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.StarterBuild);
            }
            else
            {
                TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, configIndex);
                RemoveUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.StarterBuild);
            }

            m_traitConfigStates[(int)traitConfigId] = PlayerSpellState.Changed;
        }

        public void SetTraitConfigUseSharedActionBars(int traitConfigId, bool usesSharedActionBars, bool isLastSelectedSavedConfig)
        {
            int configIndex = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return traitConfig.ID == traitConfigId
                    && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat
                    && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None;
            });
            if (configIndex < 0)
                return;

            bool currentlyUsesSharedActionBars = ((TraitCombatConfigFlags)(int)m_activePlayerData.TraitConfigs[configIndex].CombatConfigFlags).HasFlag(TraitCombatConfigFlags.SharedActionBars);
            if (currentlyUsesSharedActionBars == usesSharedActionBars)
                return;

            TraitConfig traitConfig = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TraitConfigs, configIndex);
            if (usesSharedActionBars)
            {
                SetUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.SharedActionBars);

                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_TRAIT_CONFIG);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, traitConfigId);
                DB.Characters.Execute(stmt);

                if (isLastSelectedSavedConfig)
                    StartLoadingActionButtons(); // load action buttons that were saved in shared mode
            }
            else
            {
                RemoveUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.SharedActionBars);

                // trigger a save with traitConfigId
                foreach (var (_, button) in m_actionButtons)
                    if (button.uState != ActionButtonUpdateState.Deleted)
                        button.uState = ActionButtonUpdateState.New;
            }

            m_traitConfigStates[traitConfigId] = PlayerSpellState.Changed;
        }
    }
}
