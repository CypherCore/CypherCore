// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
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
                    CooldownEntry cooldownEntry = new();
                    cooldownEntry.SpellId = cooldownsResult.Read<uint>(0);
                    if (!Global.SpellMgr.HasSpellInfo(cooldownEntry.SpellId, Difficulty.None))
                        continue;

                    if (typeof(T) == typeof(Pet))
                    {
                        cooldownEntry.CooldownEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(1));
                        cooldownEntry.ItemId = 0;
                        cooldownEntry.CategoryId = cooldownsResult.Read<uint>(2);
                        cooldownEntry.CategoryEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(3));
                    }
                    else
                    {
                        cooldownEntry.CooldownEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(2));
                        cooldownEntry.ItemId = cooldownsResult.Read<uint>(1);
                        cooldownEntry.CategoryId = cooldownsResult.Read<uint>(3);
                        cooldownEntry.CategoryEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(4));
                    }

                    _spellCooldowns[cooldownEntry.SpellId] = cooldownEntry;
                    if (cooldownEntry.CategoryId != 0)
                        _categoryCooldowns[cooldownEntry.CategoryId] = _spellCooldowns[cooldownEntry.SpellId];

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
                    charges.RechargeStart = Time.UnixTimeToDateTime(chargesResult.Read<long>(1));
                    charges.RechargeEnd = Time.UnixTimeToDateTime(chargesResult.Read<long>(2));
                    _categoryCharges.Add(categoryId, charges);

                } while (chargesResult.NextRow());
            }
        }

        public void SaveToDB<T>(SQLTransaction trans) where T : WorldObject
        {
            PreparedStatement stmt;
            if (typeof(T) == typeof(Pet))
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
                stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
                stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                byte index;
                foreach (var pair in _spellCooldowns)
                {
                    if (!pair.Value.OnHold)
                    {
                        index = 0;
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL_COOLDOWN);
                        stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                        stmt.AddValue(index++, pair.Key);
                        stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                        stmt.AddValue(index++, pair.Value.CategoryId);
                        stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                        trans.Append(stmt);
                    }
                }

                foreach (var pair in _categoryCharges)
                {
                    index = 0;
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL_CHARGES);
                    stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                    trans.Append(stmt);
                }
            }
            else
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                stmt.AddValue(0, _owner.GetGUID().GetCounter());
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_CHARGES);
                stmt.AddValue(0, _owner.GetGUID().GetCounter());
                trans.Append(stmt);

                byte index;
                foreach (var pair in _spellCooldowns)
                {
                    if (!pair.Value.OnHold)
                    {
                        index = 0;
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_COOLDOWN);
                        stmt.AddValue(index++, _owner.GetGUID().GetCounter());
                        stmt.AddValue(index++, pair.Key);
                        stmt.AddValue(index++, pair.Value.ItemId);
                        stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                        stmt.AddValue(index++, pair.Value.CategoryId);
                        stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                        trans.Append(stmt);
                    }
                }

                foreach (var pair in _categoryCharges)
                {
                    index = 0;
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_CHARGES);
                    stmt.AddValue(index++, _owner.GetGUID().GetCounter());
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                    trans.Append(stmt);
                }
            }
        }

        public void Update()
        {
            DateTime now = GameTime.GetSystemTime();
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
            HandleCooldowns(spellInfo, item != null ? item.GetEntry() : 0u, spell);
        }

        public void HandleCooldowns(SpellInfo spellInfo, uint itemId, Spell spell = null)
        {
            if (spell != null && spell.IsIgnoringCooldowns())
                return;

            if (ConsumeCharge(spellInfo.ChargeCategoryId))
                return;

            Player player = _owner.ToPlayer();
            if (player != null)
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

            if (spellInfo.IsCooldownStartedOnEvent() || spellInfo.IsPassive())
                return;

            StartCooldown(spellInfo, itemId, spell);
        }

        public bool IsReady(SpellInfo spellInfo, uint itemId = 0)
        {
            if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                if (IsSchoolLocked(spellInfo.GetSchoolMask()))
                    return false;

            if (HasCooldown(spellInfo, itemId))
                return false;

            if (!HasCharge(spellInfo.ChargeCategoryId))
                return false;

            return true;
        }

        public void WritePacket(SendSpellHistory sendSpellHistory)
        {
            DateTime now = GameTime.GetSystemTime();
            foreach (var p in _spellCooldowns)
            {
                SpellHistoryEntry historyEntry = new();
                historyEntry.SpellID = p.Key;
                historyEntry.ItemID = p.Value.ItemId;

                if (p.Value.OnHold)
                    historyEntry.OnHold = true;
                else
                {
                    TimeSpan cooldownDuration = p.Value.CooldownEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    TimeSpan categoryDuration = p.Value.CategoryEnd - now;
                    if (categoryDuration.TotalMilliseconds > 0)
                    {
                        historyEntry.Category = p.Value.CategoryId;
                        historyEntry.CategoryRecoveryTime = (int)categoryDuration.TotalMilliseconds;
                    }

                    if (cooldownDuration > categoryDuration)
                        historyEntry.RecoveryTime = (int)cooldownDuration.TotalMilliseconds;
                }

                sendSpellHistory.Entries.Add(historyEntry);
            }
        }

        public void WritePacket(SendSpellCharges sendSpellCharges)
        {
            DateTime now = GameTime.GetSystemTime();
            foreach (var key in _categoryCharges.Keys)
            {
                var list = _categoryCharges[key];
                if (!list.Empty())
                {
                    TimeSpan cooldownDuration = list.FirstOrDefault().RechargeEnd - now;
                    if (cooldownDuration.TotalMilliseconds <= 0)
                        continue;

                    SpellChargeEntry chargeEntry = new();
                    chargeEntry.Category = key;
                    chargeEntry.NextRecoveryTime = (uint)cooldownDuration.TotalMilliseconds;
                    chargeEntry.ConsumedCharges = (byte)list.Count;
                    sendSpellCharges.Entries.Add(chargeEntry);
                }
            }
        }

        public void WritePacket(PetSpells petSpells)
        {
            DateTime now = GameTime.GetSystemTime();

            foreach (var pair in _spellCooldowns)
            {
                PetSpellCooldown petSpellCooldown = new();
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

                    PetSpellHistory petChargeEntry = new();
                    petChargeEntry.CategoryID = key;
                    petChargeEntry.RecoveryTime = (uint)cooldownDuration.TotalMilliseconds;
                    petChargeEntry.ConsumedCharges = (sbyte)list.Count;

                    petSpells.SpellHistory.Add(petChargeEntry);
                }
            }
        }

        public void StartCooldown(SpellInfo spellInfo, uint itemId, Spell spell = null, bool onHold = false, TimeSpan? forcedCooldown = null)
        {
            // init cooldown values
            uint categoryId = 0;
            TimeSpan cooldown = TimeSpan.Zero;
            TimeSpan categoryCooldown = TimeSpan.Zero;

            DateTime curTime = GameTime.GetSystemTime();
            DateTime catrecTime;
            DateTime recTime;
            bool needsCooldownPacket = false;

            if (!forcedCooldown.HasValue)
                GetCooldownDurations(spellInfo, itemId, ref cooldown, ref categoryId, ref categoryCooldown);
            else
                cooldown = forcedCooldown.Value;

            // overwrite time for selected category
            if (onHold)
            {
                // use +MONTH as infinite cooldown marker
                catrecTime = categoryCooldown > TimeSpan.Zero ? (curTime + PlayerConst.InfinityCooldownDelay) : curTime;
                recTime = cooldown > TimeSpan.Zero ? (curTime + PlayerConst.InfinityCooldownDelay) : catrecTime;
            }
            else
            {
                if (!forcedCooldown.HasValue)
                {
                    // Now we have cooldown data (if found any), time to apply mods
                    Player modOwner = _owner.GetSpellModOwner();
                    if (modOwner != null)
                    {
                        void applySpellMod(ref TimeSpan value)
                        {
                            int intValue = (int)value.TotalMilliseconds;
                            modOwner.ApplySpellMod(spellInfo, SpellModOp.Cooldown, ref intValue, spell);
                            value = TimeSpan.FromMilliseconds(intValue);
                        }

                        if (cooldown >= TimeSpan.Zero)
                            applySpellMod(ref cooldown);

                        if (categoryCooldown >= TimeSpan.Zero && !spellInfo.HasAttribute(SpellAttr6.NoCategoryCooldownMods))
                            applySpellMod(ref categoryCooldown);
                    }

                    if (_owner.HasAuraTypeWithAffectMask(AuraType.ModSpellCooldownByHaste, spellInfo))
                    {
                        cooldown = TimeSpan.FromMilliseconds(cooldown.TotalMilliseconds * _owner.m_unitData.ModSpellHaste);
                        categoryCooldown = TimeSpan.FromMilliseconds(categoryCooldown.TotalMilliseconds * _owner.m_unitData.ModSpellHaste);
                    }

                    if (_owner.HasAuraTypeWithAffectMask(AuraType.ModCooldownByHasteRegen, spellInfo))
                    {
                        cooldown = TimeSpan.FromMilliseconds(cooldown.TotalMilliseconds * _owner.m_unitData.ModHasteRegen);
                        categoryCooldown = TimeSpan.FromMilliseconds(categoryCooldown.TotalMilliseconds * _owner.m_unitData.ModHasteRegen);
                    }

                    int cooldownMod = _owner.GetTotalAuraModifier(AuraType.ModCooldown);
                    if (cooldownMod != 0)
                    {
                        // Apply SPELL_AURA_MOD_COOLDOWN only to own spells
                        Player playerOwner = GetPlayerOwner();
                        if (playerOwner == null || playerOwner.HasSpell(spellInfo.Id))
                        {
                            needsCooldownPacket = true;
                            cooldown += TimeSpan.FromMilliseconds(cooldownMod);   // SPELL_AURA_MOD_COOLDOWN does not affect category cooldows, verified with shaman shocks
                        }
                    }

                    // Apply SPELL_AURA_MOD_SPELL_CATEGORY_COOLDOWN modifiers
                    // Note: This aura applies its modifiers to all cooldowns of spells with set category, not to category cooldown only
                    if (categoryId != 0)
                    {
                        int categoryModifier = _owner.GetTotalAuraModifierByMiscValue(AuraType.ModSpellCategoryCooldown, (int)categoryId);
                        if (categoryModifier != 0)
                        {
                            if (cooldown > TimeSpan.Zero)
                                cooldown += TimeSpan.FromMilliseconds(categoryModifier);

                            if (categoryCooldown > TimeSpan.Zero)
                                categoryCooldown += TimeSpan.FromMilliseconds(categoryModifier);
                        }

                        SpellCategoryRecord categoryEntry = CliDB.SpellCategoryStorage.LookupByKey(categoryId);
                        if (categoryEntry.Flags.HasAnyFlag(SpellCategoryFlags.CooldownExpiresAtDailyReset))
                            categoryCooldown = Time.UnixTimeToDateTime(Global.WorldMgr.GetNextDailyQuestsResetTime()) - GameTime.GetSystemTime();
                    }
                }
                else
                    needsCooldownPacket = true;

                // replace negative cooldowns by 0
                if (cooldown < TimeSpan.Zero)
                    cooldown = TimeSpan.Zero;

                if (categoryCooldown < TimeSpan.Zero)
                    categoryCooldown = TimeSpan.Zero;

                // no cooldown after applying spell mods
                if (cooldown == TimeSpan.Zero && categoryCooldown == TimeSpan.Zero)
                    return;

                catrecTime = categoryCooldown != TimeSpan.Zero ? curTime + categoryCooldown : curTime;
                recTime = cooldown != TimeSpan.Zero ? curTime + cooldown : catrecTime;
            }

            // self spell cooldown
            if (recTime != curTime)
            {
                AddCooldown(spellInfo.Id, itemId, recTime, categoryId, catrecTime, onHold);

                if (needsCooldownPacket)
                {
                    Player playerOwner = GetPlayerOwner();
                    if (playerOwner != null)
                    {
                        SpellCooldownPkt spellCooldown = new();
                        spellCooldown.Caster = _owner.GetGUID();
                        spellCooldown.Flags = SpellCooldownFlags.None;
                        spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellInfo.Id, (uint)cooldown.TotalMilliseconds));
                        playerOwner.SendPacket(spellCooldown);
                    }
                }
            }
        }

        public void SendCooldownEvent(SpellInfo spellInfo, uint itemId = 0, Spell spell = null, bool startCooldown = true)
        {
            Player player = GetPlayerOwner();
            if (player != null)
            {
                uint category = spellInfo.GetCategory();
                GetCooldownDurations(spellInfo, itemId, ref category);

                var categoryEntry = _categoryCooldowns.LookupByKey(category);
                if (categoryEntry != null && categoryEntry.SpellId != spellInfo.Id)
                {
                    player.SendPacket(new CooldownEvent(player != _owner, categoryEntry.SpellId));

                    if (startCooldown)
                        StartCooldown(Global.SpellMgr.GetSpellInfo(categoryEntry.SpellId, _owner.GetMap().GetDifficultyID()), itemId, spell);
                }

                player.SendPacket(new CooldownEvent(player != _owner, spellInfo.Id));
            }

            // start cooldowns at server side, if any
            if (startCooldown)
                StartCooldown(spellInfo, itemId, spell);
        }

        public void AddCooldown(uint spellId, uint itemId, TimeSpan cooldownDuration)
        {
            DateTime now = GameTime.GetSystemTime();
            AddCooldown(spellId, itemId, now + cooldownDuration, 0, now);
        }

        public void AddCooldown(uint spellId, uint itemId, DateTime cooldownEnd, uint categoryId, DateTime categoryEnd, bool onHold = false)
        {
            CooldownEntry cooldownEntry = new();
            // scripts can start multiple cooldowns for a given spell, only store the longest one
            if (cooldownEnd > cooldownEntry.CooldownEnd || categoryEnd > cooldownEntry.CategoryEnd || onHold)
            {
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
        }

        public void ModifySpellCooldown(uint spellId, TimeSpan cooldownMod, bool withoutCategoryCooldown)
        {
            var cooldownEntry = _spellCooldowns.LookupByKey(spellId);
            if (cooldownMod.TotalMilliseconds == 0 || cooldownEntry == null)
                return;

            ModifySpellCooldown(cooldownEntry, cooldownMod, withoutCategoryCooldown);
        }

        void ModifySpellCooldown(CooldownEntry cooldownEntry, TimeSpan cooldownMod, bool withoutCategoryCooldown)
        {
            DateTime now = GameTime.GetSystemTime();

            cooldownEntry.CooldownEnd += cooldownMod;

            if (cooldownEntry.CategoryId != 0)
            {
                if (!withoutCategoryCooldown)
                    cooldownEntry.CategoryEnd += cooldownMod;

                // Because category cooldown existence is tied to regular cooldown, we cannot allow a situation where regular cooldown is shorter than category
                if (cooldownEntry.CooldownEnd < cooldownEntry.CategoryEnd)
                    cooldownEntry.CooldownEnd = cooldownEntry.CategoryEnd;
            }

            Player playerOwner = GetPlayerOwner();
            if (playerOwner != null)
            {
                ModifyCooldown modifyCooldown = new();
                modifyCooldown.IsPet = _owner != playerOwner;
                modifyCooldown.SpellID = cooldownEntry.SpellId;
                modifyCooldown.DeltaTime = (int)cooldownMod.TotalMilliseconds;
                modifyCooldown.WithoutCategoryCooldown = withoutCategoryCooldown;
                playerOwner.SendPacket(modifyCooldown);
            }

            if (cooldownEntry.CooldownEnd <= now)
            {                
                _categoryCooldowns.Remove(cooldownEntry.CategoryId);
                _spellCooldowns.Remove(cooldownEntry.SpellId);
            }
        }

        public void ModifyCooldown(uint spellId, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, _owner.GetMap().GetDifficultyID());
            if (spellInfo != null)
                ModifyCooldown(spellInfo, cooldownMod, withoutCategoryCooldown);
        }

        public void ModifyCooldown(SpellInfo spellInfo, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
        {
            if (cooldownMod == TimeSpan.Zero)
                return;

            if (GetChargeRecoveryTime(spellInfo.ChargeCategoryId) > 0 && GetMaxCharges(spellInfo.ChargeCategoryId) > 0)
                ModifyChargeRecoveryTime(spellInfo.ChargeCategoryId, cooldownMod);
            else
                ModifySpellCooldown(spellInfo.Id, cooldownMod, withoutCategoryCooldown);
        }

        public void ModifyCoooldowns(Func<CooldownEntry, bool> predicate, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
        {
            foreach (var cooldownEntry in _spellCooldowns.Values.ToList())
            {
                if (predicate(cooldownEntry))
                    ModifySpellCooldown(cooldownEntry, cooldownMod, withoutCategoryCooldown);
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
                if (playerOwner != null)
                {
                    ClearCooldown clearCooldown = new();
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
            List<uint> resetCooldowns = new();
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
            if (playerOwner != null)
            {
                List<uint> cooldowns = new();
                foreach (var id in _spellCooldowns.Keys)
                    cooldowns.Add(id);

                SendClearCooldowns(cooldowns);
            }

            _categoryCooldowns.Clear();
            _spellCooldowns.Clear();
        }

        public bool HasCooldown(uint spellId, uint itemId = 0)
        {
            return HasCooldown(Global.SpellMgr.GetSpellInfo(spellId, _owner.GetMap().GetDifficultyID()), itemId);
        }

        public bool HasCooldown(SpellInfo spellInfo, uint itemId = 0)
        {
            if (_owner.HasAuraTypeWithAffectMask(AuraType.IgnoreSpellCooldown, spellInfo))
                return false;

            if (_spellCooldowns.ContainsKey(spellInfo.Id))
                return true;

            if (spellInfo.CooldownAuraSpellId != 0 && _owner.HasAura(spellInfo.CooldownAuraSpellId))
                return true;

            uint category = 0;
            GetCooldownDurations(spellInfo, itemId, ref category);

            if (category == 0)
                category = spellInfo.GetCategory();

            if (category == 0)
                return false;

            return _categoryCooldowns.ContainsKey(category);
        }

        public TimeSpan GetRemainingCooldown(SpellInfo spellInfo)
        {
            DateTime end;
            var entry = _spellCooldowns.LookupByKey(spellInfo.Id);
            if (entry != null)
                end = entry.CooldownEnd;
            else
            {
                var cooldownEntry = _categoryCooldowns.LookupByKey(spellInfo.GetCategory());
                if (cooldownEntry == null)
                    return TimeSpan.Zero;

                end = cooldownEntry.CategoryEnd;
            }

            DateTime now = GameTime.GetSystemTime();
            if (end < now)
                return TimeSpan.Zero;

            var remaining = end - now;
            return remaining;
        }

        public TimeSpan GetRemainingCategoryCooldown(uint categoryId)
        {
            DateTime end;
            var cooldownEntry = _categoryCooldowns.LookupByKey(categoryId);
            if (cooldownEntry == null)
                return TimeSpan.Zero;

            end = cooldownEntry.CategoryEnd;

            DateTime now = GameTime.GetSystemTime();
            if (end < now)
                return TimeSpan.Zero;

            TimeSpan remaining = end - now;
            return remaining;
        }

        public TimeSpan GetRemainingCategoryCooldown(SpellInfo spellInfo)
        {
            return GetRemainingCategoryCooldown(spellInfo.GetCategory());
        }
        
        public void LockSpellSchool(SpellSchoolMask schoolMask, TimeSpan lockoutTime)
        {
            DateTime now = GameTime.GetSystemTime();
            DateTime lockoutEnd = now + lockoutTime;
            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean((SpellSchoolMask)(1 << i) & schoolMask))
                    _schoolLockouts[i] = lockoutEnd;

            List<uint> knownSpells = new();
            Player plrOwner = _owner.ToPlayer();
            if (plrOwner != null)
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

            SpellCooldownPkt spellCooldown = new();
            spellCooldown.Caster = _owner.GetGUID();
            spellCooldown.Flags = SpellCooldownFlags.LossOfControlUi;
            foreach (uint spellId in knownSpells)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, _owner.GetMap().GetDifficultyID());
                if (spellInfo.IsCooldownStartedOnEvent())
                    continue;

                if (!spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                    continue;

                if ((schoolMask & spellInfo.GetSchoolMask()) == 0)
                    continue;

                if (GetRemainingCooldown(spellInfo) < lockoutTime)
                    AddCooldown(spellId, 0, lockoutEnd, 0, now);

                // always send cooldown, even if it will be shorter than already existing cooldown for LossOfControl UI
                spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellId, (uint)lockoutTime.TotalMilliseconds));
            }

            Player player = GetPlayerOwner();
            if (player != null)
                if (!spellCooldown.SpellCooldowns.Empty())
                    player.SendPacket(spellCooldown);
        }

        public bool IsSchoolLocked(SpellSchoolMask schoolMask)
        {
            DateTime now = GameTime.GetSystemTime();
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
                    recoveryStart = GameTime.GetSystemTime();
                else
                    recoveryStart = charges.Last().RechargeEnd;

                _categoryCharges.Add(chargeCategoryId, new ChargeEntry(recoveryStart, TimeSpan.FromMilliseconds(chargeRecovery)));
                return true;
            }

            return false;
        }

        void ModifyChargeRecoveryTime(uint chargeCategoryId, TimeSpan cooldownMod)
        {
            var chargeCategoryEntry = CliDB.SpellCategoryStorage.LookupByKey(chargeCategoryId);
            if (chargeCategoryEntry == null)
                return;

            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            if (chargeList == null || chargeList.Empty())
                return;

            var now = GameTime.GetSystemTime();

            for (var i = 0; i < chargeList.Count; ++i)
            {
                var entry = chargeList[i];
                entry.RechargeStart += cooldownMod;
                entry.RechargeEnd += cooldownMod;
            }

            while (!chargeList.Empty() && chargeList[0].RechargeEnd < now)
                chargeList.RemoveAt(0);

            SendSetSpellCharges(chargeCategoryId, chargeList);
        }

        public void RestoreCharge(uint chargeCategoryId)
        {
            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            if (!chargeList.Empty())
            {
                chargeList.RemoveAt(chargeList.Count - 1);

                SendSetSpellCharges(chargeCategoryId, chargeList);

                if (chargeList.Empty())
                    _categoryCharges.Remove(chargeCategoryId);
            }
        }

        public void ResetCharges(uint chargeCategoryId)
        {
            var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);
            if (!chargeList.Empty())
            {
                _categoryCharges.Remove(chargeCategoryId);

                Player player = GetPlayerOwner();
                if (player != null)
                {
                    ClearSpellCharges clearSpellCharges = new();
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
            if (player != null)
            {
                ClearAllSpellCharges clearAllSpellCharges = new();
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
                recoveryTimeF *= _owner.m_unitData.ModSpellHaste;

            if (_owner.HasAuraType(AuraType.ChargeRecoveryAffectedByHasteRegen))
                recoveryTimeF *= _owner.m_unitData.ModHasteRegen;

            return (int)Math.Floor(recoveryTimeF);
        }

        public bool HasGlobalCooldown(SpellInfo spellInfo)
        {
            return _globalCooldowns.ContainsKey(spellInfo.StartRecoveryCategory) && _globalCooldowns[spellInfo.StartRecoveryCategory] > GameTime.GetSystemTime();
        }

        public void AddGlobalCooldown(SpellInfo spellInfo, TimeSpan durationMs)
        {
            _globalCooldowns[spellInfo.StartRecoveryCategory] = GameTime.GetSystemTime() + durationMs;
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
            if (playerOwner != null)
            {
                ClearCooldowns clearCooldowns = new();
                clearCooldowns.IsPet = _owner != playerOwner;
                clearCooldowns.SpellID = cooldowns;
                playerOwner.SendPacket(clearCooldowns);
            }
        }

        void SendSetSpellCharges(uint chargeCategoryId, List<ChargeEntry> chargeCollection)
        {
            Player player = GetPlayerOwner();
            if (player != null)
            {
                SetSpellCharges setSpellCharges = new();
                setSpellCharges.Category = chargeCategoryId;
                if (!chargeCollection.Empty())
                    setSpellCharges.NextRecoveryTime = (uint)(chargeCollection[0].RechargeEnd - DateTime.Now).TotalMilliseconds;
                setSpellCharges.ConsumedCharges = (byte)chargeCollection.Count;
                setSpellCharges.IsPet = player != _owner;
                player.SendPacket(setSpellCharges);
            }
        }
        
        void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref uint categoryId)
        {
            TimeSpan notUsed = TimeSpan.Zero;
            GetCooldownDurations(spellInfo, itemId, ref notUsed, ref categoryId, ref notUsed);
        }

        void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref TimeSpan cooldown, ref uint categoryId, ref TimeSpan categoryCooldown)
        {
            TimeSpan tmpCooldown = TimeSpan.MinValue;
            uint tmpCategoryId = 0;
            TimeSpan tmpCategoryCooldown = TimeSpan.MinValue;

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
                            tmpCooldown = TimeSpan.FromMilliseconds(itemEffect.CoolDownMSec);
                            tmpCategoryId = itemEffect.SpellCategoryID;
                            tmpCategoryCooldown = TimeSpan.FromMilliseconds(itemEffect.CategoryCoolDownMSec);
                            break;
                        }
                    }
                }
            }

            // if no cooldown found above then base at DBC data
            if (tmpCooldown < TimeSpan.Zero && tmpCategoryCooldown < TimeSpan.Zero)
            {
                tmpCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);
                tmpCategoryId = spellInfo.GetCategory();
                tmpCategoryCooldown = TimeSpan.FromMilliseconds(spellInfo.CategoryRecoveryTime);
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
            if (player != null)
            {
                // add all profession CDs created while in duel (if any)
                foreach (var c in _spellCooldowns)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(c.Key, Difficulty.None);

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
                SpellCooldownPkt spellCooldown = new();
                spellCooldown.Caster = _owner.GetGUID();
                spellCooldown.Flags = SpellCooldownFlags.IncludeEventCooldowns;

                foreach (var c in _spellCooldowns)
                {
                    DateTime now = GameTime.GetSystemTime();
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
        Dictionary<uint, CooldownEntry> _spellCooldowns = new();
        Dictionary<uint, CooldownEntry> _spellCooldownsBeforeDuel = new();
        Dictionary<uint, CooldownEntry> _categoryCooldowns = new();
        DateTime[] _schoolLockouts = new DateTime[(int)SpellSchools.Max];
        MultiMap<uint, ChargeEntry> _categoryCharges = new();
        Dictionary<uint, DateTime> _globalCooldowns = new();

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

            public DateTime RechargeStart;
            public DateTime RechargeEnd;
        }
    }
}
