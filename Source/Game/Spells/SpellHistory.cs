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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Spells
{
    public class SpellHistory
    {
        public SpellHistory(Unit owner)
        {
            _owner = owner;
        }

        public void LoadFromDB<T>(SQLResult cooldownsResult, SQLResult chargesResult) where T : WorldObject
        {
            if (!cooldownsResult.IsEmpty())
            {
                do
                {
                    uint spellId = cooldownsResult.Read<uint>(0);
                    if (!Global.SpellMgr.HasSpellInfo(spellId))
                        continue;

                    int index = (typeof(T) == typeof(Pet) ? 1 : 2);

                    CooldownEntry cooldownEntry = new CooldownEntry();
                    cooldownEntry.SpellId = spellId;
                    cooldownEntry.CooldownEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<uint>(index++));
                    cooldownEntry.ItemId = 0;
                    cooldownEntry.CategoryId = cooldownsResult.Read<uint>(index++);
                    cooldownEntry.CategoryEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<uint>(index++));

                    _spellCooldowns[spellId] = cooldownEntry;
                    if (cooldownEntry.CategoryId != 0)
                        _categoryCooldowns[cooldownEntry.CategoryId] = _spellCooldowns[spellId];

                } while (cooldownsResult.NextRow());
            }

            if (!chargesResult.IsEmpty())
            {
                do
                {
                    uint categoryId = chargesResult.Read<uint>(0);

                    if (!CliDB.SpellCategoryStorage.ContainsKey(categoryId))
                        continue;

                    ChargeEntry charges;
                    charges.RechargeStart = Time.UnixTimeToDateTime(chargesResult.Read<uint>(1));
                    charges.RechargeEnd = Time.UnixTimeToDateTime(chargesResult.Read<uint>(2));
                    _categoryCharges.Add(categoryId, charges);

                } while (chargesResult.NextRow());
            }
        }

        public void SaveToDB<T>(SQLTransaction trans) where T : WorldObject
        {
            PreparedStatement stmt;
            if (typeof(T) == typeof(Pet))
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
                stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
                stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                byte index = 0;
                foreach (var pair in _spellCooldowns)
                {
                    if (!pair.Value.OnHold)
                    {
                        index = 0;
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL_COOLDOWN);
                        stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                        stmt.AddValue(index++, pair.Key);
                        stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                        stmt.AddValue(index++, pair.Value.CategoryId);
                        stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                        trans.Append(stmt);
                    }
                }

                foreach (var pair in _categoryCharges)
                {
                    index = 0;
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL_CHARGES);
                    stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                    stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                    trans.Append(stmt);
                }
            }
            else
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                stmt.AddValue(0, _owner.GetGUID().GetCounter());
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_CHARGES);
                stmt.AddValue(0, _owner.GetGUID().GetCounter());
                trans.Append(stmt);

                byte index = 0;
                foreach (var pair in _spellCooldowns)
                {
                    if (!pair.Value.OnHold)
                    {
                        index = 0;
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_COOLDOWN);
                        stmt.AddValue(index++, _owner.GetGUID().GetCounter());
                        stmt.AddValue(index++, pair.Key);
                        stmt.AddValue(index++, pair.Value.ItemId);
                        stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                        stmt.AddValue(index++, pair.Value.CategoryId);
                        stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                        trans.Append(stmt);
                    }
                }

                foreach (var pair in _categoryCharges)
                {
                    index = 0;
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_CHARGES);
                    stmt.AddValue(index++, _owner.GetGUID().GetCounter());
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                    stmt.AddValue(index++, (uint)Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                    trans.Append(stmt);
                }
            }
        }

        public void Update()
        {
            DateTime now = DateTime.Now;
            foreach (var pair in _categoryCooldowns.ToList())
            {
                if (pair.Value.CategoryEnd < now)
                    _categoryCooldowns.Remove(pair.Key);
            }

            foreach (var pair in _spellCooldowns.ToList())
            {
                if (pair.Value.CooldownEnd < now)
                {
                    _categoryCooldowns.Remove(pair.Value.CategoryId);
                    _spellCooldowns.Remove(pair.Key);
                }
            }

            foreach (var pair in _categoryCharges.KeyValueList)
            {
                if (pair.Value.RechargeEnd <= now)
                    _categoryCharges.Remove(pair);
            }
        }

        public void HandleCooldowns(SpellInfo spellInfo, Item item, Spell spell = null)
        {
            HandleCooldowns(spellInfo, item ? item.GetEntry() : 0u, spell);
        }

        public void HandleCooldowns(SpellInfo spellInfo, uint itemId, Spell spell = null)
        {
            if (ConsumeCharge(spellInfo.ChargeCategoryId))
                return;

            Player player = _owner.ToPlayer();
            if (player)
            {
                // potions start cooldown until exiting combat
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
                if (itemTemplate != null)
                {
                    if (itemTemplate.IsPotion() || spellInfo.IsCooldownStartedOnEvent())
                    {
                        player.SetLastPotionId(itemId);
                        return;
                    }
                }
            }

            if (spellInfo.IsCooldownStartedOnEvent() || spellInfo.IsPassive() || (spell && spell.IsIgnoringCooldowns()))
                return;

            StartCooldown(spellInfo, itemId, spell);
        }

        public bool IsReady(SpellInfo spellInfo, uint itemId = 0, bool ignoreCategoryCooldown = false)
        {
            if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                if (IsSchoolLocked(spellInfo.GetSchoolMask()))
                    return false;

            if (HasCooldown(spellInfo.Id, itemId, ignoreCategoryCooldown))
                return false;

            if (!HasCharge(spellInfo.ChargeCategoryId))
                return false;

            return true;
        }

        public void WritePacket(SendSpellHistory sendSpellHistory)
        {
            DateTime now = DateTime.Now;
            foreach (var p in _spellCooldowns)
            {
                SpellHistoryEntry historyEntry = new SpellHistoryEntry();
                historyEntry.SpellID = p.Key;
                historyEntry.ItemID = p.Value.ItemId;

                if (p.Value.OnHold)
                    historyEntry.OnHold = true;
                else
                {
                    TimeSpan cooldownDuration = p.Value.CooldownEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    historyEntry.RecoveryTime = (int)cooldownDuration.TotalMilliseconds;
                    TimeSpan categoryDuration = p.Value.CategoryEnd - now;
                    if (categoryDuration.TotalMilliseconds > 0)
                    {
                        historyEntry.Category = p.Value.CategoryId;
                        historyEntry.CategoryRecoveryTime = (int)categoryDuration.TotalMilliseconds;
                    }
                }

                sendSpellHistory.Entries.Add(historyEntry);
            }
        }

        public void WritePacket(SendSpellCharges sendSpellCharges)
        {
            DateTime now = DateTime.Now;
            foreach (var key in _categoryCharges.Keys)
            {
                var list = _categoryCharges[key];
                if (!list.Empty())
                {
                    TimeSpan cooldownDuration = list.FirstOrDefault().RechargeEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    SpellChargeEntry chargeEntry = new SpellChargeEntry();
                    chargeEntry.Category = key;
                    chargeEntry.NextRecoveryTime = (uint)cooldownDuration.TotalMilliseconds;
                    chargeEntry.ConsumedCharges = (byte)list.Count;
                    sendSpellCharges.Entries.Add(chargeEntry);
                }
            }
        }

        public void WritePacket(PetSpells petSpells)
        {
            DateTime now = DateTime.Now;

            foreach (var pair in _spellCooldowns)
            {
                PetSpellCooldown petSpellCooldown = new PetSpellCooldown();
                petSpellCooldown.SpellID = pair.Key;
                petSpellCooldown.Category = (ushort)pair.Value.CategoryId;

                if (!pair.Value.OnHold)
                {
                    var cooldownDuration = pair.Value.CooldownEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    petSpellCooldown.Duration = (uint)cooldownDuration.TotalMilliseconds;
                    var categoryDuration = pair.Value.CategoryEnd - now;
                    if (categoryDuration.TotalMilliseconds > 0)
                        petSpellCooldown.CategoryDuration = (uint)categoryDuration.TotalMilliseconds;
                }
                else
                    petSpellCooldown.CategoryDuration = 0x80000000;

                petSpells.Cooldowns.Add(petSpellCooldown);
            }

            foreach (var key in _categoryCharges.Keys)
            {
                var list = _categoryCharges[key];
                if (!list.Empty())
                {
                    var cooldownDuration = list.FirstOrDefault().RechargeEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    PetSpellHistory petChargeEntry = new PetSpellHistory();
                    petChargeEntry.CategoryID = key;
                    petChargeEntry.RecoveryTime = (uint)cooldownDuration.TotalMilliseconds;
                    petChargeEntry.ConsumedCharges = (sbyte)list.Count;

                    petSpells.SpellHistory.Add(petChargeEntry);
                }
            }
        }

        public void StartCooldown(SpellInfo spellInfo, uint itemId, Spell spell = null, bool onHold = false)
        {
            // init cooldown values
            uint categoryId = 0;
            int cooldown = -1;
            int categoryCooldown = -1;

            GetCooldownDurations(spellInfo, itemId, ref cooldown, ref categoryId, ref categoryCooldown);

            DateTime curTime = DateTime.Now;
            DateTime catrecTime;
            DateTime recTime;
            bool needsCooldownPacket = false;

            // overwrite time for selected category
            if (onHold)
            {
                // use +MONTH as infinite cooldown marker
                catrecTime = categoryCooldown > 0 ? (curTime + PlayerConst.InfinityCooldownDelay) : curTime;
                recTime = cooldown > 0 ? (curTime + PlayerConst.InfinityCooldownDelay) : catrecTime;
            }
            else
            {
                // shoot spells used equipped item cooldown values already assigned in GetAttackTime(RANGED_ATTACK)
                // prevent 0 cooldowns set by another way
                if (cooldown <= 0 && categoryCooldown <= 0 && (categoryId == 76 || (spellInfo.IsAutoRepeatRangedSpell() && spellInfo.Id != 75)))
                    cooldown = (int)_owner.GetUInt32Value(UnitFields.RangedAttackTime);

                // Now we have cooldown data (if found any), time to apply mods
                Player modOwner = _owner.GetSpellModOwner();
                if (modOwner)
                {
                    if (cooldown > 0)
                        modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.Cooldown, ref cooldown, spell);

                    if (categoryCooldown > 0 && !spellInfo.HasAttribute(SpellAttr6.IgnoreCategoryCooldownMods))
                        modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.Cooldown, ref categoryCooldown, spell);
                }

                if (_owner.HasAuraTypeWithAffectMask(AuraType.ModSpellCooldownByHaste, spellInfo))
                {
                    cooldown = (int)(cooldown * _owner.GetFloatValue(UnitFields.ModCastHaste));
                    categoryCooldown = (int)(categoryCooldown * _owner.GetFloatValue(UnitFields.ModCastHaste));
                }

                if (_owner.HasAuraTypeWithAffectMask(AuraType.ModCooldownByHasteRegen, spellInfo))
                {
                    cooldown = (int)(cooldown * _owner.GetFloatValue(UnitFields.ModHasteRegen));
                    categoryCooldown = (int)(categoryCooldown * _owner.GetFloatValue(UnitFields.ModHasteRegen));
                }

                int cooldownMod = _owner.GetTotalAuraModifier(AuraType.ModCooldown);
                if (cooldownMod != 0)
                {
                    // Apply SPELL_AURA_MOD_COOLDOWN only to own spells
                    Player playerOwner = GetPlayerOwner();
                    if (!playerOwner || playerOwner.HasSpell(spellInfo.Id))
                    {
                        needsCooldownPacket = true;
                        cooldown += cooldownMod * Time.InMilliseconds;   // SPELL_AURA_MOD_COOLDOWN does not affect category cooldows, verified with shaman shocks
                    }
                }

                // Apply SPELL_AURA_MOD_SPELL_CATEGORY_COOLDOWN modifiers
                // Note: This aura applies its modifiers to all cooldowns of spells with set category, not to category cooldown only
                if (categoryId != 0)
                {
                    int categoryModifier = _owner.GetTotalAuraModifierByMiscValue(AuraType.ModSpellCategoryCooldown, (int)categoryId);
                    if (categoryModifier != 0)
                    {
                        if (cooldown > 0)
                            cooldown += categoryModifier;

                        if (categoryCooldown > 0)
                            categoryCooldown += categoryModifier;
                    }

                    SpellCategoryRecord categoryEntry = CliDB.SpellCategoryStorage.LookupByKey(categoryId);
                    if (categoryEntry.Flags.HasAnyFlag(SpellCategoryFlags.CooldownExpiresAtDailyReset))
                        categoryCooldown = (int)(Time.UnixTimeToDateTime(Global.WorldMgr.GetNextDailyQuestsResetTime()) - DateTime.Now).TotalMilliseconds;
                }

                // replace negative cooldowns by 0
                if (cooldown < 0)
                    cooldown = 0;

                if (categoryCooldown < 0)
                    categoryCooldown = 0;

                // no cooldown after applying spell mods
                if (cooldown == 0 && categoryCooldown == 0)
                    return;

                catrecTime = categoryCooldown != 0 ? curTime + TimeSpan.FromMilliseconds(categoryCooldown) : curTime;
                recTime = cooldown != 0 ? curTime + TimeSpan.FromMilliseconds(cooldown) : catrecTime;
            }

            // self spell cooldown
            if (recTime != curTime)
            {
                AddCooldown(spellInfo.Id, itemId, recTime, categoryId, catrecTime, onHold);

                if (needsCooldownPacket)
                {
                    Player playerOwner = GetPlayerOwner();
                    if (playerOwner)
                    {
                        SpellCooldownPkt spellCooldown = new SpellCooldownPkt();
                        spellCooldown.Caster = _owner.GetGUID();
                        spellCooldown.Flags = SpellCooldownFlags.None;
                        spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellInfo.Id, (uint)cooldown));
                        playerOwner.SendPacket(spellCooldown);
                    }
                }
            }
        }

        public void SendCooldownEvent(SpellInfo spellInfo, uint itemId = 0, Spell spell = null, bool startCooldown = true)
        {
            Player player = GetPlayerOwner();
            if (player)
            {
                uint category = spellInfo.GetCategory();
                GetCooldownDurations(spellInfo, itemId, ref category);

                var categoryEntry = _categoryCooldowns.LookupByKey(category);
                if (categoryEntry != null && categoryEntry.SpellId != spellInfo.Id)
                {
                    player.SendPacket(new CooldownEvent(player != _owner, categoryEntry.SpellId));

                    if (startCooldown)
                        StartCooldown(Global.SpellMgr.GetSpellInfo(categoryEntry.SpellId), itemId, spell);
                }

                player.SendPacket(new CooldownEvent(player != _owner, spellInfo.Id));
            }

            // start cooldowns at server side, if any
            if (startCooldown)
                StartCooldown(spellInfo, itemId, spell);
        }

        public void AddCooldown(uint spellId, uint itemId, TimeSpan cooldownDuration)
        {
            DateTime now = DateTime.Now;
            AddCooldown(spellId, itemId, now + cooldownDuration, 0, now);
        }

        public void AddCooldown(uint spellId, uint itemId, DateTime cooldownEnd, uint categoryId, DateTime categoryEnd, bool onHold = false)
        {
            CooldownEntry cooldownEntry = new CooldownEntry();
            cooldownEntry.SpellId = spellId;
            cooldownEntry.CooldownEnd = cooldownEnd;
            cooldownEntry.ItemId = itemId;
            cooldownEntry.CategoryId = categoryId;
            cooldownEntry.CategoryEnd = categoryEnd;
            cooldownEntry.OnHold = onHold;
            _spellCooldowns[spellId] = cooldownEntry;

            if (categoryId != 0)
                _categoryCooldowns[categoryId] = cooldownEntry;
        }

        public void ModifyCooldown(uint spellId, int cooldownModMs)
        {
            TimeSpan offset = TimeSpan.FromMilliseconds(cooldownModMs);
            ModifyCooldown(spellId, offset);
        }

        public void ModifyCooldown(uint spellId, TimeSpan offset)
        {
            var cooldownEntry = _spellCooldowns.LookupByKey(spellId);
            if (offset.TotalMilliseconds == 0 || cooldownEntry == null)
                return;

            DateTime now = DateTime.Now;

            if (cooldownEntry.CooldownEnd + offset > now)
                cooldownEntry.CooldownEnd += offset;
            else
            {
                _categoryCooldowns.Remove(cooldownEntry.CategoryId);
                _spellCooldowns.Remove(spellId);
            }

            Player playerOwner = GetPlayerOwner();
            if (playerOwner)
            {
                ModifyCooldown modifyCooldown = new ModifyCooldown();
                modifyCooldown.IsPet = _owner != playerOwner;
                modifyCooldown.SpellID = spellId;
                modifyCooldown.DeltaTime = (int)offset.TotalMilliseconds;
                playerOwner.SendPacket(modifyCooldown);
            }
        }

        public void ResetCooldown(uint spellId, bool update = false)
        {
            var entry = _spellCooldowns.LookupByKey(spellId);
            if (entry == null)
                return;

            if (update)
            {
                Player playerOwner = GetPlayerOwner();
                if (playerOwner)
                {
                    ClearCooldown clearCooldown = new ClearCooldown();
                    clearCooldown.IsPet = _owner != playerOwner;
                    clearCooldown.SpellID = spellId;
                    clearCooldown.ClearOnHold = false;
                    playerOwner.SendPacket(clearCooldown);
                }
            }

            _categoryCooldowns.Remove(entry.CategoryId);
            _spellCooldowns.Remove(spellId);
        }

        public void ResetCooldowns(Func<KeyValuePair<uint, CooldownEntry>, bool> predicate, bool update = false)
        {
            List<uint> resetCooldowns = new List<uint>();
            foreach (var pair in _spellCooldowns)
            {
                if (predicate(pair))
                {
                    resetCooldowns.Add(pair.Key);
                    ResetCooldown(pair.Key, false);
                }
            }

            if (update && !resetCooldowns.Empty())
                SendClearCooldowns(resetCooldowns);
        }

        public void ResetAllCooldowns()
        {
            Player playerOwner = GetPlayerOwner();
            if (playerOwner)
            {
                List<uint> cooldowns = new List<uint>();
                foreach (var id in _spellCooldowns.Keys)
                    cooldowns.Add(id);

                SendClearCooldowns(cooldowns);
            }

            _categoryCooldowns.Clear();
            _spellCooldowns.Clear();
        }

        public bool HasCooldown(uint spellId, uint itemId = 0, bool ignoreCategoryCooldown = false)
        {
            return HasCooldown(Global.SpellMgr.GetSpellInfo(spellId), itemId, ignoreCategoryCooldown);
        }

        public bool HasCooldown(SpellInfo spellInfo, uint itemId = 0, bool ignoreCategoryCooldown = false)
        {
            if (_spellCooldowns.ContainsKey(spellInfo.Id))
                return true;

            if (ignoreCategoryCooldown)
                return false;

            uint category = 0;
            GetCooldownDurations(spellInfo, itemId, ref category);

            if (category == 0)
                category = spellInfo.GetCategory();

            if (category == 0)
                return false;

            return _categoryCooldowns.ContainsKey(category);
        }

        public uint GetRemainingCooldown(SpellInfo spellInfo)
        {
            DateTime end;
            var entry = _spellCooldowns.LookupByKey(spellInfo.Id);
            if (entry != null)
                end = entry.CooldownEnd;
            else
            {
                var cooldownEntry = _categoryCooldowns.LookupByKey(spellInfo.GetCategory());
                if (cooldownEntry == null)
                    return 0;

                end = cooldownEntry.CategoryEnd;
            }

            DateTime now = DateTime.Now;
            if (end < now)
                return 0;

            var remaining = end - now;
            return (uint)remaining.TotalMilliseconds;
        }

        public void LockSpellSchool(SpellSchoolMask schoolMask, uint lockoutTime)
        {
            DateTime now = DateTime.Now;
            DateTime lockoutEnd = now + TimeSpan.FromMilliseconds(lockoutTime);
            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean((SpellSchoolMask)(1 << i) & schoolMask))
                    _schoolLockouts[i] = lockoutEnd;

            List<uint> knownSpells = new List<uint>();
            Player plrOwner = _owner.ToPlayer();
            if (plrOwner)
            {
                foreach (var p in plrOwner.GetSpellMap())
                    if (p.Value.State != PlayerSpellState.Removed)
                        knownSpells.Add(p.Key);
            }
            else if (_owner.IsPet())
            {
                Pet petOwner = _owner.ToPet();
                foreach (var p in petOwner.m_spells)
                    if (p.Value.state != PetSpellState.Removed)
                        knownSpells.Add(p.Key);
            }
            else
            {
                Creature creatureOwner = _owner.ToCreature();
                for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                    if (creatureOwner.m_spells[i] != 0)
                        knownSpells.Add(creatureOwner.m_spells[i]);
            }

            SpellCooldownPkt spellCooldown = new SpellCooldownPkt();
            spellCooldown.Caster = _owner.GetGUID();
            spellCooldown.Flags = SpellCooldownFlags.None;
            foreach (uint spellId in knownSpells)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
                if (spellInfo.IsCooldownStartedOnEvent())
                    continue;

                if (!spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                    continue;

                if (Convert.ToBoolean(schoolMask & spellInfo.GetSchoolMask()) && GetRemainingCooldown(spellInfo) < lockoutTime)
                {
                    spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellId, lockoutTime));
                    AddCooldown(spellId, 0, lockoutEnd, 0, now);
                }
            }

            Player player = GetPlayerOwner();
            if (player)
                if (!spellCooldown.SpellCooldowns.Empty())
                    player.SendPacket(spellCooldown);
        }

        public bool IsSchoolLocked(SpellSchoolMask schoolMask)
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean((SpellSchoolMask)(1 << i) & schoolMask))
                    if (_schoolLockouts[i] > now)
                        return true;

            return false;
        }

        public bool ConsumeCharge(uint chargeCategoryId)
        {
            if (!CliDB.SpellCategoryStorage.ContainsKey(chargeCategoryId))
                return false;

            int chargeRecovery = GetChargeRecoveryTime(chargeCategoryId);
            if (chargeRecovery > 0 && GetMaxCharges(chargeCategoryId) > 0)
            {
                DateTime recoveryStart;
                var charges = _categoryCharges.LookupByKey(chargeCategoryId);
                if (charges.Empty())
                    recoveryStart = DateTime.Now;
                else
                    recoveryStart = charges.Last().RechargeEnd;

                _categoryCharges.Add(chargeCategoryId, new ChargeEntry(recoveryStart, TimeSpan.FromMilliseconds(chargeRecovery)));
                return true;
            }

            return false;
        }

        public void RestoreCharge(uint chargeCategoryId)
        {
            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            if (!chargeList.Empty())
            {
                chargeList.RemoveAt(chargeList.Count - 1);

                Player player = GetPlayerOwner();
                if (player)
                {
                    SetSpellCharges setSpellCharges = new SetSpellCharges();
                    setSpellCharges.Category = chargeCategoryId;
                    if (!chargeList.Empty())
                        setSpellCharges.NextRecoveryTime = (uint)(chargeList.FirstOrDefault().RechargeEnd - DateTime.Now).TotalMilliseconds;
                    setSpellCharges.ConsumedCharges = (byte)chargeList.Count;
                    setSpellCharges.IsPet = player != _owner;

                    player.SendPacket(setSpellCharges);
                }
            }
        }

        public void ResetCharges(uint chargeCategoryId)
        {
            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            if (!chargeList.Empty())
            {
                _categoryCharges.Remove(chargeCategoryId);

                Player player = GetPlayerOwner();
                if (player)
                {
                    ClearSpellCharges clearSpellCharges = new ClearSpellCharges();
                    clearSpellCharges.IsPet = _owner != player;
                    clearSpellCharges.Category = chargeCategoryId;
                    player.SendPacket(clearSpellCharges);
                }
            }
        }

        public void ResetAllCharges()
        {
            _categoryCharges.Clear();

            Player player = GetPlayerOwner();
            if (player)
            {
                ClearAllSpellCharges clearAllSpellCharges = new ClearAllSpellCharges();
                clearAllSpellCharges.IsPet = _owner != player;
                player.SendPacket(clearAllSpellCharges);
            }
        }

        public bool HasCharge(uint chargeCategoryId)
        {
            if (!CliDB.SpellCategoryStorage.ContainsKey(chargeCategoryId))
                return true;

            // Check if the spell is currently using charges (untalented warlock Dark Soul)
            int maxCharges = GetMaxCharges(chargeCategoryId);
            if (maxCharges <= 0)
                return true;

            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            return chargeList.Empty() || chargeList.Count < maxCharges;
        }

        public int GetMaxCharges(uint chargeCategoryId)
        {
            SpellCategoryRecord chargeCategoryEntry = CliDB.SpellCategoryStorage.LookupByKey(chargeCategoryId);
            if (chargeCategoryEntry == null)
                return 0;

            uint charges = chargeCategoryEntry.MaxCharges;
            charges += (uint)_owner.GetTotalAuraModifierByMiscValue(AuraType.ModMaxCharges, (int)chargeCategoryId);
            return (int)charges;
        }

        public int GetChargeRecoveryTime(uint chargeCategoryId)
        {
            SpellCategoryRecord chargeCategoryEntry = CliDB.SpellCategoryStorage.LookupByKey(chargeCategoryId);
            if (chargeCategoryEntry == null)
                return 0;

            int recoveryTime = chargeCategoryEntry.ChargeRecoveryTime;
            recoveryTime += _owner.GetTotalAuraModifierByMiscValue(AuraType.ChargeRecoveryMod, (int)chargeCategoryId);

            float recoveryTimeF = recoveryTime;
            recoveryTimeF *= _owner.GetTotalAuraMultiplierByMiscValue(AuraType.ChargeRecoveryMultiplier, (int)chargeCategoryId);

            if (_owner.HasAuraType(AuraType.ChargeRecoveryAffectedByHaste))
                recoveryTimeF *= _owner.GetFloatValue(UnitFields.ModCastHaste);

            if (_owner.HasAuraType(AuraType.ChargeRecoveryAffectedByHasteRegen))
                recoveryTimeF *= _owner.GetFloatValue(UnitFields.ModHasteRegen);

            return (int)Math.Floor(recoveryTimeF);
        }

        public bool HasGlobalCooldown(SpellInfo spellInfo)
        {
            return _globalCooldowns.ContainsKey(spellInfo.StartRecoveryCategory) && _globalCooldowns[spellInfo.StartRecoveryCategory] > DateTime.Now;
        }

        public void AddGlobalCooldown(SpellInfo spellInfo, uint duration)
        {
            _globalCooldowns[spellInfo.StartRecoveryCategory] = DateTime.Now + TimeSpan.FromMilliseconds(duration);
        }

        public void CancelGlobalCooldown(SpellInfo spellInfo)
        {
            _globalCooldowns[spellInfo.StartRecoveryCategory] = new DateTime();
        }

        public Player GetPlayerOwner()
        {
            return _owner.GetCharmerOrOwnerPlayerOrPlayerItself();
        }

        public void SendClearCooldowns(List<uint> cooldowns)
        {
            Player playerOwner = GetPlayerOwner();
            if (playerOwner)
            {
                ClearCooldowns clearCooldowns = new ClearCooldowns();
                clearCooldowns.IsPet = _owner != playerOwner;
                clearCooldowns.SpellID = cooldowns;
                playerOwner.SendPacket(clearCooldowns);
            }
        }

        void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref uint categoryId)
        {
            int notUsed = 0;
            GetCooldownDurations(spellInfo, itemId, ref notUsed, ref categoryId, ref notUsed);
        }

        void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref int cooldown, ref uint categoryId, ref int categoryCooldown)
        {
            int tmpCooldown = -1;
            uint tmpCategoryId = 0;
            int tmpCategoryCooldown = -1;

            // cooldown information stored in ItemEffect.db2, overriding normal cooldown and category
            if (itemId != 0)
            {
                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);
                if (proto != null)
                {
                    foreach (ItemEffectRecord itemEffect in proto.Effects)
                    {
                        if (itemEffect.SpellID == spellInfo.Id)
                        {
                            tmpCooldown = itemEffect.CoolDownMSec;
                            tmpCategoryId = itemEffect.SpellCategoryID;
                            tmpCategoryCooldown = itemEffect.CategoryCoolDownMSec;
                            break;
                        }
                    }
                }
            }

            // if no cooldown found above then base at DBC data
            if (tmpCooldown < 0 && tmpCategoryCooldown < 0)
            {
                tmpCooldown = (int)spellInfo.RecoveryTime;
                tmpCategoryId = spellInfo.GetCategory();
                tmpCategoryCooldown = (int)spellInfo.CategoryRecoveryTime;
            }

            cooldown = tmpCooldown;
            categoryId = tmpCategoryId;
            categoryCooldown = tmpCategoryCooldown;
        }

        public void SaveCooldownStateBeforeDuel()
        {
            _spellCooldownsBeforeDuel = _spellCooldowns;
        }

        public void RestoreCooldownStateAfterDuel()
        {
            Player player = _owner.ToPlayer();
            if (player)
            {
                // add all profession CDs created while in duel (if any)
                foreach (var c in _spellCooldowns)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(c.Key);

                    if (spellInfo.RecoveryTime > 10 * Time.Minute * Time.InMilliseconds || spellInfo.CategoryRecoveryTime > 10 * Time.Minute * Time.InMilliseconds)
                        _spellCooldownsBeforeDuel[c.Key] = _spellCooldowns[c.Key];
                }

                // check for spell with onHold active before and during the duel
                foreach (var pair in _spellCooldownsBeforeDuel)
                {
                    if (!pair.Value.OnHold && _spellCooldowns.ContainsKey(pair.Key) && !_spellCooldowns[pair.Key].OnHold)
                        _spellCooldowns[pair.Key] = _spellCooldownsBeforeDuel[pair.Key];
                }

                // update the client: restore old cooldowns
                SpellCooldownPkt spellCooldown = new SpellCooldownPkt();
                spellCooldown.Caster = _owner.GetGUID();
                spellCooldown.Flags = SpellCooldownFlags.IncludeEventCooldowns;

                foreach (var c in _spellCooldowns)
                {
                    DateTime now = DateTime.Now;
                    uint cooldownDuration = c.Value.CooldownEnd > now ? (uint)(c.Value.CooldownEnd - now).TotalMilliseconds : 0;

                    // cooldownDuration must be between 0 and 10 minutes in order to avoid any visual bugs
                    if (cooldownDuration <= 0 || cooldownDuration > 10 * Time.Minute * Time.InMilliseconds || c.Value.OnHold)
                        continue;

                    spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(c.Key, cooldownDuration));
                }

                player.SendPacket(spellCooldown);
            }
        }

        Unit _owner;
        Dictionary<uint, CooldownEntry> _spellCooldowns = new Dictionary<uint, CooldownEntry>();
        Dictionary<uint, CooldownEntry> _spellCooldownsBeforeDuel = new Dictionary<uint, CooldownEntry>();
        Dictionary<uint, CooldownEntry> _categoryCooldowns = new Dictionary<uint, CooldownEntry>();
        DateTime[] _schoolLockouts = new DateTime[(int)SpellSchools.Max];
        MultiMap<uint, ChargeEntry> _categoryCharges = new MultiMap<uint, ChargeEntry>();
        Dictionary<uint, DateTime> _globalCooldowns = new Dictionary<uint, DateTime>();

        public class CooldownEntry
        {
            public uint SpellId;
            public DateTime CooldownEnd;
            public uint ItemId;
            public uint CategoryId;
            public DateTime CategoryEnd;
            public bool OnHold;
        }

        public struct ChargeEntry
        {
            public ChargeEntry(DateTime startTime, TimeSpan rechargeTime)
            {
                RechargeStart = startTime;
                RechargeEnd = startTime + rechargeTime;
            }
            public ChargeEntry(DateTime startTime, DateTime endTime)
            {
                RechargeStart = startTime;
                RechargeEnd = endTime;
            }

            public DateTime RechargeStart;
            public DateTime RechargeEnd;
        }
    }
}
