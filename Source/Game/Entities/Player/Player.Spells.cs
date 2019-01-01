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
using Framework.Dynamic;
using Game.BattlePets;
using Game.DataStorage;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Player
    {
        void UpdateSkillsForLevel()
        {
            ushort maxSkill = GetMaxSkillValueForLevel();

            foreach (var pair in mSkillStatus)
            {
                if (pair.Value.State == SkillState.Deleted)
                    continue;

                uint pskill = pair.Key;
                SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(pskill, GetRace(), GetClass());
                if (rcEntry == null)
                    continue;

                ushort field = (ushort)(pair.Value.Pos / 2);
                byte offset = (byte)(pair.Value.Pos & 1);

                if (Global.SpellMgr.GetSkillRangeType(rcEntry) == SkillRangeType.Level)
                {
                    ushort max = GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);

                    // update only level dependent max skill values
                    if (max != 1)
                    {
                        SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, maxSkill);
                        SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, maxSkill);
                        if (pair.Value.State != SkillState.New)
                            pair.Value.State = SkillState.Changed;
                    }
                }

                // Update level dependent skillline spells
                LearnSkillRewardedSpells(rcEntry.SkillID, GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset));
            }
        }

        public void UpdateSkillsToMaxSkillsForLevel()
        {
            foreach (var skill in mSkillStatus)
            {
                if (skill.Value.State == SkillState.Deleted)
                    continue;

                uint pskill = skill.Key;
                SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(pskill, GetRace(), GetClass());
                if (rcEntry == null)
                    continue;

                if (Global.SpellMgr.IsProfessionOrRidingSkill(rcEntry.SkillID))
                    continue;

                if (Global.SpellMgr.IsWeaponSkill(rcEntry.SkillID))
                    continue;

                ushort field = (ushort)(skill.Value.Pos / 2);
                byte offset = (byte)(skill.Value.Pos & 1);

                ushort max = GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);
                if (max > 1)
                {
                    SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, max);

                    if (skill.Value.State != SkillState.New)
                        skill.Value.State = SkillState.Changed;
                }
            }
        }

        public ushort GetSkillValue(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            int result = GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
            result += GetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset);
            result += GetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset);
            return (ushort)(result < 0 ? 0 : result);
        }

        ushort GetMaxSkillValue(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            int result = GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);
            result += GetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset);
            result += GetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset);
            return (ushort)(result < 0 ? 0 : result);
        }

        public ushort GetPureSkillValue(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey((uint)skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            return GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
        }

        public ushort GetSkillStep(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            return GetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset);
        }

        public ushort GetPureMaxSkillValue(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            return GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);
        }

        public ushort GetBaseSkillValue(SkillType skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            var result = (int)GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
            result += GetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset);
            return (ushort)(result < 0 ? 0 : result);
        }

        public ushort GetSkillPermBonusValue(uint skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            return GetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset);
        }

        public ushort GetSkillTempBonusValue(uint skill)
        {
            if (skill == 0)
                return 0;

            var skillStatusData = mSkillStatus.LookupByKey(skill);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return 0;

            var field = (ushort)(skillStatusData.Pos / 2);
            var offset = (byte)(skillStatusData.Pos & 1);

            return GetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset);
        }

        void InitializeSelfResurrectionSpells()
        {
            ClearDynamicValue(ActivePlayerDynamicFields.SelfResSpells);

            uint[] spells = new uint[3];

            var dummyAuras = GetAuraEffectsByType(AuraType.Dummy);
            foreach (var auraEffect in dummyAuras)
            {
                // Soulstone Resurrection                           // prio: 3 (max, non death persistent)
                if (auraEffect.GetSpellInfo().SpellFamilyName == SpellFamilyNames.Warlock && auraEffect.GetSpellInfo().SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
                    spells[0] = 3026;
                // Twisting Nether                                  // prio: 2 (max)
                else if (auraEffect.GetId() == 23701 && RandomHelper.randChance(10))
                    spells[1] = 23700;
            }

            // Reincarnation (passive spell)  // prio: 1
            if (HasSpell(20608) && !GetSpellHistory().HasCooldown(21169))
                spells[2] = 21169;

            foreach (uint selfResSpell in spells)
                if (selfResSpell != 0)
                    AddDynamicValue(ActivePlayerDynamicFields.SelfResSpells, selfResSpell);
        }

        public void PetSpellInitialize()
        {
            Pet pet = GetPet();

            if (!pet)
                return;

            Log.outDebug(LogFilter.Pet, "Pet Spells Groups");

            CharmInfo charmInfo = pet.GetCharmInfo();

            PetSpells petSpellsPacket = new PetSpells();
            petSpellsPacket.PetGUID = pet.GetGUID();
            petSpellsPacket.CreatureFamily = (ushort)pet.GetCreatureTemplate().Family;         // creature family (required for pet talents)
            petSpellsPacket.Specialization = pet.GetSpecialization();
            petSpellsPacket.TimeLimit = (uint)pet.GetDuration();
            petSpellsPacket.ReactState = pet.GetReactState();
            petSpellsPacket.CommandState = charmInfo.GetCommandState();

            // action bar loop
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
                petSpellsPacket.ActionButtons[i] = charmInfo.GetActionBarEntry(i).packedData;

            if (pet.IsPermanentPetFor(this))
            {
                // spells loop
                foreach (var pair in pet.m_spells)
                {
                    if (pair.Value.state == PetSpellState.Removed)
                        continue;

                    petSpellsPacket.Actions.Add(UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(pair.Key, (uint)pair.Value.active));
                }
            }

            // Cooldowns
            pet.GetSpellHistory().WritePacket(petSpellsPacket);

            SendPacket(petSpellsPacket);
        }

        public bool CanSeeSpellClickOn(Creature creature)
        {
            if (!creature.HasFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick))
                return false;

            var clickPair = Global.ObjectMgr.GetSpellClickInfoMapBounds(creature.GetEntry());
            if (clickPair.Empty())
                return false;

            foreach (var spellClickInfo in clickPair)
            {
                if (!spellClickInfo.IsFitToRequirements(this, creature))
                    return false;

                if (Global.ConditionMgr.IsObjectMeetingSpellClickConditions(creature.GetEntry(), spellClickInfo.spellId, this, creature))
                    return true;
            }

            return false;
        }

        public override SpellInfo GetCastSpellInfo(SpellInfo spellInfo)
        {
            var overrides = m_overrideSpells.LookupByKey(spellInfo.Id);
            if (!overrides.Empty())
            {
                foreach (uint spellId in overrides)
                {
                    SpellInfo newInfo = Global.SpellMgr.GetSpellInfo(spellId);
                    if (newInfo != null)
                        return GetCastSpellInfo(newInfo);
                }
            }

            return base.GetCastSpellInfo(spellInfo);
        }

        void AddOverrideSpell(uint overridenSpellId, uint newSpellId)
        {
            m_overrideSpells.Add(overridenSpellId, newSpellId);
        }

        void RemoveOverrideSpell(uint overridenSpellId, uint newSpellId)
        {
            m_overrideSpells.Remove(overridenSpellId, newSpellId);
        }

        void LearnSpecializationSpells()
        {
            var specSpells = Global.DB2Mgr.GetSpecializationSpells(GetUInt32Value(PlayerFields.CurrentSpecId));
            if (specSpells != null)
            {
                for (int j = 0; j < specSpells.Count; ++j)
                {
                    SpecializationSpellsRecord specSpell = specSpells[j];
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID);
                    if (spellInfo == null || spellInfo.SpellLevel > getLevel())
                        continue;

                    LearnSpell(specSpell.SpellID, false);
                    if (specSpell.OverridesSpellID != 0)
                        AddOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
                }
            }
        }

        void RemoveSpecializationSpells()
        {
            for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                ChrSpecializationRecord specialization = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), i);
                if (specialization != null)
                {
                    var specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization.Id);
                    if (specSpells != null)
                    {
                        for (int j = 0; j < specSpells.Count; ++j)
                        {
                            SpecializationSpellsRecord specSpell = specSpells[j];
                            RemoveSpell(specSpell.SpellID, true);
                            if (specSpell.OverridesSpellID != 0)
                                RemoveOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
                        }
                    }

                    for (uint j = 0; j < PlayerConst.MaxMasterySpells; ++j)
                    {
                        uint mastery = specialization.MasterySpellID[j];
                        if (mastery != 0)
                            RemoveAurasDueToSpell(mastery);
                    }
                }
            }
        }

        public void SendSpellCategoryCooldowns()
        {
            SpellCategoryCooldown cooldowns = new SpellCategoryCooldown();

            var categoryCooldownAuras = GetAuraEffectsByType(AuraType.ModSpellCategoryCooldown);
            foreach (AuraEffect aurEff in categoryCooldownAuras)
            {
                uint categoryId = (uint)aurEff.GetMiscValue();
                var cooldownInfo = cooldowns.CategoryCooldowns.Find(p => p.Category == categoryId);

                if (cooldownInfo == null)
                    cooldowns.CategoryCooldowns.Add(new SpellCategoryCooldown.CategoryCooldownInfo(categoryId, -aurEff.GetAmount()));
                else
                    cooldownInfo.ModCooldown -= aurEff.GetAmount();
            }

            SendPacket(cooldowns);
        }

        public bool UpdateSkillPro(SkillType skillId, int chance, uint step)
        {
            return UpdateSkillPro((uint)skillId, chance, step);
        }
        public bool UpdateSkillPro(uint skillId, int chance, uint step)
        {
            // levels sync. with spell requirement for skill levels to learn
            // bonus abilities in sSkillLineAbilityStore
            // Used only to avoid scan DBC at each skill grow
            uint[] bonusSkillLevels = { 75, 150, 225, 300, 375, 450, 525, 600, 700, 850 };

            Log.outDebug(LogFilter.Player, "UpdateSkillPro(SkillId {0}, Chance {0:D3}%)", skillId, chance / 10.0f);
            if (skillId == 0)
                return false;

            if (chance <= 0)                                         // speedup in 0 chance case
            {
                Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:D3}% missed", chance / 10.0f);
                return false;
            }

            var skillStatusData = mSkillStatus.LookupByKey(skillId);
            if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
                return false;

            ushort field = (ushort)(skillStatusData.Pos / 2);
            byte offset = (byte)(skillStatusData.Pos & 1);

            ushort value = GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
            ushort max = GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);

            if (max == 0 || value == 0 || value >= max)
                return false;

            if (RandomHelper.IRand(1, 1000) > chance)
            {
                Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:F3}% missed", chance / 10.0f);
                return false;
            }

            ushort new_value = (ushort)(value + step);
            if (new_value > max)
                new_value = max;

            SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, new_value);
            if (skillStatusData.State != SkillState.New)
                skillStatusData.State = SkillState.Changed;

            foreach (uint bsl in bonusSkillLevels)
            {
                if (value < bsl && new_value >= bsl)
                {
                    LearnSkillRewardedSpells(skillId, new_value);
                    break;
                }
            }

            UpdateSkillEnchantments(skillId, value, new_value);
            UpdateCriteria(CriteriaTypes.ReachSkillLevel, skillId);
            Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:F3}% taken", chance / 10.0f);
            return true;
        }
        void UpdateSkillEnchantments(uint skill_id, ushort curr_value, ushort new_value)
        {
            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                    {
                        uint ench_id = m_items[i].GetEnchantmentId(slot);
                        if (ench_id == 0)
                            continue;

                        SpellItemEnchantmentRecord Enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(ench_id);
                        if (Enchant == null)
                            return;

                        if (Enchant.RequiredSkillID == skill_id)
                        {
                            // Checks if the enchantment needs to be applied or removed
                            if (curr_value < Enchant.RequiredSkillRank && new_value >= Enchant.RequiredSkillRank)
                                ApplyEnchantment(m_items[i], slot, true);
                            else if (new_value < Enchant.RequiredSkillRank && curr_value >= Enchant.RequiredSkillRank)
                                ApplyEnchantment(m_items[i], slot, false);
                        }

                        // If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
                        // rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
                        if ((slot == EnchantmentSlot.Sock1 || slot == EnchantmentSlot.Sock2 || slot == EnchantmentSlot.Sock3)
                            && m_items[i].GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
                        {
                            SpellItemEnchantmentRecord pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(m_items[i].GetEnchantmentId(EnchantmentSlot.Prismatic));

                            if (pPrismaticEnchant != null && pPrismaticEnchant.RequiredSkillID == skill_id)
                            {
                                if (curr_value < pPrismaticEnchant.RequiredSkillRank && new_value >= pPrismaticEnchant.RequiredSkillRank)
                                    ApplyEnchantment(m_items[i], slot, true);
                                else if (new_value < pPrismaticEnchant.RequiredSkillRank && curr_value >= pPrismaticEnchant.RequiredSkillRank)
                                    ApplyEnchantment(m_items[i], slot, false);
                            }
                        }
                    }
                }
            }
        }

        void UpdateEnchantTime(uint time)
        {
            foreach (var enchat in m_enchantDuration)
            {
                if (enchat.item.GetEnchantmentId(enchat.slot) == 0)
                {
                    m_enchantDuration.Remove(enchat);
                }
                else if (enchat.leftduration <= time)
                {
                    ApplyEnchantment(enchat.item, enchat.slot, false, false);
                    enchat.item.ClearEnchantment(enchat.slot);
                    m_enchantDuration.Remove(enchat);
                }
                else if (enchat.leftduration > time)
                {
                    enchat.leftduration -= time;
                }
            }
        }

        void ApplyEnchantment(Item item, bool apply)
        {
            for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                ApplyEnchantment(item, slot, apply);
        }
        public void ApplyEnchantment(Item item, EnchantmentSlot slot, bool apply, bool apply_dur = true, bool ignore_condition = false)
        {
            if (item == null || !item.IsEquipped())
                return;

            if (slot >= EnchantmentSlot.Max)
                return;

            uint enchant_id = item.GetEnchantmentId(slot);
            if (enchant_id == 0)
                return;

            SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
            if (pEnchant == null)
                return;

            if (!ignore_condition && pEnchant.ConditionID != 0 && !EnchantmentFitsRequirements(pEnchant.ConditionID, -1))
                return;

            if (pEnchant.MinLevel > getLevel())
                return;

            if (pEnchant.RequiredSkillID > 0 && pEnchant.RequiredSkillRank > GetSkillValue((SkillType)pEnchant.RequiredSkillID))
                return;

            // If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
            // rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
            if ((slot == EnchantmentSlot.Sock1 || slot == EnchantmentSlot.Sock2 || slot == EnchantmentSlot.Sock3))
            {
                if (item.GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
                {
                    // Check if the requirements for the prismatic socket are met before applying the gem stats
                    SpellItemEnchantmentRecord pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId(EnchantmentSlot.Prismatic));
                    if (pPrismaticEnchant == null || (pPrismaticEnchant.RequiredSkillID > 0 && pPrismaticEnchant.RequiredSkillRank > GetSkillValue((SkillType)pPrismaticEnchant.RequiredSkillID)))
                        return;
                }

                // Cogwheel gems dont have requirement data set in SpellItemEnchantment.dbc, but they do have it in Item-sparse.db2
                ItemDynamicFieldGems gem = item.GetGem((ushort)(slot - EnchantmentSlot.Sock1));
                if (gem != null)
                {
                    ItemTemplate gemTemplate = Global.ObjectMgr.GetItemTemplate(gem.ItemId);
                    if (gemTemplate != null)
                        if (gemTemplate.GetRequiredSkill() != 0 && GetSkillValue((SkillType)gemTemplate.GetRequiredSkill()) < gemTemplate.GetRequiredSkillRank())
                            return;
                }
            }

            if (!item.IsBroken())
            {
                for (int s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                {
                    ItemEnchantmentType enchant_display_type = (ItemEnchantmentType)pEnchant.Effect[s];
                    uint enchant_amount = pEnchant.EffectPointsMin[s];
                    uint enchant_spell_id = pEnchant.EffectArg[s];

                    switch (enchant_display_type)
                    {
                        case ItemEnchantmentType.None:
                            break;
                        case ItemEnchantmentType.CombatSpell:
                            // processed in Player.CastItemCombatSpell
                            break;
                        case ItemEnchantmentType.Damage:
                            if (item.GetSlot() == EquipmentSlot.MainHand)
                            {
                                if (item.GetTemplate().GetInventoryType() != InventoryType.Ranged && item.GetTemplate().GetInventoryType() != InventoryType.RangedRight)
                                    HandleStatModifier(UnitMods.DamageMainHand, UnitModifierType.TotalValue, enchant_amount, apply);
                                else
                                    HandleStatModifier(UnitMods.DamageRanged, UnitModifierType.TotalValue, enchant_amount, apply);
                            }
                            else if (item.GetSlot() == EquipmentSlot.OffHand)
                                HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalValue, enchant_amount, apply);
                            break;
                        case ItemEnchantmentType.EquipSpell:
                            if (enchant_spell_id != 0)
                            {
                                if (apply)
                                {
                                    int basepoints = 0;
                                    // Random Property Exist - try found basepoints for spell (basepoints depends from item suffix factor)
                                    if (item.GetItemRandomPropertyId() < 0)
                                    {
                                        ItemRandomSuffixRecord item_rand = CliDB.ItemRandomSuffixStorage.LookupByKey(Math.Abs(item.GetItemRandomPropertyId()));
                                        if (item_rand != null)
                                        {
                                            // Search enchant_amount
                                            for (int k = 0; k < ItemConst.MaxItemRandomProperties; ++k)
                                            {
                                                if (item_rand.Enchantment[k] == enchant_id)
                                                {
                                                    basepoints = (int)((item_rand.AllocationPct[k] * item.GetItemSuffixFactor()) / 10000);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    // Cast custom spell vs all equal basepoints got from enchant_amount
                                    if (basepoints != 0)
                                        CastCustomSpell(this, enchant_spell_id, basepoints, basepoints, basepoints, true, item);
                                    else
                                        CastSpell(this, enchant_spell_id, true, item);
                                }
                                else
                                    RemoveAurasDueToItemSpell(enchant_spell_id, item.GetGUID());
                            }
                            break;
                        case ItemEnchantmentType.Resistance:
                            if (pEnchant.ScalingClass != 0)
                            {
                                int scalingClass = pEnchant.ScalingClass;
                                if ((GetUInt32Value(UnitFields.MinItemLevel) != 0 || GetUInt32Value(UnitFields.MaxItemlevel) != 0) && pEnchant.ScalingClassRestricted != 0)
                                    scalingClass = pEnchant.ScalingClassRestricted;

                                uint minLevel = ((uint)(pEnchant.Flags)).HasAnyFlag(0x20u) ? 1 : 60u;
                                uint scalingLevel = getLevel();
                                byte maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.GetTableRowCount() - 1);

                                if (minLevel > getLevel())
                                    scalingLevel = minLevel;
                                else if (maxLevel < getLevel())
                                    scalingLevel = maxLevel;

                                GtSpellScalingRecord spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);
                                if (spellScaling != null)
                                    enchant_amount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
                            }

                            if (enchant_amount == 0)
                            {
                                ItemRandomSuffixRecord item_rand = CliDB.ItemRandomSuffixStorage.LookupByKey(Math.Abs(item.GetItemRandomPropertyId()));
                                if (item_rand != null)
                                {
                                    for (int k = 0; k < ItemConst.MaxItemRandomProperties; ++k)
                                    {
                                        if (item_rand.Enchantment[k] == enchant_id)
                                        {
                                            enchant_amount = (item_rand.AllocationPct[k] * item.GetItemSuffixFactor()) / 10000;
                                            break;
                                        }
                                    }
                                }
                            }

                            HandleStatModifier((UnitMods)((uint)UnitMods.ResistanceStart + enchant_spell_id), UnitModifierType.TotalValue, enchant_amount, apply);
                            break;
                        case ItemEnchantmentType.Stat:
                            {
                                if (pEnchant.ScalingClass != 0)
                                {
                                    int scalingClass = pEnchant.ScalingClass;
                                    if ((GetUInt32Value(UnitFields.MinItemLevel) != 0 || GetUInt32Value(UnitFields.MaxItemlevel) != 0) && pEnchant.ScalingClassRestricted != 0)
                                        scalingClass = pEnchant.ScalingClassRestricted;

                                    uint minLevel = ((uint)(pEnchant.Flags)).HasAnyFlag(0x20u) ? 1 : 60u;
                                    uint scalingLevel = getLevel();
                                    byte maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.GetTableRowCount() - 1);

                                    if (minLevel > getLevel())
                                        scalingLevel = minLevel;
                                    else if (maxLevel < getLevel())
                                        scalingLevel = maxLevel;

                                    GtSpellScalingRecord spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);
                                    if (spellScaling != null)
                                        enchant_amount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
                                }

                                if (enchant_amount == 0)
                                {
                                    ItemRandomSuffixRecord item_rand_suffix = CliDB.ItemRandomSuffixStorage.LookupByKey(Math.Abs(item.GetItemRandomPropertyId()));
                                    if (item_rand_suffix != null)
                                    {
                                        for (int k = 0; k < ItemConst.MaxItemRandomProperties; ++k)
                                        {
                                            if (item_rand_suffix.Enchantment[k] == enchant_id)
                                            {
                                                enchant_amount = (item_rand_suffix.AllocationPct[k] * item.GetItemSuffixFactor()) / 10000;
                                                break;
                                            }
                                        }
                                    }
                                }

                                Log.outDebug(LogFilter.Player, "Adding {0} to stat nb {1}", enchant_amount, enchant_spell_id);
                                switch ((ItemModType)enchant_spell_id)
                                {
                                    case ItemModType.Mana:
                                        Log.outDebug(LogFilter.Player, "+ {0} MANA", enchant_amount);
                                        HandleStatModifier(UnitMods.Mana, UnitModifierType.BaseValue, enchant_amount, apply);
                                        break;
                                    case ItemModType.Health:
                                        Log.outDebug(LogFilter.Player, "+ {0} HEALTH", enchant_amount);
                                        HandleStatModifier(UnitMods.Health, UnitModifierType.BaseValue, enchant_amount, apply);
                                        break;
                                    case ItemModType.Agility:
                                        Log.outDebug(LogFilter.Player, "+ {0} AGILITY", enchant_amount);
                                        HandleStatModifier(UnitMods.StatAgility, UnitModifierType.TotalValue, enchant_amount, apply);
                                        ApplyStatBuffMod(Stats.Agility, enchant_amount, apply);
                                        break;
                                    case ItemModType.Strength:
                                        Log.outDebug(LogFilter.Player, "+ {0} STRENGTH", enchant_amount);
                                        HandleStatModifier(UnitMods.StatStrength, UnitModifierType.TotalValue, enchant_amount, apply);
                                        ApplyStatBuffMod(Stats.Strength, enchant_amount, apply);
                                        break;
                                    case ItemModType.Intellect:
                                        Log.outDebug(LogFilter.Player, "+ {0} INTELLECT", enchant_amount);
                                        HandleStatModifier(UnitMods.StatIntellect, UnitModifierType.TotalValue, enchant_amount, apply);
                                        ApplyStatBuffMod(Stats.Intellect, enchant_amount, apply);
                                        break;
                                    //case ItemModType.Spirit:
                                    //Log.outDebug(LogFilter.Player, "+ {0} SPIRIT", enchant_amount);
                                    //HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.TotalValue, enchant_amount, apply);
                                    //ApplyStatBuffMod(Stats.Spirit, enchant_amount, apply);
                                    //break;
                                    case ItemModType.Stamina:
                                        Log.outDebug(LogFilter.Player, "+ {0} STAMINA", enchant_amount);
                                        HandleStatModifier(UnitMods.StatStamina, UnitModifierType.TotalValue, enchant_amount, apply);
                                        ApplyStatBuffMod(Stats.Stamina, enchant_amount, apply);
                                        break;
                                    case ItemModType.DefenseSkillRating:
                                        ApplyRatingMod(CombatRating.DefenseSkill, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} DEFENSE", enchant_amount);
                                        break;
                                    case ItemModType.DodgeRating:
                                        ApplyRatingMod(CombatRating.Dodge, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} DODGE", enchant_amount);
                                        break;
                                    case ItemModType.ParryRating:
                                        ApplyRatingMod(CombatRating.Parry, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} PARRY", enchant_amount);
                                        break;
                                    case ItemModType.BlockRating:
                                        ApplyRatingMod(CombatRating.Block, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} SHIELD_BLOCK", enchant_amount);
                                        break;
                                    case ItemModType.HitMeleeRating:
                                        ApplyRatingMod(CombatRating.HitMelee, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} MELEE_HIT", enchant_amount);
                                        break;
                                    case ItemModType.HitRangedRating:
                                        ApplyRatingMod(CombatRating.HitRanged, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} RANGED_HIT", enchant_amount);
                                        break;
                                    case ItemModType.HitSpellRating:
                                        ApplyRatingMod(CombatRating.HitSpell, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} SPELL_HIT", enchant_amount);
                                        break;
                                    case ItemModType.CritMeleeRating:
                                        ApplyRatingMod(CombatRating.CritMelee, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} MELEE_CRIT", enchant_amount);
                                        break;
                                    case ItemModType.CritRangedRating:
                                        ApplyRatingMod(CombatRating.CritRanged, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} RANGED_CRIT", enchant_amount);
                                        break;
                                    case ItemModType.CritSpellRating:
                                        ApplyRatingMod(CombatRating.CritSpell, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} SPELL_CRIT", enchant_amount);
                                        break;
                                    case ItemModType.HasteSpellRating:
                                        ApplyRatingMod(CombatRating.HasteSpell, (int)enchant_amount, apply);
                                        break;
                                    case ItemModType.HitRating:
                                        ApplyRatingMod(CombatRating.HitMelee, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.HitRanged, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.HitSpell, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} HIT", enchant_amount);
                                        break;
                                    case ItemModType.CritRating:
                                        ApplyRatingMod(CombatRating.CritMelee, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.CritRanged, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.CritSpell, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} CRITICAL", enchant_amount);
                                        break;
                                    case ItemModType.ResilienceRating:
                                        ApplyRatingMod(CombatRating.ResiliencePlayerDamage, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} RESILIENCE", enchant_amount);
                                        break;
                                    case ItemModType.HasteRating:
                                        ApplyRatingMod(CombatRating.HasteMelee, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.HasteRanged, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.HasteSpell, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} HASTE", enchant_amount);
                                        break;
                                    case ItemModType.ExpertiseRating:
                                        ApplyRatingMod(CombatRating.Expertise, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} EXPERTISE", enchant_amount);
                                        break;
                                    case ItemModType.AttackPower:
                                        HandleStatModifier(UnitMods.AttackPower, UnitModifierType.TotalValue, enchant_amount, apply);
                                        HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalValue, enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} ATTACK_POWER", enchant_amount);
                                        break;
                                    case ItemModType.RangedAttackPower:
                                        HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalValue, enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} RANGED_ATTACK_POWER", enchant_amount);
                                        break;
                                    case ItemModType.ManaRegeneration:
                                        ApplyManaRegenBonus((int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} MANA_REGENERATION", enchant_amount);
                                        break;
                                    case ItemModType.ArmorPenetrationRating:
                                        ApplyRatingMod(CombatRating.ArmorPenetration, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} ARMOR PENETRATION", enchant_amount);
                                        break;
                                    case ItemModType.SpellPower:
                                        ApplySpellPowerBonus((int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} SPELL_POWER", enchant_amount);
                                        break;
                                    case ItemModType.HealthRegen:
                                        ApplyHealthRegenBonus((int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} HEALTH_REGENERATION", enchant_amount);
                                        break;
                                    case ItemModType.SpellPenetration:
                                        ApplySpellPenetrationBonus((int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} SPELL_PENETRATION", enchant_amount);
                                        break;
                                    case ItemModType.BlockValue:
                                        HandleBaseModValue(BaseModGroup.ShieldBlockValue, BaseModType.FlatMod, enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} BLOCK_VALUE", enchant_amount);
                                        break;
                                    case ItemModType.MasteryRating:
                                        ApplyRatingMod(CombatRating.Mastery, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} MASTERY", enchant_amount);
                                        break;
                                    case ItemModType.Versatility:
                                        ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)enchant_amount, apply);
                                        ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)enchant_amount, apply);
                                        Log.outDebug(LogFilter.Player, "+ {0} VERSATILITY", enchant_amount);
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            }
                        case ItemEnchantmentType.Totem:           // Shaman Rockbiter Weapon
                            {
                                if (GetClass() == Class.Shaman)
                                {
                                    float addValue = 0.0f;
                                    if (item.GetSlot() == EquipmentSlot.MainHand)
                                    {
                                        addValue = enchant_amount * item.GetTemplate().GetDelay() / 1000.0f;
                                        HandleStatModifier(UnitMods.DamageMainHand, UnitModifierType.TotalValue, addValue, apply);
                                    }
                                    else if (item.GetSlot() == EquipmentSlot.OffHand)
                                    {
                                        addValue = enchant_amount * item.GetTemplate().GetDelay() / 1000.0f;
                                        HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalValue, addValue, apply);
                                    }
                                }
                                break;
                            }
                        case ItemEnchantmentType.UseSpell:
                            // processed in Player.CastItemUseSpell
                            break;
                        case ItemEnchantmentType.PrismaticSocket:
                        case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                        case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                        case ItemEnchantmentType.BonusListID:
                        case ItemEnchantmentType.BonusListCurve:
                        case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                            // nothing do..
                            break;
                        default:
                            Log.outError(LogFilter.Player, "Unknown item enchantment (id = {0}) display type: {1}", enchant_id, enchant_display_type);
                            break;
                    }
                }
            }

            // visualize enchantment at player and equipped items
            if (slot == EnchantmentSlot.Perm)
                SetUInt16Value(PlayerFields.VisibleItem + 1 + (item.GetSlot() * 2), 1, item.GetVisibleItemVisual(this));

            if (apply_dur)
            {
                if (apply)
                {
                    // set duration
                    uint duration = item.GetEnchantmentDuration(slot);
                    if (duration > 0)
                        AddEnchantmentDuration(item, slot, duration);
                }
                else
                {
                    // duration == 0 will remove EnchantDuration
                    AddEnchantmentDuration(item, slot, 0);
                }
            }
        }

        public void ModifySkillBonus(SkillType skillid, int val, bool talent)
        {
            var skill = mSkillStatus.LookupByKey(skillid);
            if (skill == null || skill.State == SkillState.Deleted)
                return;

            int field = (int)(skill.Pos / 2 + (talent ? ActivePlayerFields.SkillLinePermBonus : ActivePlayerFields.SkillLineTempBonus));
            byte offset = (byte)(skill.Pos & 1);

            ushort bonus = GetUInt16Value(field, offset);

            SetUInt16Value(field, offset, (ushort)(bonus + val));
        }

        public void StopCastingBindSight()
        {
            WorldObject target = GetViewpoint();
            if (target)
            {
                if (target.isTypeMask(TypeMask.Unit))
                {
                    ((Unit)target).RemoveAurasByType(AuraType.BindSight, GetGUID());
                    ((Unit)target).RemoveAurasByType(AuraType.ModPossess, GetGUID());
                    ((Unit)target).RemoveAurasByType(AuraType.ModPossessPet, GetGUID());
                }
            }
        }

        void AddEnchantmentDurations(Item item)
        {
            for (EnchantmentSlot x = 0; x < EnchantmentSlot.Max; ++x)
            {
                if (item.GetEnchantmentId(x) == 0)
                    continue;

                uint duration = item.GetEnchantmentDuration(x);
                if (duration > 0)
                    AddEnchantmentDuration(item, x, duration);
            }
        }
        void AddEnchantmentDuration(Item item, EnchantmentSlot slot, uint duration)
        {
            if (item == null)
                return;

            if (slot >= EnchantmentSlot.Max)
                return;

            foreach (var enchantDuration in m_enchantDuration)
            {
                if (enchantDuration.item == item && enchantDuration.slot == slot)
                {
                    enchantDuration.item.SetEnchantmentDuration(enchantDuration.slot, enchantDuration.leftduration, this);
                    m_enchantDuration.Remove(enchantDuration);
                    break;
                }
            }
            if (item != null && duration > 0)
            {
                GetSession().SendItemEnchantTimeUpdate(GetGUID(), item.GetGUID(), (uint)slot, duration / 1000);
                m_enchantDuration.Add(new EnchantDuration(item, slot, duration));
            }
        }
        void RemoveEnchantmentDurations(Item item)
        {
            foreach (var enchantDuration in m_enchantDuration)
            {
                if (enchantDuration.item == item)
                {
                    // save duration in item
                    item.SetEnchantmentDuration(enchantDuration.slot, enchantDuration.leftduration, this);
                    m_enchantDuration.Remove(enchantDuration);
                }
            }
        }
        public void RemoveArenaEnchantments(EnchantmentSlot slot)
        {
            // remove enchantments from equipped items first to clean up the m_enchantDuration list
            foreach (var enchantDuration in m_enchantDuration)
            {
                if (enchantDuration.slot == slot)
                {
                    if (enchantDuration.item && enchantDuration.item.GetEnchantmentId(slot) != 0)
                    {
                        // Poisons and DK runes are enchants which are allowed on arenas
                        if (Global.SpellMgr.IsArenaAllowedEnchancment(enchantDuration.item.GetEnchantmentId(slot)))
                            continue;

                        // remove from stats
                        ApplyEnchantment(enchantDuration.item, slot, false, false);
                        // remove visual
                        enchantDuration.item.ClearEnchantment(slot);
                    }
                    // remove from update list
                    m_enchantDuration.Remove(enchantDuration);
                }
            }

            // remove enchants from inventory items
            // NOTE: no need to remove these from stats, since these aren't equipped
            // in inventory
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetEnchantmentId(slot) != 0)
                        pItem.ClearEnchantment(slot);
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem)
                            if (pItem.GetEnchantmentId(slot) != 0)
                                pItem.ClearEnchantment(slot);
                    }
                }
            }
        }

        public void UpdatePotionCooldown(Spell spell = null)
        {
            // no potion used i combat or still in combat
            if (m_lastPotionId == 0 || IsInCombat())
                return;

            // Call not from spell cast, send cooldown event for item spells if no in combat
            if (!spell)
            {
                // spell/item pair let set proper cooldown (except not existed charged spell cooldown spellmods for potions)
                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(m_lastPotionId);
                if (proto != null)
                    for (byte idx = 0; idx < proto.Effects.Count; ++idx)
                    {
                        if (proto.Effects[idx].SpellID != 0 && proto.Effects[idx].TriggerType == ItemSpelltriggerType.OnUse)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)proto.Effects[idx].SpellID);
                            if (spellInfo != null)
                                GetSpellHistory().SendCooldownEvent(spellInfo, m_lastPotionId);
                        }
                    }
            }
            // from spell cases (m_lastPotionId set in Spell.SendSpellCooldown)
            else
                GetSpellHistory().SendCooldownEvent(spell.m_spellInfo, m_lastPotionId, spell);

            m_lastPotionId = 0;
        }

        public bool CanUseMastery()
        {
            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(GetUInt32Value(PlayerFields.CurrentSpecId));
            if (chrSpec != null)
                return HasSpell(chrSpec.MasterySpellID[0]) || HasSpell(chrSpec.MasterySpellID[1]);

            return false;
        }

        public bool HasSkill(SkillType skill)
        {
            if (skill == 0)
                return false;

            var _skill = mSkillStatus.LookupByKey((uint)skill);
            return _skill != null && _skill.State != SkillState.Deleted;
        }
        public void SetSkill(SkillType skill, uint step, uint newVal, uint maxVal)
        {
            SetSkill((uint)skill, step, newVal, maxVal);
        }
        public void SetSkill(uint id, uint step, uint newVal, uint maxVal)
        {
            if (id == 0)
                return;

            ushort currVal;
            var skillStatusData = mSkillStatus.LookupByKey(id);

            //has skill
            if (skillStatusData != null && skillStatusData.State != SkillState.Deleted)
            {
                var field = (ushort)(skillStatusData.Pos / 2);
                var offset = (byte)(skillStatusData.Pos & 1);
                currVal = GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
                if (newVal != 0)
                {
                    // if skill value is going down, update enchantments before setting the new value
                    if (newVal < currVal)
                        UpdateSkillEnchantments(id, currVal, (ushort)newVal);

                    // update step
                    SetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset, (ushort)step);
                    // update value
                    SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, (ushort)newVal);
                    SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, (ushort)maxVal);

                    if (skillStatusData.State != SkillState.New)
                        skillStatusData.State = SkillState.Changed;

                    LearnSkillRewardedSpells(id, newVal);
                    // if skill value is going up, update enchantments after setting the new value
                    if (newVal > currVal)
                        UpdateSkillEnchantments(id, currVal, (ushort)newVal);

                    UpdateCriteria(CriteriaTypes.ReachSkillLevel, id);
                    UpdateCriteria(CriteriaTypes.LearnSkillLevel, id);
                }
                else                                                //remove
                {
                    //remove enchantments needing this skill
                    UpdateSkillEnchantments(id, currVal, 0);
                    // clear skill fields
                    SetUInt16Value(ActivePlayerFields.SkillLineId + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset, 0);

                    // mark as deleted or simply remove from map if not saved yet
                    if (skillStatusData.State != SkillState.New)
                        skillStatusData.State = SkillState.Deleted;
                    else
                        mSkillStatus.Remove(id);

                    // remove all spells that related to this skill
                    List<SkillLineAbilityRecord> skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(id);
                    foreach (SkillLineAbilityRecord skillLineAbility in skillLineAbilities)
                        RemoveSpell(Global.SpellMgr.GetFirstSpellInChain(skillLineAbility.Spell));

                    foreach (SkillLineRecord childSkillLine in CliDB.SkillLineStorage.Values)
                    {
                        if (childSkillLine.ParentSkillLineID == id)
                            SetSkill(childSkillLine.Id, 0, 0, 0);
                    }

                    // Clear profession lines
                    if (GetUInt32Value(ActivePlayerFields.ProfessionSkillLine) == id)
                        SetUInt32Value(ActivePlayerFields.ProfessionSkillLine, 0);
                    else if (GetUInt32Value(ActivePlayerFields.ProfessionSkillLine + 1) == id)
                        SetUInt32Value(ActivePlayerFields.ProfessionSkillLine + 1, 0);
                }
            }
            else if (newVal != 0)                                        //add
            {
                currVal = 0;
                for (int i = 0; i < SkillConst.MaxPlayerSkills; ++i)
                {
                    var field = (ushort)(i / 2);
                    var offset = (byte)(i & 1);
                    if (GetUInt16Value(ActivePlayerFields.SkillLineId + field, offset) == 0)
                    {
                        var skillEntry = CliDB.SkillLineStorage.LookupByKey(id);
                        if (skillEntry == null)
                        {
                            Log.outError(LogFilter.Spells, "Skill not found in SkillLineStore: skill #{0}", id);
                            return;
                        }


                        if (skillEntry.ParentSkillLineID != 0 && skillEntry.ParentTierIndex > 0)
                        {
                            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillEntry.ParentSkillLineID, GetRace(), GetClass());
                            if (rcEntry != null)
                            {
                                SkillTiersEntry tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);
                                if (tier != null)
                                {
                                    ushort skillval = GetPureSkillValue((SkillType)skillEntry.ParentSkillLineID);
                                    SetSkill((SkillType)skillEntry.ParentSkillLineID, (uint)skillEntry.ParentTierIndex, Math.Max(skillval, (ushort)1), tier.Value[skillEntry.ParentTierIndex - 1]);
                                }
                            }
                            if (skillEntry.CategoryID == SkillCategory.Profession)
                            {
                                int freeProfessionSlot = FindProfessionSlotFor(id);
                                if (freeProfessionSlot != -1)
                                    SetUInt32Value(ActivePlayerFields.ProfessionSkillLine + freeProfessionSlot, id);
                            }
                        }

                        SetUInt16Value(ActivePlayerFields.SkillLineId + field, offset, (ushort)id);
                        SetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset, (ushort)step);
                        SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, (ushort)newVal);
                        SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, (ushort)maxVal);

                        UpdateSkillEnchantments(id, currVal, (ushort)newVal);
                        UpdateCriteria(CriteriaTypes.ReachSkillLevel, id);
                        UpdateCriteria(CriteriaTypes.LearnSkillLevel, id);

                        // insert new entry or update if not deleted old entry yet
                        if (skillStatusData != null)
                        {
                            skillStatusData.Pos = (byte)i;
                            skillStatusData.State = SkillState.Changed;
                        }
                        else
                            mSkillStatus.Add(id, new SkillStatusData((uint)i, SkillState.New));

                        // apply skill bonuses
                        SetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset, 0);
                        SetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset, 0);

                        // temporary bonuses
                        var mModSkill = GetAuraEffectsByType(AuraType.ModSkill);
                        foreach (var j in mModSkill)
                        {
                            if (j.GetMiscValue() == id)
                                j.HandleEffect(this, AuraEffectHandleModes.Skill, true);
                        }

                        var mModSkill2 = GetAuraEffectsByType(AuraType.ModSkill2);
                        foreach (var j in mModSkill2)
                        {
                            if (j.GetMiscValue() == id)
                                j.HandleEffect(this, AuraEffectHandleModes.Skill, true);
                        }

                        // permanent bonuses
                        var mModSkillTalent = GetAuraEffectsByType(AuraType.ModSkillTalent);
                        foreach (var eff in mModSkillTalent)
                        {
                            if (eff.GetMiscValue() == id)
                                eff.HandleEffect(this, AuraEffectHandleModes.Skill, true);
                        }

                        // Learn all spells for skill
                        LearnSkillRewardedSpells(id, newVal);
                        return;
                    }
                }
            }
        }

        public bool UpdateCraftSkill(uint spellid)
        {
            Log.outDebug(LogFilter.Player, "UpdateCraftSkill spellid {0}", spellid);

            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellid);

            foreach (var _spell_idx in bounds)
            {
                if (_spell_idx.SkillupSkillLineID != 0)
                {
                    uint SkillValue = GetPureSkillValue((SkillType)_spell_idx.SkillupSkillLineID);

                    // Alchemy Discoveries here
                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(spellid);
                    if (spellEntry != null && spellEntry.Mechanic == Mechanics.Discovery)
                    {
                        uint discoveredSpell = SkillDiscovery.GetSkillDiscoverySpell(_spell_idx.SkillupSkillLineID, spellid, this);
                        if (discoveredSpell != 0)
                            LearnSpell(discoveredSpell, false);
                    }

                    uint craft_skill_gain = _spell_idx.NumSkillUps * WorldConfig.GetUIntValue(WorldCfg.SkillGainCrafting);

                    return UpdateSkillPro(_spell_idx.SkillupSkillLineID, SkillGainChance(SkillValue, _spell_idx.TrivialSkillLineRankHigh,
                        (uint)(_spell_idx.TrivialSkillLineRankHigh + _spell_idx.TrivialSkillLineRankLow) / 2, _spell_idx.TrivialSkillLineRankLow), craft_skill_gain);
                }
            }
            return false;
        }
        public bool UpdateGatherSkill(uint SkillId, uint SkillValue, uint RedLevel, uint Multiplicator = 1)
        {
            return UpdateGatherSkill((SkillType)SkillId, SkillValue, RedLevel, Multiplicator);
        }
        public bool UpdateGatherSkill(SkillType SkillId, uint SkillValue, uint RedLevel, uint Multiplicator = 1)
        {
            Log.outDebug(LogFilter.Player, "UpdateGatherSkill(SkillId {0} SkillLevel {1} RedLevel {2})", SkillId, SkillValue, RedLevel);

            uint gathering_skill_gain = WorldConfig.GetUIntValue(WorldCfg.SkillGainGathering);

            // For skinning and Mining chance decrease with level. 1-74 - no decrease, 75-149 - 2 times, 225-299 - 8 times
            switch (SkillId)
            {
                case SkillType.Herbalism:
                case SkillType.Herbalism2:
                case SkillType.OutlandHerbalism:
                case SkillType.NorthrendHerbalism:
                case SkillType.CataclysmHerbalism:
                case SkillType.PandariaHerbalism:
                case SkillType.DraenorHerbalism:
                case SkillType.LegionHerbalism:
                case SkillType.KulTiranHerbalism:
                case SkillType.Jewelcrafting:
                case SkillType.Inscription:
                    return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, RedLevel + 100, RedLevel + 50, RedLevel + 25) * (int)Multiplicator, gathering_skill_gain);
                case SkillType.Skinning:
                case SkillType.Skinning2:
                case SkillType.OutlandSkinning:
                case SkillType.NorthrendSkinning:
                case SkillType.CataclysmSkinning:
                case SkillType.PandariaSkinning:
                case SkillType.DraenorSkinning:
                case SkillType.LegionSkinning:
                case SkillType.KulTiranSkinning:
                    if (WorldConfig.GetIntValue(WorldCfg.SkillChanceSkinningSteps) == 0)
                        return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, RedLevel + 100, RedLevel + 50, RedLevel + 25) * (int)Multiplicator, gathering_skill_gain);
                    else
                        return UpdateSkillPro(SkillId, (int)(SkillGainChance(SkillValue, RedLevel + 100, RedLevel + 50, RedLevel + 25) * Multiplicator) >> (int)(SkillValue / WorldConfig.GetIntValue(WorldCfg.SkillChanceSkinningSteps)), gathering_skill_gain);
                case SkillType.Mining:
                case SkillType.Mining2:
                case SkillType.OutlandMining:
                case SkillType.NorthrendMining:
                case SkillType.CataclysmMining:
                case SkillType.PandariaMining:
                case SkillType.DraenorMining:
                case SkillType.LegionMining:
                case SkillType.KulTiranMining:
                    if (WorldConfig.GetIntValue(WorldCfg.SkillChanceMiningSteps) == 0)
                        return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, RedLevel + 100, RedLevel + 50, RedLevel + 25) * (int)Multiplicator, gathering_skill_gain);
                    else
                        return UpdateSkillPro(SkillId, (int)(SkillGainChance(SkillValue, RedLevel + 100, RedLevel + 50, RedLevel + 25) * Multiplicator) >> (int)(SkillValue / WorldConfig.GetIntValue(WorldCfg.SkillChanceMiningSteps)), gathering_skill_gain);
            }
            return false;
        }

        byte GetFishingStepsNeededToLevelUp(uint SkillValue)
        {
            // These formulas are guessed to be as close as possible to how the skill difficulty curve for fishing was on Retail.
            if (SkillValue < 75)
                return 1;

            if (SkillValue <= 300)
                return (byte)(SkillValue / 44);

            return (byte)(SkillValue / 31);
        }

        public bool UpdateFishingSkill()
        {
            Log.outDebug(LogFilter.Player, "UpdateFishingSkill");

            uint SkillValue = GetPureSkillValue(SkillType.Fishing);

            if (SkillValue >= GetMaxSkillValue(SkillType.Fishing))
                return false;

            byte stepsNeededToLevelUp = GetFishingStepsNeededToLevelUp(SkillValue);
            ++m_fishingSteps;

            if (m_fishingSteps >= stepsNeededToLevelUp)
            {
                m_fishingSteps = 0;

                uint gathering_skill_gain = WorldConfig.GetUIntValue(WorldCfg.SkillGainGathering);
                return UpdateSkillPro(SkillType.Fishing, 100 * 10, gathering_skill_gain);
            }

            return false;
        }

        int SkillGainChance(uint SkillValue, uint GrayLevel, uint GreenLevel, uint YellowLevel)
        {
            if (SkillValue >= GrayLevel)
                return WorldConfig.GetIntValue(WorldCfg.SkillChanceGrey) * 10;
            if (SkillValue >= GreenLevel)
                return WorldConfig.GetIntValue(WorldCfg.SkillChanceGreen) * 10;
            if (SkillValue >= YellowLevel)
                return WorldConfig.GetIntValue(WorldCfg.SkillChanceYellow) * 10;
            return WorldConfig.GetIntValue(WorldCfg.SkillChanceOrange) * 10;
        }

        bool EnchantmentFitsRequirements(uint enchantmentcondition, sbyte slot)
        {
            if (enchantmentcondition == 0)
                return true;

            SpellItemEnchantmentConditionRecord Condition = CliDB.SpellItemEnchantmentConditionStorage.LookupByKey(enchantmentcondition);

            if (Condition == null)
                return true;

            byte[] curcount = { 0, 0, 0, 0 };

            //counting current equipped gem colors
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                if (i == slot)
                    continue;

                Item pItem2 = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem2 != null && !pItem2.IsBroken())
                {
                    foreach (ItemDynamicFieldGems gemData in pItem2.GetGems())
                    {
                        ItemTemplate gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);
                        if (gemProto == null)
                            continue;

                        GemPropertiesRecord gemProperty = CliDB.GemPropertiesStorage.LookupByKey(gemProto.GetGemProperties());
                        if (gemProperty == null)
                            continue;

                        uint GemColor = (uint)gemProperty.Type;

                        for (byte b = 0, tmpcolormask = 1; b < 4; b++, tmpcolormask <<= 1)
                        {
                            if (Convert.ToBoolean(tmpcolormask & GemColor))
                                ++curcount[b];
                        }
                    }
                }
            }

            bool activate = true;

            for (byte i = 0; i < 5; i++)
            {
                if (Condition.LtOperandType[i] == 0)
                    continue;

                uint _cur_gem = curcount[Condition.LtOperandType[i] - 1];

                // if have <CompareColor> use them as count, else use <value> from Condition
                uint _cmp_gem = Condition.RtOperandType[i] != 0 ? curcount[Condition.RtOperandType[i] - 1] : Condition.RtOperand[i];

                switch (Condition.Operator[i])
                {
                    case 2:                                         // requires less <color> than (<value> || <comparecolor>) gems
                        activate &= (_cur_gem < _cmp_gem) ? true : false;
                        break;
                    case 3:                                         // requires more <color> than (<value> || <comparecolor>) gems
                        activate &= (_cur_gem > _cmp_gem) ? true : false;
                        break;
                    case 5:                                         // requires at least <color> than (<value> || <comparecolor>) gems
                        activate &= (_cur_gem >= _cmp_gem) ? true : false;
                        break;
                }
            }

            Log.outDebug(LogFilter.Player, "Checking Condition {0}, there are {1} Meta Gems, {2} Red Gems, {3} Yellow Gems and {4} Blue Gems, Activate:{5}", enchantmentcondition, curcount[0], curcount[1], curcount[2], curcount[3], activate ? "yes" : "no");

            return activate;
        }
        void CorrectMetaGemEnchants(byte exceptslot, bool apply)
        {
            //cycle all equipped items
            for (byte slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
            {
                //enchants for the slot being socketed are handled by Player.ApplyItemMods
                if (slot == exceptslot)
                    continue;

                Item pItem = GetItemByPos(InventorySlots.Bag0, slot);

                if (pItem == null || pItem.GetSocketColor(0) == 0)
                    continue;

                for (EnchantmentSlot enchant_slot = EnchantmentSlot.Sock1; enchant_slot < EnchantmentSlot.Sock3; ++enchant_slot)
                {
                    uint enchant_id = pItem.GetEnchantmentId(enchant_slot);
                    if (enchant_id == 0)
                        continue;

                    SpellItemEnchantmentRecord enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                    if (enchantEntry == null)
                        continue;

                    uint condition = enchantEntry.ConditionID;
                    if (condition != 0)
                    {
                        //was enchant active with/without item?
                        bool wasactive = EnchantmentFitsRequirements(condition, (sbyte)(apply ? exceptslot : -1));
                        //should it now be?
                        if (wasactive ^ EnchantmentFitsRequirements(condition, (sbyte)(apply ? -1 : exceptslot)))
                        {
                            // ignore item gem conditions
                            //if state changed, (dis)apply enchant
                            ApplyEnchantment(pItem, enchant_slot, !wasactive, true, true);
                        }
                    }
                }
            }
        }

        public void CastItemUseSpell(Item item, SpellCastTargets targets, ObjectGuid castCount, uint[] misc)
        {
            ItemTemplate proto = item.GetTemplate();
            // special learning case
            if (proto.Effects.Count >= 2)
            {
                if (proto.Effects[0].SpellID == 483 || proto.Effects[0].SpellID == 55884)
                {
                    uint learn_spell_id = (uint)proto.Effects[0].SpellID;
                    uint learning_spell_id = (uint)proto.Effects[1].SpellID;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(learn_spell_id);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Player.CastItemUseSpell: Item (Entry: {0}) in have wrong spell id {1}, ignoring ", proto.GetId(), learn_spell_id);
                        SendEquipError(InventoryResult.InternalBagError, item);
                        return;
                    }

                    Spell spell = new Spell(this, spellInfo, TriggerCastFlags.None);

                    SpellPrepare spellPrepare = new SpellPrepare();
                    spellPrepare.ClientCastID = castCount;
                    spellPrepare.ServerCastID = spell.m_castId;
                    SendPacket(spellPrepare);

                    spell.m_fromClient = true;
                    spell.m_CastItem = item;
                    spell.SetSpellValue(SpellValueMod.BasePoint0, (int)learning_spell_id);
                    spell.prepare(targets);
                    return;
                }
            }

            // item spells casted at use
            for (byte i = 0; i < proto.Effects.Count; ++i)
            {
                var spellData = proto.Effects[i];

                // no spell
                if (spellData.SpellID == 0)
                    continue;

                // wrong triggering type
                if (spellData.TriggerType != ItemSpelltriggerType.OnUse)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)spellData.SpellID);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Player, "Player.CastItemUseSpell: Item (Entry: {0}) in have wrong spell id {1}, ignoring", proto.GetId(), spellData.SpellID);
                    continue;
                }

                Spell spell = new Spell(this, spellInfo, TriggerCastFlags.None);

                SpellPrepare spellPrepare = new SpellPrepare();
                spellPrepare.ClientCastID = castCount;
                spellPrepare.ServerCastID = spell.m_castId;
                SendPacket(spellPrepare);

                spell.m_fromClient = true;
                spell.m_CastItem = item;
                spell.m_misc.Data0 = misc[0];
                spell.m_misc.Data1 = misc[1];
                spell.prepare(targets);
                return;
            }

            // Item enchantments spells casted at use
            for (EnchantmentSlot e_slot = 0; e_slot < EnchantmentSlot.Max; ++e_slot)
            {
                uint enchant_id = item.GetEnchantmentId(e_slot);
                var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                if (pEnchant == null)
                    continue;
                for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                {
                    if (pEnchant.Effect[s] != ItemEnchantmentType.UseSpell)
                        continue;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pEnchant.EffectArg[s]);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Player.CastItemUseSpell Enchant {0}, cast unknown spell {1}", enchant_id, pEnchant.EffectArg[s]);
                        continue;
                    }

                    Spell spell = new Spell(this, spellInfo, TriggerCastFlags.None);

                    SpellPrepare spellPrepare = new SpellPrepare();
                    spellPrepare.ClientCastID = castCount;
                    spellPrepare.ServerCastID = spell.m_castId;
                    SendPacket(spellPrepare);

                    spell.m_fromClient = true;
                    spell.m_CastItem = item;
                    spell.m_misc.Data0 = misc[0];
                    spell.m_misc.Data1 = misc[1];
                    spell.prepare(targets);
                    return;
                }
            }
        }

        public uint GetLastPotionId() { return m_lastPotionId; }
        public void SetLastPotionId(uint item_id) { m_lastPotionId = item_id; }

        void LearnSkillRewardedSpells(uint skillId, uint skillValue)
        {
            ulong raceMask = getRaceMask();
            uint classMask = getClassMask();

            List<SkillLineAbilityRecord> skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(skillId);
            foreach (var ability in skillLineAbilities)
            {
                if (ability.SkillLine != skillId)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ability.Spell);
                if (spellInfo == null)
                    continue;

                if (ability.AcquireMethod != AbilityLearnType.OnSkillValue && ability.AcquireMethod != AbilityLearnType.OnSkillLearn)
                    continue;

                // AcquireMethod == 2 && NumSkillUps == 1 --> automatically learn riding skill spell, else we skip it (client shows riding in spellbook as trainable).
                if (skillId == (uint)SkillType.Riding && (ability.AcquireMethod != AbilityLearnType.OnSkillLearn || ability.NumSkillUps != 1))
                    continue;

                // Check race if set
                if (ability.RaceMask != 0 && !Convert.ToBoolean(ability.RaceMask & raceMask))
                    continue;

                // Check class if set
                if (ability.ClassMask != 0 && !Convert.ToBoolean(ability.ClassMask & classMask))
                    continue;

                // check level, skip class spells if not high enough
                if (getLevel() < spellInfo.SpellLevel)
                    continue;

                // need unlearn spell
                if (skillValue < ability.MinSkillLineRank && ability.AcquireMethod == AbilityLearnType.OnSkillValue)
                    RemoveSpell(ability.Spell);
                // need learn
                else if (!IsInWorld)
                    AddSpell(ability.Spell, true, true, true, false, false, ability.SkillLine);
                else
                    LearnSpell(ability.Spell, true, ability.SkillLine);

            }
        }

        int FindProfessionSlotFor(uint skillId)
        {
            SkillLineRecord skillEntry = CliDB.SkillLineStorage.LookupByKey(skillId);
            if (skillEntry == null)
                return -1;

            // both free, return first slot
            if (GetUInt32Value(ActivePlayerFields.ProfessionSkillLine) == 0 && GetUInt32Value(ActivePlayerFields.ProfessionSkillLine + 1) == 0)
                return 0;

            ActivePlayerFields professionsBegin = ActivePlayerFields.ProfessionSkillLine;
            ActivePlayerFields professionsEnd = professionsBegin + 2;

            // when any slot is filled we need to check both - one of them might be earlier step of the same profession
            ActivePlayerFields sameProfessionSlot = professionsEnd;
            for (var slot = professionsBegin; slot < professionsEnd; ++slot)
            {
                SkillLineRecord slotProfession = CliDB.SkillLineStorage.LookupByKey(GetUInt32Value(slot));
                if (slotProfession != null)
                {
                    if (slotProfession.ParentSkillLineID == skillEntry.ParentSkillLineID)
                    {
                        sameProfessionSlot = slot;
                        break;
                    }
                }
            }

            if (sameProfessionSlot != professionsEnd)
            {
                if (CliDB.SkillLineStorage.LookupByKey(sameProfessionSlot).ParentTierIndex < skillEntry.ParentTierIndex)
                    return sameProfessionSlot - professionsBegin;
                return -1;
            }

            // if there is no same profession, find any free slot
            for (var slot = professionsBegin; slot < professionsEnd; ++slot)
                if (GetUInt32Value(slot) == 0)
                    return slot - professionsBegin;

            return -1;
        }

        void RemoveItemDependentAurasAndCasts(Item pItem)
        {
            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;

                // skip not self applied auras
                SpellInfo spellInfo = aura.GetSpellInfo();
                if (aura.GetCasterGUID() != GetGUID())
                    continue;

                // skip if not item dependent or have alternative item
                if (HasItemFitToSpellRequirements(spellInfo, pItem))
                    continue;

                // no alt item, remove aura, restart check
                RemoveOwnedAura(pair);
            }

            // currently casted spells can be dependent from item
            for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
            {
                Spell spell = GetCurrentSpell(i);
                if (spell != null)
                    if (spell.getState() != SpellState.Delayed && !HasItemFitToSpellRequirements(spell.m_spellInfo, pItem))
                        InterruptSpell(i);
            }
        }

        public bool HasItemFitToSpellRequirements(SpellInfo spellInfo, Item ignoreItem = null)
        {
            if (spellInfo.EquippedItemClass < 0)
                return true;

            // scan other equipped items for same requirements (mostly 2 daggers/etc)
            // for optimize check 2 used cases only
            switch (spellInfo.EquippedItemClass)
            {
                case ItemClass.Weapon:
                    {
                        Item item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                        if (item)
                            if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                                return true;

                        item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                        if (item)
                            if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                                return true;
                        break;
                    }
                case ItemClass.Armor:
                    {
                        if (!spellInfo.HasAttribute(SpellAttr8.ArmorSpecialization))
                        {
                            // tabard not have dependent spells
                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.MainHand; ++i)
                            {
                                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                                if (item)
                                    if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                                        return true;
                            }

                            // shields can be equipped to offhand slot
                            Item item1 = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                            if (item1)
                                if (item1 != ignoreItem && item1.IsFitToSpellRequirements(spellInfo))
                                    return true;
                        }
                        else
                        {
                            // requires item equipped in all armor slots
                            foreach (byte i in new[] { EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Chest, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Wrist, EquipmentSlot.Hands })
                            {
                                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                                if (!item || item == ignoreItem || !item.IsFitToSpellRequirements(spellInfo))
                                    return false;
                            }

                            return true;
                        }
                        break;
                    }
                default:
                    Log.outError(LogFilter.Player, "HasItemFitToSpellRequirements: Not handled spell requirement for item class {0}", spellInfo.EquippedItemClass);
                    break;
            }

            return false;
        }

        public Dictionary<uint, PlayerSpell> GetSpellMap() { return m_spells; }

        void CastAllObtainSpells()
        {
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte slot = InventorySlots.ItemStart; slot < inventoryEnd; ++slot)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, slot);
                if (item)
                    ApplyItemObtainSpells(item, true);
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag bag = GetBagByPos(i);
                if (!bag)
                    continue;

                for (byte slot = 0; slot < bag.GetBagSize(); ++slot)
                {
                    Item item = bag.GetItemByPos(slot);
                    if (item)
                        ApplyItemObtainSpells(item, true);
                }
            }
        }

        void ApplyItemObtainSpells(Item item, bool apply)
        {
            ItemTemplate itemTemplate = item.GetTemplate();
            for (byte i = 0; i < itemTemplate.Effects.Count; ++i)
            {
                if (itemTemplate.Effects[i].TriggerType != ItemSpelltriggerType.OnObtain) // On obtain trigger
                    continue;

                int spellId = itemTemplate.Effects[i].SpellID;
                if (spellId <= 0)
                    continue;

                if (apply)
                {
                    if (!HasAura((uint)spellId))
                        CastSpell(this, (uint)spellId, true, item);
                }
                else
                    RemoveAurasDueToSpell((uint)spellId);
            }
        }

        public void ApplyItemDependentAuras(Item item, bool apply)
        {
            if (apply)
            {
                var spells = GetSpellMap();
                foreach (var pair in spells)
                {
                    if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                        continue;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);
                    if (spellInfo == null || !spellInfo.IsPassive() || spellInfo.EquippedItemClass < 0)
                        continue;

                    if (!HasAura(pair.Key) && HasItemFitToSpellRequirements(spellInfo))
                        AddAura(pair.Key, this);  // no SMSG_SPELL_GO in sniff found
                }
            }
            else
                RemoveItemDependentAurasAndCasts(item);
        }

        public void AddTemporarySpell(uint spellId)
        {
            var spell = m_spells.LookupByKey(spellId);
            // spell already added - do not do anything
            if (spell != null)
                return;

            PlayerSpell newspell = new PlayerSpell();
            newspell.State = PlayerSpellState.Temporary;
            newspell.Active = true;
            newspell.Dependent = false;
            newspell.Disabled = false;

            m_spells[spellId] = newspell;
        }

        public void RemoveTemporarySpell(uint spellId)
        {
            var spell = m_spells.LookupByKey(spellId);
            // spell already not in list - do not do anything
            if (spell == null)
                return;
            // spell has other state than temporary - do not change it
            if (spell.State != PlayerSpellState.Temporary)
                return;

            m_spells.Remove(spellId);
        }

        public void UpdateZoneDependentAuras(uint newZone)
        {
            // Some spells applied at enter into zone (with subzones), aura removed in UpdateAreaDependentAuras that called always at zone.area update
            var saBounds = Global.SpellMgr.GetSpellAreaForAreaMapBounds(newZone);
            foreach (var spell in saBounds)
                if (spell.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, newZone, 0))
                    if (!HasAura(spell.spellId))
                        CastSpell(this, spell.spellId, true);
        }

        public void UpdateAreaDependentAuras(uint newArea)
        {
            // remove auras from spells with area limitations
            foreach (var pair in GetOwnedAuras())
            {
                // use m_zoneUpdateId for speed: UpdateArea called from UpdateZone or instead UpdateZone in both cases m_zoneUpdateId up-to-date
                if (pair.Value.GetSpellInfo().CheckLocation(GetMapId(), m_zoneUpdateId, newArea, this) != SpellCastResult.SpellCastOk)
                    RemoveOwnedAura(pair);
            }

            // some auras applied at subzone enter
            var saBounds = Global.SpellMgr.GetSpellAreaForAreaMapBounds(newArea);
            foreach (var spell in saBounds)
                if (spell.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, m_zoneUpdateId, newArea))
                    if (!HasAura(spell.spellId))
                        CastSpell(this, spell.spellId, true);
        }

        public void ApplyModToSpell(SpellModifier mod, Spell spell)
        {
            if (spell == null)
                return;

            // don't do anything with no charges
            if (mod.ownerAura.IsUsingCharges() && mod.ownerAura.GetCharges() == 0)
                return;

            // register inside spell, proc system uses this to drop charges
            spell.m_appliedMods.Add(mod.ownerAura);
        }

        public void LearnCustomSpells()
        {
            if (!WorldConfig.GetBoolValue(WorldCfg.StartAllSpells))
                return;

            // learn default race/class spells
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            foreach (var tspell in info.customSpells)
            {
                Log.outDebug(LogFilter.Player, "PLAYER (Class: {0} Race: {1}): Adding initial spell, id = {2}", GetClass(), GetRace(), tspell);
                if (!IsInWorld)                                    // will send in INITIAL_SPELLS in list anyway at map add
                    AddSpell(tspell, true, true, true, false);
                else                                                // but send in normal spell in game learn case
                    LearnSpell(tspell, true);
            }
        }

        public void LearnDefaultSkills()
        {
            // learn default race/class skills
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            foreach (var rcInfo in info.skills)
            {
                if (HasSkill((SkillType)rcInfo.SkillID))
                    continue;

                if (rcInfo.MinLevel > getLevel())
                    continue;

                LearnDefaultSkill(rcInfo);
            }
        }

        public void LearnDefaultSkill(SkillRaceClassInfoRecord rcInfo)
        {
            SkillType skillId = (SkillType)rcInfo.SkillID;
            switch (Global.SpellMgr.GetSkillRangeType(rcInfo))
            {
                case SkillRangeType.Language:
                    SetSkill(skillId, 0, 300, 300);
                    break;
                case SkillRangeType.Level:
                    {
                        ushort skillValue = 1;
                        ushort maxValue = GetMaxSkillValueForLevel();
                        if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                            skillValue = maxValue;
                        else if (GetClass() == Class.Deathknight)
                            skillValue = (ushort)Math.Min(Math.Max(1, (getLevel() - 1) * 5), maxValue);
                        else if (skillId == SkillType.FistWeapons)
                            skillValue = Math.Max((ushort)1, GetSkillValue(SkillType.Unarmed));

                        SetSkill(skillId, 0, skillValue, maxValue);
                        break;
                    }
                case SkillRangeType.Mono:
                    SetSkill(skillId, 0, 1, 1);
                    break;
                case SkillRangeType.Rank:
                    {
                        SkillTiersEntry tier = Global.ObjectMgr.GetSkillTier(rcInfo.SkillTierID);
                        ushort maxValue = (ushort)tier.Value[0];
                        ushort skillValue = 1;
                        if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                            skillValue = maxValue;
                        else if (GetClass() == Class.Deathknight)
                            skillValue = (ushort)Math.Min(Math.Max(1, (getLevel() - 1) * 5), maxValue);

                        SetSkill(skillId, 1, skillValue, maxValue);
                        break;
                    }
                default:
                    break;
            }
        }

        void SendKnownSpells()
        {
            SendKnownSpells knownSpells = new SendKnownSpells();
            knownSpells.InitialLogin = false; // @todo

            foreach (var spell in m_spells.ToList())
            {
                if (spell.Value.State == PlayerSpellState.Removed)
                    continue;

                if (!spell.Value.Active || spell.Value.Disabled)
                    continue;

                knownSpells.KnownSpells.Add(spell.Key);
            }

            SendPacket(knownSpells);
        }

        public void LearnSpellHighestRank(uint spellid)
        {
            LearnSpell(spellid, false);

            uint next = Global.SpellMgr.GetNextSpellInChain(spellid);
            if (next != 0)
                LearnSpellHighestRank(next);
        }

        public void LearnSpell(uint spellId, bool dependent, uint fromSkill = 0)
        {
            PlayerSpell spell = m_spells.LookupByKey(spellId);

            bool disabled = (spell != null) ? spell.Disabled : false;
            bool active = !disabled || spell.Active;

            bool learning = AddSpell(spellId, active, true, dependent, false, false, fromSkill);

            // prevent duplicated entires in spell book, also not send if not in world (loading)
            if (learning && IsInWorld)
            {
                LearnedSpells packet = new LearnedSpells();
                packet.SpellID.Add(spellId);
                SendPacket(packet);
            }

            // learn all disabled higher ranks and required spells (recursive)
            if (disabled)
            {
                uint nextSpell = Global.SpellMgr.GetNextSpellInChain(spellId);
                if (nextSpell != 0)
                {
                    var _spell = m_spells.LookupByKey(nextSpell);
                    if (spellId != 0 && _spell.Disabled)
                        LearnSpell(nextSpell, false, fromSkill);
                }

                var spellsRequiringSpell = Global.SpellMgr.GetSpellsRequiringSpellBounds(spellId);
                foreach (var id in spellsRequiringSpell)
                {
                    var spell1 = m_spells.LookupByKey(id);
                    if (spell1 != null && spell1.Disabled)
                        LearnSpell(id, false, fromSkill);
                }
            }

        }

        public void RemoveSpell(uint spell_id, bool disabled = false, bool learn_low_rank = true)
        {
            var pSpell = m_spells.LookupByKey(spell_id);
            if (pSpell == null)
                return;

            if (pSpell.State == PlayerSpellState.Removed || (disabled && pSpell.Disabled) || pSpell.State == PlayerSpellState.Temporary)
                return;

            // unlearn non talent higher ranks (recursive)
            uint nextSpell = Global.SpellMgr.GetNextSpellInChain(spell_id);
            if (nextSpell != 0)
            {
                SpellInfo spellInfo1 = Global.SpellMgr.GetSpellInfo(nextSpell);
                if (HasSpell(nextSpell) && !spellInfo1.HasAttribute(SpellCustomAttributes.IsTalent))
                    RemoveSpell(nextSpell, disabled, false);
            }
            //unlearn spells dependent from recently removed spells
            var spellsRequiringSpell = Global.SpellMgr.GetSpellsRequiringSpellBounds(spell_id);
            foreach (var id in spellsRequiringSpell)
                RemoveSpell(id, disabled);

            // re-search, it can be corrupted in prev loop
            pSpell = m_spells.LookupByKey(spell_id);
            if (pSpell == null)
                return;                                             // already unleared

            bool cur_active = pSpell.Active;
            bool cur_dependent = pSpell.Dependent;

            if (disabled)
            {
                pSpell.Disabled = disabled;
                if (pSpell.State != PlayerSpellState.New)
                    pSpell.State = PlayerSpellState.Changed;
            }
            else
            {
                if (pSpell.State == PlayerSpellState.New)
                    m_spells.Remove(spell_id);
                else
                    pSpell.State = PlayerSpellState.Removed;
            }

            RemoveOwnedAura(spell_id, GetGUID());

            // remove pet auras
            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
            {
                PetAura petSpell = Global.SpellMgr.GetPetAura(spell_id, i);
                if (petSpell != null)
                    RemovePetAura(petSpell);
            }

            // update free primary prof.points (if not overflow setting, can be in case GM use before .learn prof. learning)
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id);
            if (spellInfo != null && spellInfo.IsPrimaryProfessionFirstRank())
            {
                uint freeProfs = GetFreePrimaryProfessionPoints() + 1;
                if (freeProfs <= WorldConfig.GetIntValue(WorldCfg.MaxPrimaryTradeSkill))
                    SetFreePrimaryProfessions(freeProfs);
            }

            // remove dependent skill
            var spellLearnSkill = Global.SpellMgr.GetSpellLearnSkill(spell_id);
            if (spellLearnSkill != null)
            {
                uint prev_spell = Global.SpellMgr.GetPrevSpellInChain(spell_id);
                if (prev_spell == 0)                                    // first rank, remove skill
                    SetSkill(spellLearnSkill.skill, 0, 0, 0);
                else
                {
                    // search prev. skill setting by spell ranks chain
                    var prevSkill = Global.SpellMgr.GetSpellLearnSkill(prev_spell);
                    while (prevSkill == null && prev_spell != 0)
                    {
                        prev_spell = Global.SpellMgr.GetPrevSpellInChain(prev_spell);
                        prevSkill = Global.SpellMgr.GetSpellLearnSkill(Global.SpellMgr.GetFirstSpellInChain(prev_spell));
                    }

                    if (prevSkill == null)                                 // not found prev skill setting, remove skill
                        SetSkill(spellLearnSkill.skill, 0, 0, 0);
                    else                                            // set to prev. skill setting values
                    {
                        uint skill_value = GetPureSkillValue(prevSkill.skill);
                        uint skill_max_value = GetPureMaxSkillValue(prevSkill.skill);

                        if (skill_value > prevSkill.value)
                            skill_value = prevSkill.value;

                        uint new_skill_max_value = prevSkill.maxvalue == 0 ? GetMaxSkillValueForLevel() : prevSkill.maxvalue;

                        if (skill_max_value > new_skill_max_value)
                            skill_max_value = new_skill_max_value;

                        SetSkill(prevSkill.skill, prevSkill.step, skill_value, skill_max_value);
                    }
                }
            }

            // remove dependent spells
            var spell_bounds = Global.SpellMgr.GetSpellLearnSpellMapBounds(spell_id);

            foreach (var spellNode in spell_bounds)
            {
                RemoveSpell(spellNode.Spell, disabled);
                if (spellNode.OverridesSpell != 0)
                    RemoveOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
            }

            // activate lesser rank in spellbook/action bar, and cast it if need
            bool prev_activate = false;

            uint prev_id = Global.SpellMgr.GetPrevSpellInChain(spell_id);
            if (prev_id != 0)
            {
                // if ranked non-stackable spell: need activate lesser rank and update dendence state
                // No need to check for spellInfo != NULL here because if cur_active is true, then that means that the spell was already in m_spells, and only valid spells can be pushed there.
                if (cur_active && spellInfo.IsRanked())
                {
                    // need manually update dependence state (learn spell ignore like attempts)
                    var prevSpell = m_spells.LookupByKey(prev_id);
                    if (prevSpell != null)
                    {
                        if (prevSpell.Dependent != cur_dependent)
                        {
                            prevSpell.Dependent = cur_dependent;
                            if (prevSpell.State != PlayerSpellState.New)
                                prevSpell.State = PlayerSpellState.Changed;
                        }

                        // now re-learn if need re-activate
                        if (cur_active && !prevSpell.Active && learn_low_rank)
                        {
                            if (AddSpell(prev_id, true, false, prevSpell.Dependent, prevSpell.Disabled))
                            {
                                // downgrade spell ranks in spellbook and action bar
                                SendSupercededSpell(spell_id, prev_id);
                                prev_activate = true;
                            }
                        }
                    }
                }
            }

            m_overrideSpells.Remove(spell_id);

            if (m_canTitanGrip)
            {
                if (spellInfo != null && spellInfo.IsPassive() && spellInfo.HasEffect(SpellEffectName.TitanGrip))
                {
                    RemoveAurasDueToSpell(m_titanGripPenaltySpellId);
                    SetCanTitanGrip(false);
                }
            }

            if (CanDualWield())
            {
                if (spellInfo != null && spellInfo.IsPassive() && spellInfo.HasEffect(SpellEffectName.DualWield))
                    SetCanDualWield(false);
            }

            if (WorldConfig.GetBoolValue(WorldCfg.OffhandCheckAtSpellUnlearn))
                AutoUnequipOffhandIfNeed();

            // remove from spell book if not replaced by lesser rank
            if (!prev_activate)
            {
                UnlearnedSpells removedSpells = new UnlearnedSpells();
                removedSpells.SpellID.Add(spell_id);
                SendPacket(removedSpells);
            }
        }
        bool IsNeedCastPassiveSpellAtLearn(SpellInfo spellInfo)
        {
            // note: form passives activated with shapeshift spells be implemented by HandleShapeshiftBoosts instead of spell_learn_spell
            // talent dependent passives activated at form apply have proper stance data
            ShapeShiftForm form = GetShapeshiftForm();
            bool need_cast = (spellInfo.Stances == 0 || (form != 0 && Convert.ToBoolean(spellInfo.Stances & (1ul << ((int)form - 1)))) ||
            (form == 0 && spellInfo.HasAttribute(SpellAttr2.NotNeedShapeshift)));

            if (spellInfo.HasAttribute(SpellAttr8.MasterySpecialization))
                need_cast &= IsCurrentSpecMasterySpell(spellInfo);

            // Check EquippedItemClass
            // passive spells which apply aura and have an item requirement are to be added in Player::ApplyItemDependentAuras
            if (spellInfo.IsPassive() && spellInfo.EquippedItemClass >= 0)
            {
                foreach (SpellEffectInfo effectInfo in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                    if (effectInfo != null && effectInfo.IsAura())
                        return false;
            }

            //Check CasterAuraStates
            return need_cast && (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState));
        }

        public bool IsCurrentSpecMasterySpell(SpellInfo spellInfo)
        {
            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(GetUInt32Value(PlayerFields.CurrentSpecId));
            if (chrSpec != null)
                return spellInfo.Id == chrSpec.MasterySpellID[0] || spellInfo.Id == chrSpec.MasterySpellID[1];

            return false;
        }

        bool AddSpell(uint spellId, bool active, bool learning, bool dependent, bool disabled, bool loading = false, uint fromSkill = 0)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                // do character spell book cleanup (all characters)
                if (!IsInWorld && !learning)
                {
                    Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) does not exist. deleting for all characters in `character_spell`.", spellId);

                    DeleteSpellFromAllPlayers(spellId);
                }
                else
                    Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) does not exist", spellId);

                return false;
            }

            if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
            {
                // do character spell book cleanup (all characters)
                if (!IsInWorld && !learning)
                {
                    Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) is invalid. deleting for all characters in `character_spell`.", spellId);

                    DeleteSpellFromAllPlayers(spellId);
                }
                else
                    Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) is invalid", spellId);

                return false;
            }

            PlayerSpellState state = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

            bool dependent_set = false;
            bool disabled_case = false;
            bool superceded_old = false;

            PlayerSpell spell = m_spells.LookupByKey(spellId);
            if (spell != null && spell.State == PlayerSpellState.Temporary)
                RemoveTemporarySpell(spellId);
            if (spell != null)
            {
                uint next_active_spell_id = 0;
                // fix activate state for non-stackable low rank (and find next spell for !active case)
                if (spellInfo.IsRanked())
                {
                    uint next = Global.SpellMgr.GetNextSpellInChain(spellId);
                    if (next != 0)
                    {
                        if (HasSpell(next))
                        {
                            // high rank already known so this must !active
                            active = false;
                            next_active_spell_id = next;
                        }
                    }
                }

                // not do anything if already known in expected state
                if (spell.State != PlayerSpellState.Removed && spell.Active == active &&
                    spell.Dependent == dependent && spell.Disabled == disabled)
                {
                    if (!IsInWorld && !learning)
                        spell.State = PlayerSpellState.Unchanged;

                    return false;
                }

                // dependent spell known as not dependent, overwrite state
                if (spell.State != PlayerSpellState.Removed && !spell.Dependent && dependent)
                {
                    spell.Dependent = dependent;
                    if (spell.State != PlayerSpellState.New)
                        spell.State = PlayerSpellState.Changed;
                    dependent_set = true;
                }

                // update active state for known spell
                if (spell.Active != active && spell.State != PlayerSpellState.Removed && !spell.Disabled)
                {
                    spell.Active = active;

                    if (!IsInWorld && !learning && !dependent_set) // explicitly load from DB and then exist in it already and set correctly
                        spell.State = PlayerSpellState.Unchanged;
                    else if (spell.State != PlayerSpellState.New)
                        spell.State = PlayerSpellState.Changed;

                    if (active)
                    {
                        if (spellInfo.IsPassive() && IsNeedCastPassiveSpellAtLearn(spellInfo))
                            CastSpell(this, spellId, true);
                    }
                    else if (IsInWorld)
                    {
                        if (next_active_spell_id != 0)
                            SendSupercededSpell(spellId, next_active_spell_id);
                        else
                        {
                            UnlearnedSpells removedSpells = new UnlearnedSpells();
                            removedSpells.SpellID.Add(spellId);
                            SendPacket(removedSpells);
                        }
                    }

                    return active;
                }

                if (spell.Disabled != disabled && spell.State != PlayerSpellState.Removed)
                {
                    if (spell.State != PlayerSpellState.New)
                        spell.State = PlayerSpellState.Changed;
                    spell.Disabled = disabled;

                    if (disabled)
                        return false;

                    disabled_case = true;
                }
                else
                {
                    switch (spell.State)
                    {
                        case PlayerSpellState.Unchanged:
                            return false;
                        case PlayerSpellState.Removed:
                            {
                                m_spells.Remove(spellId);
                                state = PlayerSpellState.Changed;
                                break;
                            }
                        default:
                            {
                                // can be in case spell loading but learned at some previous spell loading
                                if (!IsInWorld && !learning && !dependent_set)
                                    spell.State = PlayerSpellState.Unchanged;
                                return false;
                            }
                    }
                }
            }

            if (!disabled_case) // skip new spell adding if spell already known (disabled spells case)
            {
                // non talent spell: learn low ranks (recursive call)
                uint prev_spell = Global.SpellMgr.GetPrevSpellInChain(spellId);
                if (prev_spell != 0)
                {
                    if (!IsInWorld || disabled)                    // at spells loading, no output, but allow save
                        AddSpell(prev_spell, active, true, true, disabled, false, fromSkill);
                    else                                            // at normal learning
                        LearnSpell(prev_spell, true, fromSkill);
                }

                PlayerSpell newspell = new PlayerSpell();
                newspell.State = state;
                newspell.Active = active;
                newspell.Dependent = dependent;
                newspell.Disabled = disabled;

                // replace spells in action bars and spellbook to bigger rank if only one spell rank must be accessible
                if (newspell.Active && !newspell.Disabled && spellInfo.IsRanked())
                {
                    foreach (var _spell in m_spells)
                    {
                        if (_spell.Value.State == PlayerSpellState.Removed)
                            continue;

                        SpellInfo i_spellInfo = Global.SpellMgr.GetSpellInfo(_spell.Key);
                        if (i_spellInfo == null)
                            continue;

                        if (spellInfo.IsDifferentRankOf(i_spellInfo))
                        {
                            if (_spell.Value.Active)
                            {
                                if (spellInfo.IsHighRankOf(i_spellInfo))
                                {
                                    if (IsInWorld)                 // not send spell (re-/over-)learn packets at loading
                                        SendSupercededSpell(_spell.Key, spellId);

                                    // mark old spell as disable (SMSG_SUPERCEDED_SPELL replace it in client by new)
                                    _spell.Value.Active = false;
                                    if (_spell.Value.State != PlayerSpellState.New)
                                        _spell.Value.State = PlayerSpellState.Changed;
                                    superceded_old = true;          // new spell replace old in action bars and spell book.
                                }
                                else
                                {
                                    if (IsInWorld)                 // not send spell (re-/over-)learn packets at loading
                                        SendSupercededSpell(spellId, _spell.Key);

                                    // mark new spell as disable (not learned yet for client and will not learned)
                                    newspell.Active = false;
                                    if (newspell.State != PlayerSpellState.New)
                                        newspell.State = PlayerSpellState.Changed;
                                }
                            }
                        }
                    }
                }
                m_spells[spellId] = newspell;

                // return false if spell disabled
                if (newspell.Disabled)
                    return false;
            }

            // cast talents with SPELL_EFFECT_LEARN_SPELL (other dependent spells will learned later as not auto-learned)
            // note: all spells with SPELL_EFFECT_LEARN_SPELL isn't passive
            if (!loading && spellInfo.HasAttribute(SpellCustomAttributes.IsTalent) && spellInfo.HasEffect(SpellEffectName.LearnSpell))
            {
                // ignore stance requirement for talent learn spell (stance set for spell only for client spell description show)
                CastSpell(this, spellId, true);
            }
            // also cast passive spells (including all talents without SPELL_EFFECT_LEARN_SPELL) with additional checks
            else if (spellInfo.IsPassive())
            {
                if (IsNeedCastPassiveSpellAtLearn(spellInfo))
                    CastSpell(this, spellId, true);
            }
            else if (spellInfo.HasEffect(SpellEffectName.SkillStep))
            {
                CastSpell(this, spellId, true);
                return false;
            }

            // update free primary prof.points (if any, can be none in case GM .learn prof. learning)
            uint freeProfs = GetFreePrimaryProfessionPoints();
            if (freeProfs != 0)
            {
                if (spellInfo.IsPrimaryProfessionFirstRank())
                    SetFreePrimaryProfessions(freeProfs - 1);
            }

            var skill_bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);

            SpellLearnSkillNode spellLearnSkill = Global.SpellMgr.GetSpellLearnSkill(spellId);
            if (spellLearnSkill != null)
            {
                // add dependent skills if this spell is not learned from adding skill already
                if ((uint)spellLearnSkill.skill != fromSkill)
                {
                    ushort skill_value = GetPureSkillValue(spellLearnSkill.skill);
                    ushort skill_max_value = GetPureMaxSkillValue(spellLearnSkill.skill);

                    if (skill_value < spellLearnSkill.value)
                        skill_value = spellLearnSkill.value;

                    ushort new_skill_max_value = spellLearnSkill.maxvalue == 0 ? GetMaxSkillValueForLevel() : spellLearnSkill.maxvalue;

                    if (skill_max_value < new_skill_max_value)
                        skill_max_value = new_skill_max_value;

                    SetSkill(spellLearnSkill.skill, spellLearnSkill.step, skill_value, skill_max_value);
                }
            }
            else
            {
                // not ranked skills
                foreach (var _spell_idx in skill_bounds)
                {
                    SkillLineRecord pSkill = CliDB.SkillLineStorage.LookupByKey(_spell_idx.SkillLine);
                    if (pSkill == null)
                        continue;

                    if (_spell_idx.SkillLine == fromSkill)
                        continue;

                    // Runeforging special case
                    if ((_spell_idx.AcquireMethod == AbilityLearnType.OnSkillLearn && !HasSkill((SkillType)_spell_idx.SkillLine))
                        || ((_spell_idx.SkillLine == (int)SkillType.Runeforging) && _spell_idx.TrivialSkillLineRankHigh == 0))
                    {
                        SkillRaceClassInfoRecord rcInfo = Global.DB2Mgr.GetSkillRaceClassInfo(_spell_idx.SkillLine, GetRace(), GetClass());
                        if (rcInfo != null)
                            LearnDefaultSkill(rcInfo);
                    }
                }
            }


            // learn dependent spells
            var spell_bounds = Global.SpellMgr.GetSpellLearnSpellMapBounds(spellId);
            foreach (var spellNode in spell_bounds)
            {
                if (!spellNode.AutoLearned)
                {
                    if (!IsInWorld || !spellNode.Active)       // at spells loading, no output, but allow save
                        AddSpell(spellNode.Spell, spellNode.Active, true, true, false);
                    else                                            // at normal learning
                        LearnSpell(spellNode.Spell, true);
                }

                if (spellNode.OverridesSpell != 0 && spellNode.Active)
                    AddOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
            }

            if (!GetSession().PlayerLoading())
            {
                // not ranked skills
                foreach (var _spell_idx in skill_bounds)
                {
                    UpdateCriteria(CriteriaTypes.LearnSkillLine, _spell_idx.SkillLine);
                    UpdateCriteria(CriteriaTypes.LearnSkilllineSpells, _spell_idx.SkillLine);
                }

                UpdateCriteria(CriteriaTypes.LearnSpell, spellId);
            }

            // needs to be when spell is already learned, to prevent infinite recursion crashes
            if (Global.DB2Mgr.GetMount(spellId) != null)
                GetSession().GetCollectionMgr().AddMount(spellId, MountStatusFlags.None, false, !IsInWorld);

            // need to add Battle pets automatically into pet journal
            foreach (BattlePetSpeciesRecord entry in CliDB.BattlePetSpeciesStorage.Values)
            {
                if (entry.SummonSpellID == spellId && GetSession().GetBattlePetMgr().GetPetCount(entry.Id) == 0)
                {
                    GetSession().GetBattlePetMgr().AddPet(entry.Id, entry.CreatureID, BattlePetMgr.RollPetBreed(entry.Id), BattlePetMgr.GetDefaultPetQuality(entry.Id));
                    UpdateCriteria(CriteriaTypes.OwnBattlePetCount);
                    break;
                }
            }

            // return true (for send learn packet) only if spell active (in case ranked spells) and not replace old spell
            return active && !disabled && !superceded_old;
        }
        public override bool HasSpell(uint spellId)
        {
            var spell = m_spells.LookupByKey(spellId);
            if (spell != null)
                return spell.State != PlayerSpellState.Removed && !spell.Disabled;

            return false;
        }
        public bool HasActiveSpell(uint spellId)
        {
            var spell = m_spells.LookupByKey(spellId);
            if (spell != null)
                return spell.State != PlayerSpellState.Removed && spell.Active
                    && !spell.Disabled;

            return false;
        }

        public void AddSpellMod(SpellModifier mod, bool apply)
        {
            Log.outDebug(LogFilter.Spells, "Player.AddSpellMod {0}", mod.spellId);

            // First, manipulate our spellmodifier container
            if (apply)
                m_spellMods[(int)mod.op][(int)mod.type].Add(mod);
            else
                m_spellMods[(int)mod.op][(int)mod.type].Remove(mod);

            // Now, send spellmodifier packet
            if (!IsLoading())
            {
                ServerOpcodes opcode = (mod.type == SpellModType.Flat ? ServerOpcodes.SetFlatSpellModifier : ServerOpcodes.SetPctSpellModifier);
                SetSpellModifier packet = new SetSpellModifier(opcode);

                // @todo Implement sending of bulk modifiers instead of single
                SpellModifierInfo spellMod = new SpellModifierInfo();

                spellMod.ModIndex = (byte)mod.op;
                for (int eff = 0; eff < 128; ++eff)
                {
                    FlagArray128 mask = new FlagArray128();
                    mask[eff / 32] = 1u << (eff %32);
                    if (mod.mask & mask)
                    {
                        SpellModifierData modData = new SpellModifierData();
                        if (mod.type == SpellModType.Flat)
                        {
                            modData.ModifierValue = 0.0f;
                            foreach (var spell in m_spellMods[(int)mod.op][(int)SpellModType.Flat])
                                if (spell.mask & mask)
                                    modData.ModifierValue += spell.value;
                        }
                        else
                        {
                            modData.ModifierValue = 1.0f;
                            foreach (var spell in m_spellMods[(int)mod.op][(int)SpellModType.Pct])
                                if (spell.mask & mask)
                                    modData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, spell.value);
                        }

                        modData.ClassIndex = (byte)eff;

                        spellMod.ModifierData.Add(modData);
                    }
                }
                packet.Modifiers.Add(spellMod);

                SendPacket(packet);
            }
        }

        public void ApplySpellMod(uint spellId, SpellModOp op, ref int basevalue, Spell spell = null)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
                return;

            float totalmul = 1.0f;
            int totalflat = 0;

            // Drop charges for triggering spells instead of triggered ones
            if (m_spellModTakingSpell != null)
                spell = m_spellModTakingSpell;

            switch (op)
            {
                // special case, if a mod makes spell instant, only consume that mod
                case SpellModOp.CastingTime:
                    {
                        SpellModifier modInstantSpell = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Pct])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (basevalue < 10000 && mod.value <= -100)
                            {
                                modInstantSpell = mod;
                                break;
                            }
                        }

                        if (modInstantSpell != null)
                        {
                            ApplyModToSpell(modInstantSpell, spell);
                            basevalue = 0;
                            return;
                        }
                        break;
                    }
                // special case if two mods apply 100% critical chance, only consume one
                case SpellModOp.CriticalChance:
                    {
                        SpellModifier modCritical = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Flat])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (mod.value >= 100)
                            {
                                modCritical = mod;
                                break;
                            }
                        }

                        if (modCritical != null)
                        {
                            ApplyModToSpell(modCritical, spell);
                            basevalue = 100;
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Flat])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                totalflat += mod.value;
                ApplyModToSpell(mod, spell);
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Pct])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                // skip percent mods for null basevalue (most important for spell mods with charges)
                if (basevalue + totalflat == 0)
                    continue;

                // special case (skip > 10sec spell casts for instant cast setting)
                if (op == SpellModOp.CastingTime)
                {
                    if (basevalue >= 10000 && mod.value <= -100)
                        continue;
                }

                totalmul *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.value);
                ApplyModToSpell(mod, spell);
            }

            basevalue = (int)((float)(basevalue + totalflat) * totalmul);
        }

        public void ApplySpellMod(uint spellId, SpellModOp op, ref uint basevalue, Spell spell = null)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
                return;

            float totalmul = 1.0f;
            int totalflat = 0;

            // Drop charges for triggering spells instead of triggered ones
            if (m_spellModTakingSpell != null)
                spell = m_spellModTakingSpell;

            switch (op)
            {
                // special case, if a mod makes spell instant, only consume that mod
                case SpellModOp.CastingTime:
                    {
                        SpellModifier modInstantSpell = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Pct])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (basevalue < 10000 && mod.value <= -100)
                            {
                                modInstantSpell = mod;
                                break;
                            }
                        }

                        if (modInstantSpell != null)
                        {
                            ApplyModToSpell(modInstantSpell, spell);
                            basevalue = 0;
                            return;
                        }
                        break;
                    }
                // special case if two mods apply 100% critical chance, only consume one
                case SpellModOp.CriticalChance:
                    {
                        SpellModifier modCritical = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Flat])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (mod.value >= 100)
                            {
                                modCritical = mod;
                                break;
                            }
                        }

                        if (modCritical != null)
                        {
                            ApplyModToSpell(modCritical, spell);
                            basevalue = 100;
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Flat])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                totalflat += mod.value;
                ApplyModToSpell(mod, spell);
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Pct])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                // skip percent mods for null basevalue (most important for spell mods with charges)
                if (basevalue + totalflat == 0)
                    continue;

                // special case (skip > 10sec spell casts for instant cast setting)
                if (op == SpellModOp.CastingTime)
                {
                    if (basevalue >= 10000 && mod.value <= -100)
                        continue;
                }

                totalmul *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.value);
                ApplyModToSpell(mod, spell);
            }

            basevalue = (uint)((float)(basevalue + totalflat) * totalmul);
        }

        public void ApplySpellMod(uint spellId, SpellModOp op, ref float basevalue, Spell spell = null)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
                return;

            float totalmul = 1.0f;
            int totalflat = 0;

            // Drop charges for triggering spells instead of triggered ones
            if (m_spellModTakingSpell != null)
                spell = m_spellModTakingSpell;

            switch (op)
            {
                // special case, if a mod makes spell instant, only consume that mod
                case SpellModOp.CastingTime:
                    {
                        SpellModifier modInstantSpell = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Pct])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (basevalue < 10000f && mod.value <= -100)
                            {
                                modInstantSpell = mod;
                                break;
                            }
                        }

                        if (modInstantSpell != null)
                        {
                            ApplyModToSpell(modInstantSpell, spell);
                            basevalue = 0f;
                            return;
                        }
                        break;
                    }
                // special case if two mods apply 100% critical chance, only consume one
                case SpellModOp.CriticalChance:
                    {
                        SpellModifier modCritical = null;
                        foreach (SpellModifier mod in m_spellMods[(int)op][(int)SpellModType.Flat])
                        {
                            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                                continue;

                            if (mod.value >= 100)
                            {
                                modCritical = mod;
                                break;
                            }
                        }

                        if (modCritical != null)
                        {
                            ApplyModToSpell(modCritical, spell);
                            basevalue = 100f;
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Flat])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                totalflat += mod.value;
                ApplyModToSpell(mod, spell);
            }

            foreach (var mod in m_spellMods[(int)op][(int)SpellModType.Pct])
            {
                if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                    continue;

                // skip percent mods for null basevalue (most important for spell mods with charges)
                if (basevalue + totalflat == 0)
                    continue;

                // special case (skip > 10sec spell casts for instant cast setting)
                if (op == SpellModOp.CastingTime)
                {
                    if (basevalue >= 10000 && mod.value <= -100)
                        continue;
                }

                totalmul *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.value);
                ApplyModToSpell(mod, spell);
            }

            basevalue = (basevalue + totalflat) * totalmul;
        }

        bool IsAffectedBySpellmod(SpellInfo spellInfo, SpellModifier mod, Spell spell)
        {
            if (mod == null || spellInfo == null)
                return false;

            // First time this aura applies a mod to us and is out of charges
            if (spell && mod.ownerAura.IsUsingCharges() && mod.ownerAura.GetCharges() == 0 && !spell.m_appliedMods.Contains(mod.ownerAura))
                return false;

            // +duration to infinite duration spells making them limited
            if (mod.op == SpellModOp.Duration && spellInfo.GetDuration() == -1)
                return false;

            return spellInfo.IsAffectedBySpellMod(mod);
        }

        public void SetSpellModTakingSpell(Spell spell, bool apply)
        {
            if (spell == null || (m_spellModTakingSpell != null && m_spellModTakingSpell != spell))
                return;

            if (apply && spell.getState() == SpellState.Finished)
                return;

            m_spellModTakingSpell = apply ? spell : null;
        }

        void SendSpellModifiers()
        {
            SetSpellModifier flatMods = new SetSpellModifier(ServerOpcodes.SetFlatSpellModifier);
            SetSpellModifier pctMods = new SetSpellModifier(ServerOpcodes.SetPctSpellModifier);
            for (var i = 0; i < (int)SpellModOp.Max; ++i)
            {
                SpellModifierInfo flatMod = new SpellModifierInfo();
                SpellModifierInfo pctMod = new SpellModifierInfo();
                flatMod.ModIndex = pctMod.ModIndex = (byte)i;
                for (byte j = 0; j < 128; ++j)
                {
                    FlagArray128 mask = new FlagArray128();
                    mask[j / 32] = 1u << (j % 32);

                    SpellModifierData flatData;
                    SpellModifierData pctData;

                    flatData.ClassIndex = j;
                    flatData.ModifierValue = 0.0f;
                    pctData.ClassIndex = j;
                    pctData.ModifierValue = 1.0f;

                    foreach (SpellModifier mod in m_spellMods[i][(int)SpellModType.Flat])
                    {
                        if (mod.mask & mask)
                            flatData.ModifierValue += mod.value;
                    }

                    foreach (SpellModifier mod in m_spellMods[i][(int)SpellModType.Pct])
                    {
                        if (mod.mask & mask)
                            pctData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.value);

                    }

                    flatMod.ModifierData.Add(flatData);
                    pctMod.ModifierData.Add(pctData);
                }

                flatMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 0.0f));

                pctMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 1.0f));

                flatMods.Modifiers.Add(flatMod);
                pctMods.Modifiers.Add(pctMod);
            }

            if (!flatMods.Modifiers.Empty())
                SendPacket(flatMods);

            if (!pctMods.Modifiers.Empty())
                SendPacket(pctMods);
        }

        void SendSupercededSpell(uint oldSpell, uint newSpell)
        {
            SupercededSpells supercededSpells = new SupercededSpells();
            supercededSpells.SpellID.Add(newSpell);
            supercededSpells.Superceded.Add(oldSpell);
            SendPacket(supercededSpells);
        }

        public void UpdateEquipSpellsAtFormChange()
        {
            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] && !m_items[i].IsBroken() && CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                {
                    ApplyItemEquipSpell(m_items[i], false, true);     // remove spells that not fit to form
                    ApplyItemEquipSpell(m_items[i], true, true);      // add spells that fit form but not active
                }
            }

            UpdateItemSetAuras(true);
        }

        void UpdateItemSetAuras(bool formChange = false)
        { 
            // item set bonuses not dependent from item broken state
            for (int setindex = 0; setindex < ItemSetEff.Count; ++setindex)
            {
                ItemSetEffect eff = ItemSetEff[setindex];
                if (eff == null)
                    continue;

                foreach (ItemSetSpellRecord itemSetSpell in eff.SetBonuses)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID);

                    if (itemSetSpell.ChrSpecID != 0 && itemSetSpell.ChrSpecID != GetUInt32Value(PlayerFields.CurrentSpecId))
                        ApplyEquipSpell(spellInfo, null, false, false);  // item set aura is not for current spec
                    else
                    {
                        ApplyEquipSpell(spellInfo, null, false, formChange); // remove spells that not fit to form - removal is skipped if shapeshift condition is satisfied
                        ApplyEquipSpell(spellInfo, null, true, formChange);  // add spells that fit form but not active
                    }
                }
            }
        }

        public int GetSpellPenetrationItemMod() { return m_spellPenetrationItemMod; }

        public void RemoveArenaSpellCooldowns(bool removeActivePetCooldowns)
        {
            // remove cooldowns on spells that have < 10 min CD
            GetSpellHistory().ResetCooldowns(p =>
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(p.Key);
                return spellInfo.RecoveryTime < 10 * Time.Minute * Time.InMilliseconds && spellInfo.CategoryRecoveryTime < 10 * Time.Minute * Time.InMilliseconds;
            }, true);

            // pet cooldowns
            if (removeActivePetCooldowns)
            {
                Pet pet = GetPet();
                if (pet)
                    pet.GetSpellHistory().ResetAllCooldowns();
            }
        }

        /**********************************/
        /*************Runes****************/
        /**********************************/
        public void SetRuneCooldown(byte index, uint cooldown)
        {
            m_runes.Cooldown[index] = cooldown;
            m_runes.SetRuneState(index, (cooldown == 0) ? true : false);
            int activeRunes = m_runes.Cooldown.Count(p => p == 0);
            if (activeRunes != GetPower(PowerType.Runes))
                SetPower(PowerType.Runes, activeRunes);
        }

        public byte GetRunesState()
        {
            return (byte)(m_runes.RuneState & ((1 << GetMaxPower(PowerType.Runes)) - 1));
        }

        public uint GetRuneBaseCooldown()
        {
            float cooldown = RuneCooldowns.Base;

            var regenAura = GetAuraEffectsByType(AuraType.ModPowerRegenPercent);
            foreach (var i in regenAura)
                if (i.GetMiscValue() == (int)PowerType.Runes)
                    cooldown *= 1.0f - i.GetAmount() / 100.0f;

            // Runes cooldown are now affected by player's haste from equipment ...
            float hastePct = GetRatingBonusValue(CombatRating.HasteMelee);

            // ... and some auras.
            hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste);
            hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste2);
            hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste3);

            cooldown *= 1.0f - (hastePct / 100.0f);

            return (uint)cooldown;
        }

        public void ResyncRunes()
        {
            int maxRunes = GetMaxPower(PowerType.Runes);

            ResyncRunes data = new ResyncRunes();
            data.Runes.Start = (byte)((1 << maxRunes) - 1);
            data.Runes.Count = GetRunesState();

            float baseCd = GetRuneBaseCooldown();
            for (byte i = 0; i < maxRunes; ++i)
                data.Runes.Cooldowns.Add((byte)((baseCd - GetRuneCooldown(i)) / baseCd * 255));

            SendPacket(data);
        }

        public void InitRunes()
        {
            if (GetClass() != Class.Deathknight)
                return;

            int runeIndex = (int)GetPowerIndex(PowerType.Runes);
            if (runeIndex == (int)PowerType.Max)
                return;

            m_runes = new Runes();
            m_runes.RuneState = 0;

            for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
                SetRuneCooldown(i, 0);                                          // reset cooldowns

            // set a base regen timer equal to 10 sec
            SetStatFloatValue(UnitFields.PowerRegenFlatModifier + runeIndex, 0.0f);
            SetStatFloatValue(UnitFields.PowerRegenInterruptedFlatModifier + runeIndex, 0.0f);
        }

        public void UpdateAllRunesRegen()
        {
            if (GetClass() != Class.Deathknight)
                return;

            int runeIndex = (int)GetPowerIndex(PowerType.Runes);
            if (runeIndex == (int)PowerType.Max)
                return;

            PowerTypeRecord runeEntry = Global.DB2Mgr.GetPowerTypeEntry(PowerType.Runes);

            uint cooldown = GetRuneBaseCooldown();
            SetStatFloatValue(UnitFields.PowerRegenFlatModifier + runeIndex, (float)(1 * Time.InMilliseconds) / (float)cooldown - runeEntry.RegenPeace);
            SetStatFloatValue(UnitFields.PowerRegenInterruptedFlatModifier + runeIndex, (float)(1 * Time.InMilliseconds) / (float)cooldown - runeEntry.RegenCombat);
        }

        public uint GetRuneCooldown(byte index) { return m_runes.Cooldown[index]; }

        public bool CanNoReagentCast(SpellInfo spellInfo)
        {
            // don't take reagents for spells with SPELL_ATTR5_NO_REAGENT_WHILE_PREP
            if (spellInfo.HasAttribute(SpellAttr5.NoReagentWhilePrep) &&
                HasFlag(UnitFields.Flags, UnitFlags.Preparation))
                return true;

            // Check no reagent use mask
            FlagArray128 noReagentMask = new FlagArray128();
            noReagentMask[0] = GetUInt32Value(ActivePlayerFields.NoReagentCost);
            noReagentMask[1] = GetUInt32Value(ActivePlayerFields.NoReagentCost + 1);
            noReagentMask[2] = GetUInt32Value(ActivePlayerFields.NoReagentCost + 2);
            noReagentMask[3] = GetUInt32Value(ActivePlayerFields.NoReagentCost + 3);
            if (spellInfo.SpellFamilyFlags & noReagentMask)
                return true;

            return false;
        }

        public void CastItemCombatSpell(DamageInfo damageInfo)
        {
            Unit target = damageInfo.GetVictim();
            if (target == null || !target.IsAlive() || target == this)
                return;

            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                // If usable, try to cast item spell
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (!item.IsBroken() && CanUseAttackType(damageInfo.GetAttackType()))
                    {
                        ItemTemplate proto = item.GetTemplate();
                        if (proto != null)
                        {
                            // Additional check for weapons
                            if (proto.GetClass() == ItemClass.Weapon)
                            {
                                // offhand item cannot proc from main hand hit etc
                                byte slot;
                                switch (damageInfo.GetAttackType())
                                {
                                    case WeaponAttackType.BaseAttack:
                                    case WeaponAttackType.RangedAttack:
                                        slot = EquipmentSlot.MainHand;
                                        break;
                                    case WeaponAttackType.OffAttack:
                                        slot = EquipmentSlot.OffHand;
                                        break;
                                    default:
                                        slot = EquipmentSlot.End;
                                        break;
                                }
                                if (slot != i)
                                    continue;
                                // Check if item is useable (forms or disarm)
                                if (damageInfo.GetAttackType() == WeaponAttackType.BaseAttack)
                                    if (!IsUseEquipedWeapon(true) && !IsInFeralForm())
                                        continue;
                            }

                            CastItemCombatSpell(damageInfo, item, proto);
                        }
                    }
                }
            }
        }

        public void CastItemCombatSpell(DamageInfo damageInfo, Item item, ItemTemplate proto)
        {
            // Can do effect if any damage done to target
            // for done procs allow normal + critical + absorbs by default
            bool canTrigger = damageInfo.GetHitMask().HasAnyFlag(ProcFlagsHit.Normal | ProcFlagsHit.Critical | ProcFlagsHit.Absorb);
            if (canTrigger)
            {
                for (byte i = 0; i < proto.Effects.Count; ++i)
                {
                    var spellData = proto.Effects[i];

                    // no spell
                    if (spellData.SpellID == 0)
                        continue;

                    // wrong triggering type
                    if (spellData.TriggerType != ItemSpelltriggerType.ChanceOnHit)
                        continue;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)spellData.SpellID);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "WORLD: unknown Item spellid {0}", spellData.SpellID);
                        continue;
                    }

                    // not allow proc extra attack spell at extra attack
                    if (m_extraAttacks != 0 && spellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                        return;

                    float chance = spellInfo.ProcChance;

                    if (proto.SpellPPMRate != 0)
                    {
                        uint WeaponSpeed = GetBaseAttackTime(damageInfo.GetAttackType());
                        chance = GetPPMProcChance(WeaponSpeed, proto.SpellPPMRate, spellInfo);
                    }
                    else if (chance > 100.0f)
                        chance = GetWeaponProcChance();

                    if (RandomHelper.randChance(chance))
                        CastSpell(damageInfo.GetVictim(), spellInfo.Id, true, item);
                }
            }

            // item combat enchantments
            for (byte e_slot = 0; e_slot < (byte)EnchantmentSlot.Max; ++e_slot)
            {
                uint enchant_id = item.GetEnchantmentId((EnchantmentSlot)e_slot);
                SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                if (pEnchant == null)
                    continue;

                for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                {
                    if (pEnchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                        continue;

                    SpellEnchantProcEntry entry = Global.SpellMgr.GetSpellEnchantProcEvent(enchant_id);

                    if (entry != null && entry.procEx != 0)
                    {
                        // Check hit/crit/dodge/parry requirement
                        if (((uint)entry.procEx & (uint)damageInfo.GetHitMask()) == 0)
                            continue;
                    }
                    else
                    {
                        // for done procs allow normal + critical + absorbs by default
                        if (!canTrigger)
                            continue;
                    }

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pEnchant.EffectArg[s]);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Player.CastItemCombatSpell(GUID: {0}, name: {1}, enchant: {2}): unknown spell {3} is casted, ignoring...",
                            GetGUID().ToString(), GetName(), enchant_id, pEnchant.EffectArg[s]);
                        continue;
                    }

                    float chance = pEnchant.EffectPointsMin[s] != 0 ? pEnchant.EffectPointsMin[s] : GetWeaponProcChance();

                    if (entry != null)
                    {
                        if (entry.PPMChance != 0)
                            chance = GetPPMProcChance(proto.GetDelay(), entry.PPMChance, spellInfo);
                        else if (entry.customChance != 0)
                            chance = entry.customChance;
                    }

                    // Apply spell mods
                    ApplySpellMod(pEnchant.EffectArg[s], SpellModOp.ChanceOfSuccess, ref chance);

                    // Shiv has 100% chance to apply the poison
                    if (FindCurrentSpellBySpellId(5938) != null && e_slot == (byte)EnchantmentSlot.Temp)
                        chance = 100.0f;

                    if (RandomHelper.randChance(chance))
                    {
                        if (spellInfo.IsPositive())
                            CastSpell(this, spellInfo, true, item);
                        else
                            CastSpell(damageInfo.GetVictim(), spellInfo, true, item);
                    }
                }
            }
        }

        float GetWeaponProcChance()
        {
            // normalized proc chance for weapon attack speed
            // (odd formula...)
            if (isAttackReady(WeaponAttackType.BaseAttack))
                return (GetBaseAttackTime(WeaponAttackType.BaseAttack) * 1.8f / 1000.0f);
            else if (haveOffhandWeapon() && isAttackReady(WeaponAttackType.OffAttack))
                return (GetBaseAttackTime(WeaponAttackType.OffAttack) * 1.6f / 1000.0f);
            return 0;
        }

        public void ResetSpells(bool myClassOnly = false)
        {
            // not need after this call
            if (HasAtLoginFlag(AtLoginFlags.ResetSpells))
                RemoveAtLoginFlag(AtLoginFlags.ResetSpells, true);

            // make full copy of map (spells removed and marked as deleted at another spell remove
            // and we can't use original map for safe iterative with visit each spell at loop end
            var smap = GetSpellMap();

            uint family;

            if (myClassOnly)
            {
                ChrClassesRecord clsEntry = CliDB.ChrClassesStorage.LookupByKey(GetClass());
                if (clsEntry == null)
                    return;
                family = clsEntry.SpellClassSet;

                foreach (var spellId in smap.Keys)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
                    if (spellInfo == null)
                        continue;

                    // skip server-side/triggered spells
                    if (spellInfo.SpellLevel == 0)
                        continue;

                    // skip wrong class/race skills
                    if (!IsSpellFitByClassAndRace(spellInfo.Id))
                        continue;

                    // skip other spell families
                    if ((uint)spellInfo.SpellFamilyName != family)
                        continue;

                    // skip broken spells
                    if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
                        continue;
                }
            }
            else
                foreach (var spellId in smap.Keys)
                    RemoveSpell(spellId, false, false);           // only iter.first can be accessed, object by iter.second can be deleted already

            LearnDefaultSkills();
            LearnCustomSpells();
            LearnQuestRewardedSpells();
        }
    }

    public class PlayerSpell
    {
        public PlayerSpellState State;
        public bool Active;
        public bool Dependent;
        public bool Disabled;
    }
}
