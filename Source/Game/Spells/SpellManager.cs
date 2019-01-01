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
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Movement;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Game.Entities
{
    public sealed class SpellManager : Singleton<SpellManager>
    {
        SpellManager()
        {
            Assembly currentAsm = Assembly.GetExecutingAssembly();
            foreach (var type in currentAsm.GetTypes())
            {
                foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    foreach (var auraEffect in methodInfo.GetCustomAttributes<AuraEffectHandlerAttribute>())
                    {
                        if (auraEffect == null)
                            continue;

                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length < 3)
                        {
                            Log.outError(LogFilter.ServerLoading, "Method: {0} has wrong parameter count: {1} Should be 3. Can't load AuraEffect.", methodInfo.Name, parameters.Length);
                            continue;
                        }

                        if (parameters[0].ParameterType != typeof(AuraApplication) || parameters[1].ParameterType != typeof(AuraEffectHandleModes) || parameters[2].ParameterType != typeof(bool))
                        {
                            Log.outError(LogFilter.ServerLoading, "Method: {0} has wrong parameter Types: ({1}, {2}, {3}) Should be (AuraApplication, AuraEffectHandleModes, Bool). Can't load AuraEffect.",
                                methodInfo.Name, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
                            continue;
                        }

                        if (AuraEffectHandlers.ContainsKey(auraEffect.auraType))
                        {
                            Log.outError(LogFilter.ServerLoading, "Tried to override AuraEffectHandler of {0} with {1} (AuraType {2}).", AuraEffectHandlers[auraEffect.auraType].ToString(), methodInfo.Name, auraEffect.auraType);
                            continue;
                        }

                        AuraEffectHandlers.Add(auraEffect.auraType, (AuraEffectHandler)methodInfo.CreateDelegate(typeof(AuraEffectHandler)));

                    }

                    foreach (var spellEffect in methodInfo.GetCustomAttributes<SpellEffectHandlerAttribute>())
                    {
                        if (spellEffect == null)
                            continue;

                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length < 1)
                        {
                            Log.outError(LogFilter.ServerLoading, "Method: {0} has wrong parameter count: {1} Should be 1. Can't load SpellEffect.", methodInfo.Name, parameters.Length);
                            continue;
                        }

                        if (parameters[0].ParameterType != typeof(uint))
                        {
                            Log.outError(LogFilter.ServerLoading, "Method: {0} has wrong parameter Types: ({1}) Should be (uint). Can't load SpellEffect.", methodInfo.Name, parameters[0].ParameterType);
                            continue;
                        }

                        if (SpellEffectsHandlers.ContainsKey(spellEffect.effectName))
                        {
                            Log.outError(LogFilter.ServerLoading, "Tried to override SpellEffectsHandler of {0} with {1} (EffectName {2}).", SpellEffectsHandlers[spellEffect.effectName].ToString(), methodInfo.Name, spellEffect.effectName);
                            continue;
                        }

                        SpellEffectsHandlers.Add(spellEffect.effectName, (SpellEffectHandler)methodInfo.CreateDelegate(typeof(SpellEffectHandler)));
                    }
                }
            }
        }

        public bool IsSpellValid(SpellInfo spellInfo, Player player = null, bool msg = true)
        {
            // not exist
            if (spellInfo == null)
                return false;

            bool need_check_reagents = false;

            // check effects
            foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
            {
                if (effect == null)
                    continue;

                switch (effect.Effect)
                {
                    case 0:
                        continue;

                    // craft spell for crafting non-existed item (break client recipes list show)
                    case SpellEffectName.CreateItem:
                    case SpellEffectName.CreateLoot:
                        {
                            if (effect.ItemType == 0)
                            {
                                // skip auto-loot crafting spells, its not need explicit item info (but have special fake items sometime)
                                if (!spellInfo.IsLootCrafting())
                                {
                                    if (msg)
                                    {
                                        if (player)
                                            player.SendSysMessage("Craft spell {0} not have create item entry.", spellInfo.Id);
                                        else
                                            Log.outError(LogFilter.Spells, "Craft spell {0} not have create item entry.", spellInfo.Id);
                                    }
                                    return false;
                                }

                            }
                            // also possible IsLootCrafting case but fake item must exist anyway
                            else if (Global.ObjectMgr.GetItemTemplate(effect.ItemType) == null)
                            {
                                if (msg)
                                {
                                    if (player)
                                        player.SendSysMessage("Craft spell {0} create not-exist in DB item (Entry: {1}) and then...", spellInfo.Id, effect.ItemType);
                                    else
                                        Log.outError(LogFilter.Spells, "Craft spell {0} create not-exist in DB item (Entry: {1}) and then...", spellInfo.Id, effect.ItemType);
                                }
                                return false;
                            }

                            need_check_reagents = true;
                            break;
                        }
                    case SpellEffectName.LearnSpell:
                        {
                            SpellInfo spellInfo2 = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell);
                            if (!IsSpellValid(spellInfo2, player, msg))
                            {
                                if (msg)
                                {
                                    if (player != null)
                                        player.SendSysMessage("Spell {0} learn to broken spell {1}, and then...", spellInfo.Id, effect.TriggerSpell);
                                    else
                                        Log.outError(LogFilter.Spells, "Spell {0} learn to invalid spell {1}, and then...", spellInfo.Id, effect.TriggerSpell);
                                }
                                return false;
                            }
                            break;
                        }
                }
            }

            if (need_check_reagents)
            {
                for (int j = 0; j < SpellConst.MaxReagents; ++j)
                {
                    if (spellInfo.Reagent[j] > 0 && Global.ObjectMgr.GetItemTemplate((uint)spellInfo.Reagent[j]) == null)
                    {
                        if (msg)
                        {
                            if (player != null)
                                player.SendSysMessage("Craft spell {0} have not-exist reagent in DB item (Entry: {1}) and then...", spellInfo.Id, spellInfo.Reagent[j]);
                            else
                                Log.outError(LogFilter.Spells, "Craft spell {0} have not-exist reagent in DB item (Entry: {1}) and then...", spellInfo.Id, spellInfo.Reagent[j]);
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        public SpellChainNode GetSpellChainNode(uint spell_id)
        {
            return mSpellChains.LookupByKey(spell_id);
        }

        public uint GetFirstSpellInChain(uint spell_id)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
                return node.first.Id;

            return spell_id;
        }

        public uint GetLastSpellInChain(uint spell_id)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
                return node.last.Id;

            return spell_id;
        }

        public uint GetNextSpellInChain(uint spell_id)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
                if (node.next != null)
                    return node.next.Id;

            return 0;
        }

        public uint GetPrevSpellInChain(uint spell_id)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
                if (node.prev != null)
                    return node.prev.Id;

            return 0;
        }

        public byte GetSpellRank(uint spell_id)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
                return node.rank;

            return 0;
        }

        public uint GetSpellWithRank(uint spell_id, uint rank, bool strict = false)
        {
            var node = GetSpellChainNode(spell_id);
            if (node != null)
            {
                if (rank != node.rank)
                    return GetSpellWithRank(node.rank < rank ? node.next.Id : node.prev.Id, rank, strict);
            }
            else if (strict && rank > 1)
                return 0;

            return spell_id;
        }

        public List<uint> GetSpellsRequiredForSpellBounds(uint spell_id)
        {
            return mSpellReq.LookupByKey(spell_id);
        }

        public List<uint> GetSpellsRequiringSpellBounds(uint spell_id)
        {
            return mSpellsReqSpell.LookupByKey(spell_id);
        }

        public bool IsSpellRequiringSpell(uint spellid, uint req_spellid)
        {
            var spellsRequiringSpell = GetSpellsRequiringSpellBounds(req_spellid);

            foreach (var spell in spellsRequiringSpell)
            {
                if (spell == spellid)
                    return true;
            }
            return false;
        }

        public SpellLearnSkillNode GetSpellLearnSkill(uint spell_id)
        {
            return mSpellLearnSkills.LookupByKey(spell_id);
        }

        public List<SpellLearnSpellNode> GetSpellLearnSpellMapBounds(uint spell_id)
        {
            return mSpellLearnSpells.LookupByKey(spell_id);
        }

        bool IsSpellLearnSpell(uint spell_id)
        {
            return mSpellLearnSpells.ContainsKey(spell_id);
        }

        public bool IsSpellLearnToSpell(uint spell_id1, uint spell_id2)
        {
            var bounds = GetSpellLearnSpellMapBounds(spell_id1);
            foreach (var bound in bounds)
                if (bound.Spell == spell_id2)
                    return true;
            return false;
        }

        public SpellTargetPosition GetSpellTargetPosition(uint spell_id, uint effIndex)
        {
            return mSpellTargetPositions.LookupByKey(new KeyValuePair<uint, uint>(spell_id, effIndex));
        }

        public List<SpellGroup> GetSpellSpellGroupMapBounds(uint spell_id)
        {
            return mSpellSpellGroup.LookupByKey(GetFirstSpellInChain(spell_id));
        }

        public bool IsSpellMemberOfSpellGroup(uint spellid, SpellGroup groupid)
        {
            var spellGroup = GetSpellSpellGroupMapBounds(spellid);
            foreach (var group in spellGroup)
            {
                if (group == groupid)
                    return true;
            }
            return false;
        }

        List<int> GetSpellGroupSpellMapBounds(SpellGroup group_id)
        {
            return mSpellGroupSpell.LookupByKey(group_id);
        }

        public void GetSetOfSpellsInSpellGroup(SpellGroup group_id, out List<int> foundSpells)
        {
            List<SpellGroup> usedGroups = new List<SpellGroup>();
            GetSetOfSpellsInSpellGroup(group_id, out foundSpells, ref usedGroups);
        }

        void GetSetOfSpellsInSpellGroup(SpellGroup group_id, out List<int> foundSpells, ref List<SpellGroup> usedGroups)
        {
            foundSpells = new List<int>();
            if (usedGroups.Find(p => p == group_id) == 0)
                return;

            usedGroups.Add(group_id);

            var groupSpell = GetSpellGroupSpellMapBounds(group_id);
            foreach (var group in groupSpell)
            {
                if (group < 0)
                {
                    SpellGroup currGroup = (SpellGroup)Math.Abs(group);
                    GetSetOfSpellsInSpellGroup(currGroup, out foundSpells, ref usedGroups);
                }
                else
                {
                    foundSpells.Add(group);
                }
            }
        }

        public bool AddSameEffectStackRuleSpellGroups(SpellInfo spellInfo, int amount, Dictionary<SpellGroup, int> groups)
        {
            uint spellId = spellInfo.GetFirstRankSpell().Id;
            var spellGroup = GetSpellSpellGroupMapBounds(spellId);
            // Find group with SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT if it belongs to one
            foreach (var group in spellGroup)
            {
                var found = mSpellGroupStack.FirstOrDefault(p => p.Key == group);
                if (found.Key != SpellGroup.None)
                {
                    if (found.Value == SpellGroupStackRule.ExclusiveSameEffect)
                    {
                        // Put the highest amount in the map
                        if (groups.FirstOrDefault(p => p.Key == group).Key == SpellGroup.None)
                            groups.Add(group, amount);
                        else
                        {
                            int curr_amount = groups[group];
                            // Take absolute value because this also counts for the highest negative aura
                            if (Math.Abs(curr_amount) < Math.Abs(amount))
                                groups[group] = amount;
                        }
                        // return because a spell should be in only one SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT group
                        return true;
                    }
                }
            }
            // Not in a SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT group, so return false
            return false;
        }

        public SpellGroupStackRule CheckSpellGroupStackRules(SpellInfo spellInfo1, SpellInfo spellInfo2)
        {
            uint spellid_1 = spellInfo1.GetFirstRankSpell().Id;
            uint spellid_2 = spellInfo2.GetFirstRankSpell().Id;
            if (spellid_1 == spellid_2)
                return SpellGroupStackRule.Default;
            // find SpellGroups which are common for both spells
            var spellGroup1 = GetSpellSpellGroupMapBounds(spellid_1);
            List<SpellGroup> groups = new List<SpellGroup>();
            foreach (var group in spellGroup1)
            {
                if (IsSpellMemberOfSpellGroup(spellid_2, group))
                {
                    bool add = true;
                    var groupSpell = GetSpellGroupSpellMapBounds(group);
                    foreach (var group2 in groupSpell)
                    {
                        if (group2 < 0)
                        {
                            SpellGroup currGroup = (SpellGroup)Math.Abs(group2);
                            if (IsSpellMemberOfSpellGroup(spellid_1, currGroup) && IsSpellMemberOfSpellGroup(spellid_2, currGroup))
                            {
                                add = false;
                                break;
                            }
                        }
                    }
                    if (add)
                        groups.Add(group);
                }
            }

            SpellGroupStackRule rule = SpellGroupStackRule.Default;

            foreach (var group in groups)
            {
                var found = mSpellGroupStack.LookupByKey(group);
                if (found != 0)
                    rule = found;
                if (rule != 0)
                    break;
            }
            return rule;
        }

        public SpellGroupStackRule GetSpellGroupStackRule(SpellGroup group)
        {
            if (mSpellGroupStack.ContainsKey(group))
                return mSpellGroupStack.LookupByKey(group);

            return SpellGroupStackRule.Default;
        }

        public SpellProcEntry GetSpellProcEntry(uint spellId)
        {
            return mSpellProcMap.LookupByKey(spellId);
        }

        public static bool CanSpellTriggerProcOnEvent(SpellProcEntry procEntry, ProcEventInfo eventInfo)
        {
            // proc type doesn't match
            if (!Convert.ToBoolean(eventInfo.GetTypeMask() & procEntry.ProcFlags))
                return false;

            // check XP or honor target requirement
            if (((uint)procEntry.AttributesMask & 0x0000001) != 0)
            {
                Player actor = eventInfo.GetActor().ToPlayer();
                if (actor)
                    if (eventInfo.GetActionTarget() && !actor.isHonorOrXPTarget(eventInfo.GetActionTarget()))
                        return false;
            }

            // check power requirement
            if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReqPowerCost))
            {
                if (!eventInfo.GetProcSpell())
                    return false;

                var costs = eventInfo.GetProcSpell().GetPowerCost();
                var m = costs.Find(cost => cost.Amount > 0);
                if (m == null)
                    return false;
            }

            // always trigger for these types
            if ((eventInfo.GetTypeMask() & (ProcFlags.Killed | ProcFlags.Kill | ProcFlags.Death)) != 0)
                return true;

            // Do not consider autoattacks as triggered spells
            if (!procEntry.AttributesMask.HasAnyFlag(ProcAttributes.TriggeredCanProc) && !eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.AutoAttackMask))
            {
                Spell spell = eventInfo.GetProcSpell();
                if (spell)
                {
                    if (spell.IsTriggered())
                    {
                        SpellInfo spellInfo = spell.GetSpellInfo();
                        if (!spellInfo.HasAttribute(SpellAttr3.TriggeredCanTriggerProc2) &&
                            !spellInfo.HasAttribute(SpellAttr2.TriggeredCanTriggerProc))
                            return false;
                    }
                }
            }

            // check school mask (if set) for other trigger types
            if (procEntry.SchoolMask != 0 && !Convert.ToBoolean(eventInfo.GetSchoolMask() & procEntry.SchoolMask))
                return false;

            // check spell family name/flags (if set) for spells
            if (eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.PeriodicMask | ProcFlags.SpellMask))
            {
                SpellInfo eventSpellInfo = eventInfo.GetSpellInfo();
                if (eventSpellInfo != null)
                    if (!eventSpellInfo.IsAffected(procEntry.SpellFamilyName, procEntry.SpellFamilyMask))
                        return false;
            }

            // check spell type mask (if set)
            if ((eventInfo.GetTypeMask() & (ProcFlags.SpellMask | ProcFlags.PeriodicMask)) != 0)
            {
                if (procEntry.SpellTypeMask != 0 && !Convert.ToBoolean(eventInfo.GetSpellTypeMask() & procEntry.SpellTypeMask))
                    return false;
            }

            // check spell phase mask
            if ((eventInfo.GetTypeMask() & ProcFlags.ReqSpellPhaseMask) != 0)
            {
                if (!Convert.ToBoolean(eventInfo.GetSpellPhaseMask() & procEntry.SpellPhaseMask))
                    return false;
            }

            // check hit mask (on taken hit or on done hit, but not on spell cast phase)
            if ((eventInfo.GetTypeMask() & ProcFlags.TakenHitMask) != 0 || ((eventInfo.GetTypeMask() & ProcFlags.DoneHitMask) != 0
                && !Convert.ToBoolean(eventInfo.GetSpellPhaseMask() & ProcFlagsSpellPhase.Cast)))
            {
                ProcFlagsHit hitMask = procEntry.HitMask;
                // get default values if hit mask not set
                if (hitMask == 0)
                {
                    // for taken procs allow normal + critical hits by default
                    if ((eventInfo.GetTypeMask() & ProcFlags.TakenHitMask) != 0)
                        hitMask |= ProcFlagsHit.Normal | ProcFlagsHit.Critical;
                    // for done procs allow normal + critical + absorbs by default
                    else
                        hitMask |= ProcFlagsHit.Normal | ProcFlagsHit.Critical | ProcFlagsHit.Absorb;
                }
                if (!Convert.ToBoolean(eventInfo.GetHitMask() & hitMask))
                    return false;
            }

            return true;
        }

        public SpellThreatEntry GetSpellThreatEntry(uint spellID)
        {
            var spellthreat = mSpellThreatMap.LookupByKey(spellID);
            if (spellthreat != null)
                return spellthreat;
            else
            {
                uint firstSpell = GetFirstSpellInChain(spellID);
                return mSpellThreatMap.LookupByKey(firstSpell);
            }
        }

        public List<SkillLineAbilityRecord> GetSkillLineAbilityMapBounds(uint spell_id)
        {
            return mSkillLineAbilityMap.LookupByKey(spell_id);
        }

        public PetAura GetPetAura(uint spell_id, byte eff)
        {
            return mSpellPetAuraMap.LookupByKey((spell_id << 8) + eff);
        }

        public SpellEnchantProcEntry GetSpellEnchantProcEvent(uint enchId)
        {
            return mSpellEnchantProcEventMap.LookupByKey(enchId);
        }

        public bool IsArenaAllowedEnchancment(uint ench_id)
        {
            return mEnchantCustomAttr.LookupByKey((int)ench_id);
        }

        public List<int> GetSpellLinked(int spell_id)
        {
            return mSpellLinkedMap.LookupByKey(spell_id);
        }

        public MultiMap<uint, uint> GetPetLevelupSpellList(CreatureFamily petFamily)
        {
            return mPetLevelupSpellMap.LookupByKey(petFamily);
        }

        public PetDefaultSpellsEntry GetPetDefaultSpellsEntry(int id)
        {
            return mPetDefaultSpellsMap.LookupByKey(id);
        }

        public List<SpellArea> GetSpellAreaMapBounds(uint spell_id)
        {
            return mSpellAreaMap.LookupByKey(spell_id);
        }

        public List<SpellArea> GetSpellAreaForQuestMapBounds(uint quest_id)
        {
            return mSpellAreaForQuestMap.LookupByKey(quest_id);
        }

        public List<SpellArea> GetSpellAreaForQuestEndMapBounds(uint quest_id)
        {
            return mSpellAreaForQuestEndMap.LookupByKey(quest_id);
        }

        public List<SpellArea> GetSpellAreaForAuraMapBounds(uint spell_id)
        {
            return mSpellAreaForAuraMap.LookupByKey(spell_id);
        }

        public List<SpellArea> GetSpellAreaForAreaMapBounds(uint area_id)
        {
            return mSpellAreaForAreaMap.LookupByKey(area_id);
        }

        public List<SpellArea> GetSpellAreaForQuestAreaMapBounds(uint area_id, uint quest_id)
        {
            return mSpellAreaForQuestAreaMap.LookupByKey(Tuple.Create(area_id, quest_id));
        }

        void UnloadSpellInfoChains()
        {
            foreach (var chain in mSpellChains)
                mSpellInfoMap[chain.Key].ChainEntry = null;

            mSpellChains.Clear();
        }

        #region Loads
        public void LoadSpellRanks()
        {
            uint oldMSTime = Time.GetMSTime();

            Dictionary<uint /*spell*/, uint /*next*/> chains = new Dictionary<uint, uint>();
            List<uint> hasPrev = new List<uint>();
            foreach (SkillLineAbilityRecord skillAbility in CliDB.SkillLineAbilityStorage.Values)
            {
                if (skillAbility.SupercedesSpell == 0)
                    continue;

                if (!HasSpellInfo(skillAbility.SupercedesSpell) || !HasSpellInfo(skillAbility.Spell))
                    continue;

                chains[skillAbility.SupercedesSpell] = skillAbility.Spell;
                hasPrev.Add(skillAbility.Spell);
            }

            // each key in chains that isn't present in hasPrev is a first rank
            foreach (var pair in chains)
            {
                if (hasPrev.Contains(pair.Key))
                    continue;

                SpellInfo first = GetSpellInfo(pair.Key);
                SpellInfo next = GetSpellInfo(pair.Value);

                if (!mSpellChains.ContainsKey(pair.Key))
                    mSpellChains[pair.Key] = new SpellChainNode();

                mSpellChains[pair.Key].first = first;
                mSpellChains[pair.Key].prev = null;
                mSpellChains[pair.Key].next = next;
                mSpellChains[pair.Key].last = next;
                mSpellChains[pair.Key].rank = 1;
                mSpellInfoMap[pair.Key].ChainEntry = mSpellChains[pair.Key];

                if (!mSpellChains.ContainsKey(pair.Value))
                    mSpellChains[pair.Value] = new SpellChainNode();

                mSpellChains[pair.Value].first = first;
                mSpellChains[pair.Value].prev = first;
                mSpellChains[pair.Value].next = null;
                mSpellChains[pair.Value].last = next;
                mSpellChains[pair.Value].rank = 2;
                mSpellInfoMap[pair.Value].ChainEntry = mSpellChains[pair.Value];

                byte rank = 3;
                var nextPair = chains.Find(pair.Value);
                while (nextPair.Key != 0)
                {
                    SpellInfo prev = GetSpellInfo(nextPair.Key); // already checked in previous iteration (or above, in case this is the first one)
                    SpellInfo last = GetSpellInfo(nextPair.Value);
                    if (last == null)
                        break;

                    if (!mSpellChains.ContainsKey(nextPair.Key))
                        mSpellChains[nextPair.Key] = new SpellChainNode();

                    mSpellChains[nextPair.Key].next = last;

                    if (!mSpellChains.ContainsKey(nextPair.Value))
                        mSpellChains[nextPair.Value] = new SpellChainNode();

                    mSpellChains[nextPair.Value].first = first;
                    mSpellChains[nextPair.Value].prev = prev;
                    mSpellChains[nextPair.Value].next = null;
                    mSpellChains[nextPair.Value].last = last;
                    mSpellChains[nextPair.Value].rank = rank++;
                    mSpellInfoMap[nextPair.Value].ChainEntry = mSpellChains[nextPair.Value];

                    // fill 'last'
                    do
                    {
                        mSpellChains[prev.Id].last = last;
                        prev = mSpellChains[prev.Id].prev;
                    } while (prev != null);

                    nextPair = chains.Find(nextPair.Value);
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell rank records in {1}ms", mSpellChains.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellRequired()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellsReqSpell.Clear();                                   // need for reload case
            mSpellReq.Clear();                                         // need for reload case

            //                                                   0        1
            SQLResult result = DB.World.Query("SELECT spell_id, req_spell from spell_required");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell required records. DB table `spell_required` is empty.");

                return;
            }

            uint count = 0;
            do
            {
                uint spell_id = result.Read<uint>(0);
                uint spell_req = result.Read<uint>(1);

                // check if chain is made with valid first spell
                SpellInfo spell = GetSpellInfo(spell_id);
                if (spell == null)
                {
                    Log.outError(LogFilter.Sql, "spell_id {0} in `spell_required` table is not found in dbcs, skipped", spell_id);
                    continue;
                }

                SpellInfo req_spell = GetSpellInfo(spell_req);
                if (req_spell == null)
                {
                    Log.outError(LogFilter.Sql, "req_spell {0} in `spell_required` table is not found in dbcs, skipped", spell_req);
                    continue;
                }

                if (spell.IsRankOf(req_spell))
                {
                    Log.outError(LogFilter.Sql, "req_spell {0} and spell_id {1} in `spell_required` table are ranks of the same spell, entry not needed, skipped", spell_req, spell_id);
                    continue;
                }

                if (IsSpellRequiringSpell(spell_id, spell_req))
                {
                    Log.outError(LogFilter.Sql, "duplicated entry of req_spell {0} and spell_id {1} in `spell_required`, skipped", spell_req, spell_id);
                    continue;
                }

                mSpellReq.Add(spell_id, spell_req);
                mSpellsReqSpell.Add(spell_req, spell_id);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell required records in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

        }

        public void LoadSpellLearnSkills()
        {
            mSpellLearnSkills.Clear();

            // search auto-learned skills and add its to map also for use in unlearn spells/talents
            uint dbc_count = 0;
            foreach (var spell in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in spell.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null)
                        continue;

                    SpellLearnSkillNode dbc_node = new SpellLearnSkillNode();
                    switch (effect.Effect)
                    {
                        case SpellEffectName.Skill:
                            dbc_node.skill = (SkillType)effect.MiscValue;
                            dbc_node.step = (ushort)effect.CalcValue();
                            if (dbc_node.skill != SkillType.Riding)
                                dbc_node.value = 1;
                            else
                                dbc_node.value = (ushort)(dbc_node.step * 75);
                            dbc_node.maxvalue = (ushort)(dbc_node.step * 75);
                            break;
                        case SpellEffectName.DualWield:
                            dbc_node.skill = SkillType.DualWield;
                            dbc_node.step = 1;
                            dbc_node.value = 1;
                            dbc_node.maxvalue = 1;
                            break;
                        default:
                            continue;
                    }

                    mSpellLearnSkills.Add(spell.Id, dbc_node);
                    ++dbc_count;
                    break;
                }
            }
            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Spell Learn Skills from DBC", dbc_count);
        }

        public void LoadSpellLearnSpells()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellLearnSpells.Clear();

            //                                         0      1        2
            SQLResult result = DB.World.Query("SELECT entry, SpellID, Active FROM spell_learn_spell");
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 spell learn spells. DB table `spell_learn_spell` is empty.");
                return;
            }
            uint count = 0;
            do
            {
                uint spell_id = result.Read<uint>(0);

                var node = new SpellLearnSpellNode();
                node.Spell = result.Read<uint>(1);
                node.OverridesSpell = 0;
                node.Active = result.Read<bool>(2);
                node.AutoLearned = false;

                SpellInfo spellInfo = GetSpellInfo(spell_id);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` does not exist", spell_id);
                    continue;
                }

                if (GetSpellInfo(node.Spell) == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` learning not existed spell {1}", spell_id, node.Spell);
                    continue;
                }

                if (spellInfo.HasAttribute(SpellCustomAttributes.IsTalent))
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` attempt learning talent spell {1}, skipped", spell_id, node.Spell);
                    continue;
                }

                mSpellLearnSpells.Add(spell_id, node);
                ++count;
            } while (result.NextRow());

            // search auto-learned spells and add its to map also for use in unlearn spells/talents
            uint dbc_count = 0;
            foreach (var entry in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in entry.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect != null && effect.Effect == SpellEffectName.LearnSpell)
                    {
                        var dbc_node = new SpellLearnSpellNode();
                        dbc_node.Spell = effect.TriggerSpell;
                        dbc_node.Active = true;                     // all dbc based learned spells is active (show in spell book or hide by client itself)
                        dbc_node.OverridesSpell = 0;

                        // ignore learning not existed spells (broken/outdated/or generic learnig spell 483
                        if (GetSpellInfo(dbc_node.Spell) == null)
                            continue;

                        // talent or passive spells or skill-step spells auto-cast and not need dependent learning,
                        // pet teaching spells must not be dependent learning (cast)
                        // other required explicit dependent learning
                        dbc_node.AutoLearned = effect.TargetA.GetTarget() == Targets.UnitPet || entry.HasAttribute(SpellCustomAttributes.IsTalent) || entry.IsPassive() || entry.HasEffect(SpellEffectName.SkillStep);

                        var db_node_bounds = GetSpellLearnSpellMapBounds(entry.Id);

                        bool found = false;
                        foreach (var bound in db_node_bounds)
                        {
                            if (bound.Spell == dbc_node.Spell)
                            {
                                Log.outError(LogFilter.Sql, "Spell {0} auto-learn spell {1} in spell.dbc then the record in `spell_learn_spell` is redundant, please fix DB.",
                                    entry.Id, dbc_node.Spell);
                                found = true;
                                break;
                            }
                        }

                        if (!found)                                  // add new spell-spell pair if not found
                        {
                            mSpellLearnSpells.Add(entry.Id, dbc_node);
                            ++dbc_count;
                        }
                    }
                }
            }

            foreach (var spellLearnSpell in CliDB.SpellLearnSpellStorage.Values)
            {
                if (GetSpellInfo(spellLearnSpell.SpellID) == null)
                    continue;

                var db_node_bounds = mSpellLearnSpells.LookupByKey(spellLearnSpell.LearnSpellID);
                bool found = false;
                foreach (var spellNode in db_node_bounds)
                {
                    if (spellNode.Spell == spellLearnSpell.SpellID)
                    {
                        Log.outError(LogFilter.Sql, "Found redundant record (entry: {0}, SpellID: {1}) in `spell_learn_spell`, spell added automatically from SpellLearnSpell.db2", spellLearnSpell.LearnSpellID, spellLearnSpell.SpellID);
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                // Check if it is already found in Spell.dbc, ignore silently if yes
                var dbc_node_bounds = GetSpellLearnSpellMapBounds(spellLearnSpell.LearnSpellID);
                found = false;
                foreach (var spellNode in dbc_node_bounds)
                {
                    if (spellNode.Spell == spellLearnSpell.SpellID)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                SpellLearnSpellNode dbcLearnNode = new SpellLearnSpellNode();
                dbcLearnNode.Spell = spellLearnSpell.SpellID;
                dbcLearnNode.OverridesSpell = spellLearnSpell.OverridesSpellID;
                dbcLearnNode.Active = true;
                dbcLearnNode.AutoLearned = false;

                mSpellLearnSpells.Add(spellLearnSpell.LearnSpellID, dbcLearnNode);
                ++dbc_count;
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell learn spells, {1} found in Spell.dbc in {2} ms", count, dbc_count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellTargetPositions()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellTargetPositions.Clear();                                // need for reload case

            //                                         0   1         2           3                  4                  5
            SQLResult result = DB.World.Query("SELECT ID, EffectIndex, MapID, PositionX, PositionY, PositionZ FROM spell_target_position");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell target coordinates. DB table `spell_target_position` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint spellId = result.Read<uint>(0);
                uint effIndex = result.Read<byte>(1);

                SpellTargetPosition st = new SpellTargetPosition();
                st.target_mapId = result.Read<uint>(2);
                st.target_X = result.Read<float>(3);
                st.target_Y = result.Read<float>(4);
                st.target_Z = result.Read<float>(5);

                var mapEntry = CliDB.MapStorage.LookupByKey(st.target_mapId);
                if (mapEntry == null)
                {
                    Log.outError(LogFilter.Sql, "Spell (ID: {0}, EffectIndex: {1}) is using a non-existant MapID (ID: {2})", spellId, effIndex, st.target_mapId);
                    continue;
                }

                if (st.target_X == 0 && st.target_Y == 0 && st.target_Z == 0)
                {
                    Log.outError(LogFilter.Sql, "Spell (ID: {0}, EffectIndex: {1}) target coordinates not provided.", spellId, effIndex);
                    continue;
                }

                SpellInfo spellInfo = GetSpellInfo(spellId);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Sql, "Spell (ID: {0}) listed in `spell_target_position` does not exist.", spellId);
                    continue;
                }

                SpellEffectInfo effect = spellInfo.GetEffect(effIndex);
                if (effect == null)
                {
                    Log.outError(LogFilter.Sql, "Spell (Id: {0}, effIndex: {1}) listed in `spell_target_position` does not have an effect at index {2}.", spellId, effIndex, effIndex);
                    continue;
                }

                // target facing is in degrees for 6484 & 9268... (blizz sucks)
                if (effect.PositionFacing > 2 * Math.PI)
                    st.target_Orientation = effect.PositionFacing * (float)Math.PI / 180;
                else
                    st.target_Orientation = effect.PositionFacing;

                if (effect.TargetA.GetTarget() == Targets.DestDb || effect.TargetB.GetTarget() == Targets.DestDb)
                {
                    var key = new KeyValuePair<uint, uint>(spellId, effIndex);
                    mSpellTargetPositions[key] = st;
                    ++count;
                }
                else
                {
                    Log.outError(LogFilter.Sql, "Spell (Id: {0}, effIndex: {1}) listed in `spell_target_position` does not have target TARGET_DEST_DB (17).", spellId, effIndex);
                    continue;
                }

            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell teleport coordinates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellGroups()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellSpellGroup.Clear();                                  // need for reload case
            mSpellGroupSpell.Clear();

            //                                                0     1
            SQLResult result = DB.World.Query("SELECT id, spell_id FROM spell_group");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell group definitions. DB table `spell_group` is empty.");
                return;
            }

            List<uint> groups = new List<uint>();
            uint count = 0;
            do
            {
                uint group_id = result.Read<uint>(0);
                if (group_id <= 1000 && group_id >= (uint)SpellGroup.CoreRangeMax)
                {
                    Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group` is in core range, but is not defined in core!", group_id);
                    continue;
                }
                int spell_id = result.Read<int>(1);

                groups.Add(group_id);
                mSpellGroupSpell.Add((SpellGroup)group_id, spell_id);

            } while (result.NextRow());

            foreach (var group in mSpellGroupSpell.KeyValueList)
            {
                if (group.Value < 0)
                {
                    if (!groups.Contains((uint)Math.Abs(group.Value)))
                    {
                        Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group` does not exist", Math.Abs(group.Value));
                        mSpellGroupSpell.Remove(group.Key);
                    }
                }
                else
                {
                    SpellInfo spellInfo = GetSpellInfo((uint)group.Value);

                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_group` does not exist", group.Value);
                        mSpellGroupSpell.Remove(group.Key);
                    }
                    else if (spellInfo.GetRank() > 1)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_group` is not first rank of spell", group.Value);
                        mSpellGroupSpell.Remove(group.Key);
                    }
                }
            }

            foreach (var group in groups)
            {
                List<int> spells;
                GetSetOfSpellsInSpellGroup((SpellGroup)group, out spells);

                foreach (var spell in spells)
                {
                    ++count;
                    mSpellSpellGroup.Add((uint)spell, (SpellGroup)group);
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell group definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellGroupStackRules()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellGroupStack.Clear();                                  // need for reload case

            //                                                       0         1
            SQLResult result = DB.World.Query("SELECT group_id, stack_rule FROM spell_group_stack_rules");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell group stack rules. DB table `spell_group_stack_rules` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint group_id = result.Read<uint>(0);
                byte stack_rule = result.Read<byte>(1);
                if (stack_rule >= (byte)SpellGroupStackRule.Max)
                {
                    Log.outError(LogFilter.Sql, "SpellGroupStackRule {0} listed in `spell_group_stack_rules` does not exist", stack_rule);
                    continue;
                }

                var spellGroup = GetSpellGroupSpellMapBounds((SpellGroup)group_id);

                if (spellGroup == null)
                {
                    Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group_stack_rules` does not exist", group_id);
                    continue;
                }

                mSpellGroupStack[(SpellGroup)group_id] = (SpellGroupStackRule)stack_rule;

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell group stack rules in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellProcs()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellProcMap.Clear();                             // need for reload case

            //                                            0        1           2                3                 4                 5                 6
            SQLResult result = DB.World.Query("SELECT SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, " +
            //           7              8               9       10              11              12      13        14      15
                "ProcFlags, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, ProcsPerMinute, Chance, Cooldown, Charges FROM spell_proc");

            uint count = 0;
            if (!result.IsEmpty())
            {
                do
                {
                    int spellId = result.Read<int>(0);

                    bool allRanks = false;
                    if (spellId < 0)
                    {
                        allRanks = true;
                        spellId = -spellId;
                    }

                    SpellInfo spellInfo = GetSpellInfo((uint)spellId);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` does not exist", spellId);
                        continue;
                    }

                    if (allRanks)
                    {
                        if (spellInfo.GetFirstRankSpell().Id != (uint)spellId)
                        {
                            Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` is not first rank of spell.", spellId);
                            continue;
                        }
                    }

                    SpellProcEntry baseProcEntry = new SpellProcEntry();

                    baseProcEntry.SchoolMask = (SpellSchoolMask)result.Read<uint>(1);
                    baseProcEntry.SpellFamilyName = (SpellFamilyNames)result.Read<uint>(2);
                    baseProcEntry.SpellFamilyMask = new FlagArray128(result.Read<uint>(3), result.Read<uint>(4), result.Read<uint>(5), result.Read<uint>(6));
                    baseProcEntry.ProcFlags = (ProcFlags)result.Read<uint>(7);
                    baseProcEntry.SpellTypeMask = (ProcFlagsSpellType)result.Read<uint>(8);
                    baseProcEntry.SpellPhaseMask = (ProcFlagsSpellPhase)result.Read<uint>(9);
                    baseProcEntry.HitMask = (ProcFlagsHit)result.Read<uint>(10);
                    baseProcEntry.AttributesMask = (ProcAttributes)result.Read<uint>(11);
                    baseProcEntry.ProcsPerMinute = result.Read<float>(12);
                    baseProcEntry.Chance = result.Read<float>(13);
                    baseProcEntry.Cooldown = result.Read<uint>(14);
                    baseProcEntry.Charges = result.Read<uint>(15);

                    while (spellInfo != null)
                    {
                        if (mSpellProcMap.ContainsKey(spellInfo.Id))
                        {
                            Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` has duplicate entry in the table", spellInfo.Id);
                            break;
                        }
                        SpellProcEntry procEntry = baseProcEntry;

                        // take defaults from dbcs
                        if (procEntry.ProcFlags == 0)
                            procEntry.ProcFlags = spellInfo.ProcFlags;
                        if (procEntry.Charges == 0)
                            procEntry.Charges = spellInfo.ProcCharges;
                        if (procEntry.Chance == 0 && procEntry.ProcsPerMinute == 0)
                            procEntry.Chance = spellInfo.ProcChance;
                        if (procEntry.Cooldown == 0)
                            procEntry.Cooldown = spellInfo.ProcCooldown;

                        // validate data
                        if (Convert.ToBoolean(procEntry.SchoolMask & ~SpellSchoolMask.All))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SchoolMask` set: {1}", spellInfo.Id, procEntry.SchoolMask);
                        if (procEntry.SpellFamilyName != 0 && ((int)procEntry.SpellFamilyName < 3 || (int)procEntry.SpellFamilyName > 17 || (int)procEntry.SpellFamilyName == 14 || (int)procEntry.SpellFamilyName == 16))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellFamilyName` set: {1}", spellInfo.Id, procEntry.SpellFamilyName);
                        if (procEntry.Chance < 0)
                        {
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has negative value in `Chance` field", spellInfo.Id);
                            procEntry.Chance = 0;
                        }
                        if (procEntry.ProcsPerMinute < 0)
                        {
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has negative value in `ProcsPerMinute` field", spellInfo.Id);
                            procEntry.ProcsPerMinute = 0;
                        }
                        if (procEntry.Chance == 0 && procEntry.ProcsPerMinute == 0)
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} doesn't have `Chance` and `ProcsPerMinute` values defined, proc will not be triggered", spellInfo.Id);
                        if (procEntry.ProcFlags == 0)
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} doesn't have `ProcFlags` value defined, proc will not be triggered", spellInfo.Id);
                        if (Convert.ToBoolean(procEntry.SpellTypeMask & ~ProcFlagsSpellType.MaskAll))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellTypeMask` set: {1}", spellInfo.Id, procEntry.SpellTypeMask);
                        if (procEntry.SpellTypeMask != 0 && !Convert.ToBoolean(procEntry.ProcFlags & (ProcFlags.SpellMask | ProcFlags.PeriodicMask)))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `SpellTypeMask` value defined, but it won't be used for defined `ProcFlags` value", spellInfo.Id);
                        if (procEntry.SpellPhaseMask == 0 && Convert.ToBoolean(procEntry.ProcFlags & ProcFlags.ReqSpellPhaseMask))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} doesn't have `SpellPhaseMask` value defined, but it's required for defined `ProcFlags` value, proc will not be triggered", spellInfo.Id);
                        if (Convert.ToBoolean(procEntry.SpellPhaseMask & ~ProcFlagsSpellPhase.MaskAll))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellPhaseMask` set: {1}", spellInfo.Id, procEntry.SpellPhaseMask);
                        if (procEntry.SpellPhaseMask != 0 && !Convert.ToBoolean(procEntry.ProcFlags & ProcFlags.ReqSpellPhaseMask))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `SpellPhaseMask` value defined, but it won't be used for defined `ProcFlags` value", spellInfo.Id);
                        if (Convert.ToBoolean(procEntry.HitMask & ~ProcFlagsHit.MaskAll))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `HitMask` set: {1}", spellInfo.Id, procEntry.HitMask);
                        if (procEntry.HitMask != 0 && !(Convert.ToBoolean(procEntry.ProcFlags & ProcFlags.TakenHitMask) || (Convert.ToBoolean(procEntry.ProcFlags & ProcFlags.DoneHitMask) && (procEntry.SpellPhaseMask == 0 || Convert.ToBoolean(procEntry.SpellPhaseMask & (ProcFlagsSpellPhase.Hit | ProcFlagsSpellPhase.Finish))))))
                            Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `HitMask` value defined, but it won't be used for defined `ProcFlags` and `SpellPhaseMask` values", spellInfo.Id);

                        mSpellProcMap.Add(spellInfo.Id, procEntry);
                        ++count;

                        if (allRanks)
                            spellInfo = spellInfo.GetNextRankSpell();
                        else
                            break;
                    }
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell proc conditions and data in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
            else
                Log.outInfo(LogFilter.ServerLoading, ">> Loaded 0 spell proc conditions and data. DB table `spell_proc` is empty.");

            // This generates default procs to retain compatibility with previous proc system
            Log.outInfo(LogFilter.ServerLoading, "Generating spell proc data from SpellMap...");
            count = 0;
            oldMSTime = Time.GetMSTime();

            foreach (SpellInfo spellInfo in mSpellInfoMap.Values)
            {
                if (spellInfo == null)
                    continue;

                // Data already present in DB, overwrites default proc
                if (mSpellProcMap.ContainsKey(spellInfo.Id))
                    continue;

                // Nothing to do if no flags set
                if (spellInfo.ProcFlags == 0)
                    continue;

                bool addTriggerFlag = false;
                ProcFlagsSpellType procSpellTypeMask = ProcFlagsSpellType.None;
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null || !effect.IsEffect())
                        continue;

                    AuraType auraName = effect.ApplyAuraName;
                    if (auraName == 0)
                        continue;

                    if (!isTriggerAura(auraName))
                        continue;

                    procSpellTypeMask |= getSpellTypeMask(auraName);
                    if (isAlwaysTriggeredAura(auraName))
                        addTriggerFlag = true;

                    // many proc auras with taken procFlag mask don't have attribute "can proc with triggered"
                    // they should proc nevertheless (example mage armor spells with judgement)
                    if (!addTriggerFlag && spellInfo.ProcFlags.HasAnyFlag(ProcFlags.TakenHitMask))
                    {
                        switch (auraName)
                        {
                            case AuraType.ProcTriggerSpell:
                            case AuraType.ProcTriggerDamage:
                                addTriggerFlag = true;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                }

                if (procSpellTypeMask == 0)
                {
                    foreach (SpellEffectInfo effectInfo in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                    {
                        if (effectInfo != null && effectInfo.IsAura())
                        {
                            Log.outError(LogFilter.Sql, $"Spell Id {spellInfo.Id} has DBC ProcFlags {spellInfo.ProcFlags}, but it's of non-proc aura type, it probably needs an entry in `spell_proc` table to be handled correctly.");
                            break;
                        }
                    }

                    continue;
                }

                SpellProcEntry procEntry = new SpellProcEntry();
                procEntry.SchoolMask = 0;
                procEntry.ProcFlags = spellInfo.ProcFlags;
                procEntry.SpellFamilyName = 0;
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                    if (effect != null && effect.IsEffect() && isTriggerAura(effect.ApplyAuraName))
                        procEntry.SpellFamilyMask |= effect.SpellClassMask;

                if (procEntry.SpellFamilyMask)
                    procEntry.SpellFamilyName = spellInfo.SpellFamilyName;

                procEntry.SpellTypeMask = procSpellTypeMask;
                procEntry.SpellPhaseMask = ProcFlagsSpellPhase.Hit;
                procEntry.HitMask = ProcFlagsHit.None; // uses default proc @see SpellMgr::CanSpellTriggerProcOnEvent

                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null || !effect.IsAura())
                        continue;

                    switch (effect.ApplyAuraName)
                    {
                        // Reflect auras should only proc off reflects
                        case AuraType.ReflectSpells:
                        case AuraType.ReflectSpellsSchool:
                            procEntry.HitMask = ProcFlagsHit.Reflect;
                            break;
                        // Only drop charge on crit
                        case AuraType.ModWeaponCritPercent:
                            procEntry.HitMask = ProcFlagsHit.Critical;
                            break;
                        // Only drop charge on block
                        case AuraType.ModBlockPercent:
                            procEntry.HitMask = ProcFlagsHit.Block;
                            break;
                        default:
                            continue;
                    }
                    break;
                }

                procEntry.AttributesMask = 0;
                if (spellInfo.ProcFlags.HasAnyFlag(ProcFlags.Kill))
                    procEntry.AttributesMask |= ProcAttributes.ReqExpOrHonor;
                if (addTriggerFlag)
                    procEntry.AttributesMask |= ProcAttributes.TriggeredCanProc;

                procEntry.ProcsPerMinute = 0;
                procEntry.Chance = spellInfo.ProcChance;
                procEntry.Cooldown = spellInfo.ProcCooldown;
                procEntry.Charges = spellInfo.ProcCharges;

                mSpellProcMap[spellInfo.Id] = procEntry;
                ++count;
            }

            Log.outInfo(LogFilter.ServerLoading, "Generated spell proc data for {0} spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellThreats()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellThreatMap.Clear();                                // need for reload case

            //                                           0      1        2       3
            SQLResult result = DB.World.Query("SELECT entry, flatMod, pctMod, apPctMod FROM spell_threat");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 aggro generating spells. DB table `spell_threat` is empty.");
                return;
            }
            uint count = 0;
            do
            {
                uint entry = result.Read<uint>(0);

                if (GetSpellInfo(entry) == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_threat` does not exist", entry);
                    continue;
                }

                SpellThreatEntry ste = new SpellThreatEntry();
                ste.flatMod = result.Read<int>(1);
                ste.pctMod = result.Read<float>(2);
                ste.apPctMod = result.Read<float>(3);

                mSpellThreatMap[entry] = ste;
                count++;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SpellThreatEntries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSkillLineAbilityMap()
        {
            uint oldMSTime = Time.GetMSTime();

            mSkillLineAbilityMap.Clear();

            foreach (var skill in CliDB.SkillLineAbilityStorage.Values)
                mSkillLineAbilityMap.Add(skill.Spell, skill);

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SkillLineAbility MultiMap Data in {1} ms", mSkillLineAbilityMap.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellPetAuras()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellPetAuraMap.Clear();                                  // need for reload case

            //                                                  0       1       2    3
            SQLResult result = DB.World.Query("SELECT spell, effectId, pet, aura FROM spell_pet_auras");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell pet auras. DB table `spell_pet_auras` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint spell = result.Read<uint>(0);
                byte eff = result.Read<byte>(1);
                uint pet = result.Read<uint>(2);
                uint aura = result.Read<uint>(3);

                var petAura = mSpellPetAuraMap.LookupByKey((spell << 8) + eff);
                if (petAura != null)
                    petAura.AddAura(pet, aura);
                else
                {
                    SpellInfo spellInfo = GetSpellInfo(spell);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_pet_auras` does not exist", spell);
                        continue;
                    }
                    SpellEffectInfo effect = spellInfo.GetEffect(eff);
                    if (effect == null)
                    {
                        Log.outError(LogFilter.Spells, "Spell {0} listed in `spell_pet_auras` does not have effect at index {1}", spell, eff);
                        continue;
                    }

                    if (effect.Effect != SpellEffectName.Dummy && (effect.Effect != SpellEffectName.ApplyAura || effect.ApplyAuraName != AuraType.Dummy))
                    {
                        Log.outError(LogFilter.Spells, "Spell {0} listed in `spell_pet_auras` does not have dummy aura or dummy effect", spell);
                        continue;
                    }

                    SpellInfo spellInfo2 = GetSpellInfo(aura);
                    if (spellInfo2 == null)
                    {
                        Log.outError(LogFilter.Sql, "Aura {0} listed in `spell_pet_auras` does not exist", aura);
                        continue;
                    }

                    PetAura pa = new PetAura(pet, aura, effect.TargetA.GetTarget() == Targets.UnitPet, effect.CalcValue());
                    mSpellPetAuraMap[(spell << 8) + eff] = pa;
                }
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell pet auras in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        // Fill custom data about enchancments
        public void LoadEnchantCustomAttr()
        {
            uint oldMSTime = Time.GetMSTime();

            uint count = 0;
            foreach (var spellInfo in mSpellInfoMap.Values)
            {
                // @todo find a better check
                if (!spellInfo.HasAttribute(SpellAttr2.PreserveEnchantInArena) || !spellInfo.HasAttribute(SpellAttr0.NotShapeshift))
                    continue;

                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect != null && effect.Effect == SpellEffectName.EnchantItemTemporary)
                    {
                        int enchId = effect.MiscValue;
                        var ench = CliDB.SpellItemEnchantmentStorage.LookupByKey((uint)enchId);
                        if (ench == null)
                            continue;
                        mEnchantCustomAttr[enchId] = true;
                        ++count;
                        break;
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} custom enchant attributes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellEnchantProcData()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellEnchantProcEventMap.Clear();                             // need for reload case

            //                                                  0         1           2         3
            SQLResult result = DB.World.Query("SELECT entry, customChance, PPMChance, procEx FROM spell_enchant_proc_data");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell enchant proc event conditions. DB table `spell_enchant_proc_data` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint enchantId = result.Read<uint>(0);

                var ench = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);
                if (ench == null)
                {
                    Log.outError(LogFilter.Sql, "Enchancment {0} listed in `spell_enchant_proc_data` does not exist", enchantId);
                    continue;
                }

                SpellEnchantProcEntry spe = new SpellEnchantProcEntry();
                spe.customChance = result.Read<uint>(1);
                spe.PPMChance = result.Read<float>(2);
                spe.procEx = result.Read<uint>(3);

                mSpellEnchantProcEventMap[enchantId] = spe;

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} enchant proc data definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellLinked()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellLinkedMap.Clear();    // need for reload case

            //                                                0              1             2
            SQLResult result = DB.World.Query("SELECT spell_trigger, spell_effect, type FROM spell_linked_spell");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 linked spells. DB table `spell_linked_spell` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                int trigger = result.Read<int>(0);
                int effect = result.Read<int>(1);
                int type = result.Read<int>(2);

                SpellInfo spellInfo = GetSpellInfo((uint)Math.Abs(trigger));
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_linked_spell` does not exist", Math.Abs(trigger));
                    continue;
                }
                spellInfo = GetSpellInfo((uint)Math.Abs(effect));
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_linked_spell` does not exist", Math.Abs(effect));
                    continue;
                }

                if (type != 0) //we will find a better way when more types are needed
                {
                    if (trigger > 0)
                        trigger += 200000 * type;
                    else
                        trigger -= 200000 * type;
                }
                mSpellLinkedMap.Add(trigger, effect);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} linked spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadPetLevelupSpellMap()
        {
            uint oldMSTime = Time.GetMSTime();

            mPetLevelupSpellMap.Clear();                                   // need for reload case

            uint count = 0;
            uint family_count = 0;

            foreach (var creatureFamily in CliDB.CreatureFamilyStorage.Values)
            {
                for (byte j = 0; j < 2; ++j)
                {
                    if (creatureFamily.SkillLine[j] == 0)
                        continue;

                    foreach (var skillLine in CliDB.SkillLineAbilityStorage.Values)
                    {
                        if (skillLine.SkillLine != creatureFamily.SkillLine[j])
                            continue;

                        if (skillLine.AcquireMethod != AbilityLearnType.OnSkillLearn)
                            continue;

                        SpellInfo spell = GetSpellInfo(skillLine.Spell);
                        if (spell == null) // not exist or triggered or talent
                            continue;

                        if (spell.SpellLevel == 0)
                            continue;

                        if (!mPetLevelupSpellMap.ContainsKey(creatureFamily.Id))
                            mPetLevelupSpellMap.Add(creatureFamily.Id, new MultiMap<uint, uint>());

                        var spellSet = mPetLevelupSpellMap.LookupByKey(creatureFamily.Id);
                        if (spellSet.Count == 0)
                            ++family_count;

                        spellSet.Add(spell.SpellLevel, spell.Id);
                        ++count;
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pet levelup and default spells for {1} families in {2} ms", count, family_count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadPetDefaultSpells()
        {
            uint oldMSTime = Time.GetMSTime();

            mPetDefaultSpellsMap.Clear();

            uint countCreature = 0;

            Log.outInfo(LogFilter.ServerLoading, "Loading summonable creature templates...");
            oldMSTime = Time.GetMSTime();

            // different summon spells
            foreach (var spellEntry in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in spellEntry.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect != null && (effect.Effect == SpellEffectName.Summon || effect.Effect == SpellEffectName.SummonPet))
                    {
                        int creature_id = effect.MiscValue;
                        CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate((uint)creature_id);
                        if (cInfo == null)
                            continue;

                        // get default pet spells from creature_template
                        uint petSpellsId = cInfo.Entry;
                        if (mPetDefaultSpellsMap.LookupByKey(cInfo.Entry) != null)
                            continue;

                        PetDefaultSpellsEntry petDefSpells = new PetDefaultSpellsEntry();
                        for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
                            petDefSpells.spellid[j] = cInfo.Spells[j];

                        if (LoadPetDefaultSpells_helper(cInfo, petDefSpells))
                        {
                            mPetDefaultSpellsMap[petSpellsId] = petDefSpells;
                            ++countCreature;
                        }
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} summonable creature templates in {1} ms", countCreature, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        bool LoadPetDefaultSpells_helper(CreatureTemplate cInfo, PetDefaultSpellsEntry petDefSpells)
        {
            // skip empty list;
            bool have_spell = false;
            for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
            {
                if (petDefSpells.spellid[j] != 0)
                {
                    have_spell = true;
                    break;
                }
            }
            if (!have_spell)
                return false;

            // remove duplicates with levelupSpells if any
            var levelupSpells = cInfo.Family != 0 ? GetPetLevelupSpellList(cInfo.Family) : null;
            if (levelupSpells != null)
            {
                for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
                {
                    if (petDefSpells.spellid[j] == 0)
                        continue;

                    foreach (var pair in levelupSpells)
                    {
                        if (pair.Value == petDefSpells.spellid[j])
                        {
                            petDefSpells.spellid[j] = 0;
                            break;
                        }
                    }
                }
            }

            // skip empty list;
            have_spell = false;
            for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
            {
                if (petDefSpells.spellid[j] != 0)
                {
                    have_spell = true;
                    break;
                }
            }

            return have_spell;
        }

        public void LoadSpellAreas()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellAreaMap.Clear();                                  // need for reload case
            mSpellAreaForQuestMap.Clear();
            mSpellAreaForQuestEndMap.Clear();
            mSpellAreaForAuraMap.Clear();

            //                                            0     1         2              3               4                 5          6          7       8      9
            SQLResult result = DB.World.Query("SELECT spell, area, quest_start, quest_start_status, quest_end_status, quest_end, aura_spell, racemask, gender, flags FROM spell_area");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell area requirements. DB table `spell_area` is empty.");

                return;
            }

            uint count = 0;
            do
            {
                uint spell = result.Read<uint>(0);

                SpellArea spellArea = new SpellArea();
                spellArea.spellId = spell;
                spellArea.areaId = result.Read<uint>(1);
                spellArea.questStart = result.Read<uint>(2);
                spellArea.questStartStatus = result.Read<uint>(3);
                spellArea.questEndStatus = result.Read<uint>(4);
                spellArea.questEnd = result.Read<uint>(5);
                spellArea.auraSpell = result.Read<int>(6);
                spellArea.raceMask = result.Read<ulong>(7);
                spellArea.gender = (Gender)result.Read<uint>(8);
                spellArea.flags = (SpellAreaFlag)result.Read<byte>(9);

                SpellInfo spellInfo = GetSpellInfo(spell);
                if (spellInfo != null)
                {
                    if (spellArea.flags.HasAnyFlag(SpellAreaFlag.AutoCast))
                        spellInfo.Attributes |= SpellAttr0.CantCancel;
                }
                else
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` does not exist", spell);
                    continue;
                }

                {
                    bool ok = true;
                    var sa_bounds = GetSpellAreaMapBounds(spellArea.spellId);
                    foreach (var bound in sa_bounds)
                    {
                        if (spellArea.spellId != bound.spellId)
                            continue;
                        if (spellArea.areaId != bound.areaId)
                            continue;
                        if (spellArea.questStart != bound.questStart)
                            continue;
                        if (spellArea.auraSpell != bound.auraSpell)
                            continue;
                        if ((spellArea.raceMask & bound.raceMask) == 0)
                            continue;
                        if (spellArea.gender != bound.gender)
                            continue;

                        // duplicate by requirements
                        ok = false;
                        break;
                    }

                    if (!ok)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` already listed with similar requirements.", spell);
                        continue;
                    }
                }

                if (spellArea.areaId != 0 && !CliDB.AreaTableStorage.ContainsKey(spellArea.areaId))
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong area ({1}) requirement", spell, spellArea.areaId);
                    continue;
                }

                if (spellArea.questStart != 0 && Global.ObjectMgr.GetQuestTemplate(spellArea.questStart) == null)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong start quest ({1}) requirement", spell, spellArea.questStart);
                    continue;
                }

                if (spellArea.questEnd != 0)
                {
                    if (Global.ObjectMgr.GetQuestTemplate(spellArea.questEnd) == null)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong end quest ({1}) requirement", spell, spellArea.questEnd);
                        continue;
                    }
                }

                if (spellArea.auraSpell != 0)
                {
                    SpellInfo info = GetSpellInfo((uint)Math.Abs(spellArea.auraSpell));
                    if (info == null)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong aura spell ({1}) requirement", spell, Math.Abs(spellArea.auraSpell));
                        continue;
                    }

                    if (Math.Abs(spellArea.auraSpell) == spellArea.spellId)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement for itself", spell, Math.Abs(spellArea.auraSpell));
                        continue;
                    }

                    // not allow autocast chains by auraSpell field (but allow use as alternative if not present)
                    if (spellArea.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spellArea.auraSpell > 0)
                    {
                        bool chain = false;
                        var saBound = GetSpellAreaForAuraMapBounds(spellArea.spellId);
                        foreach (var bound in saBound)
                        {
                            if (bound.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && bound.auraSpell > 0)
                            {
                                chain = true;
                                break;
                            }
                        }

                        if (chain)
                        {
                            Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement that itself autocast from aura", spell, spellArea.auraSpell);
                            continue;
                        }

                        var saBound2 = GetSpellAreaMapBounds((uint)spellArea.auraSpell);
                        foreach (var bound in saBound2)
                        {
                            if (bound.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && bound.auraSpell > 0)
                            {
                                chain = true;
                                break;
                            }
                        }

                        if (chain)
                        {
                            Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement that itself autocast from aura", spell, spellArea.auraSpell);
                            continue;
                        }
                    }
                }

                if (spellArea.raceMask != 0 && (spellArea.raceMask & (uint)Race.RaceMaskAllPlayable) == 0)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong race mask ({1}) requirement", spell, spellArea.raceMask);
                    continue;
                }

                if (spellArea.gender != Gender.None && spellArea.gender != Gender.Female && spellArea.gender != Gender.Male)
                {
                    Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong gender ({1}) requirement", spell, spellArea.gender);
                    continue;
                }
                mSpellAreaMap.Add(spell, spellArea);
                var sa = mSpellAreaMap[spell];

                // for search by current zone/subzone at zone/subzone change
                if (spellArea.areaId != 0)
                    mSpellAreaForAreaMap.AddRange(spellArea.areaId, sa);

                // for search at quest start/reward
                if (spellArea.questStart != 0)
                    mSpellAreaForQuestMap.AddRange(spellArea.questStart, sa);

                // for search at quest start/reward
                if (spellArea.questEnd != 0)
                    mSpellAreaForQuestEndMap.AddRange(spellArea.questEnd, sa);

                // for search at aura apply
                if (spellArea.auraSpell != 0)
                    mSpellAreaForAuraMap.AddRange((uint)Math.Abs(spellArea.auraSpell), sa);

                if (spellArea.areaId != 0 && spellArea.questStart != 0)
                    mSpellAreaForQuestAreaMap.AddRange(Tuple.Create(spellArea.areaId, spellArea.questStart), sa);

                if (spellArea.areaId != 0 && spellArea.questEnd != 0)
                    mSpellAreaForQuestAreaMap.AddRange(Tuple.Create(spellArea.areaId, spellArea.questEnd), sa);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell area requirements in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellInfoStore()
        {
            uint oldMSTime = Time.GetMSTime();

            mSpellInfoMap.Clear();
            Dictionary<uint, SpellInfoLoadHelper> loadData = new Dictionary<uint, SpellInfoLoadHelper>();

            Dictionary<uint, Dictionary<uint, SpellEffectRecord[]>> effectsBySpell = new Dictionary<uint, Dictionary<uint, SpellEffectRecord[]>>();
            Dictionary<uint, MultiMap<uint, SpellXSpellVisualRecord>> visualsBySpell = new Dictionary<uint, MultiMap<uint, SpellXSpellVisualRecord>>();
            foreach (var effect in CliDB.SpellEffectStorage.Values)
            {
                Cypher.Assert(effect.EffectIndex < SpellConst.MaxEffects, $"MAX_SPELL_EFFECTS must be at least {effect.EffectIndex}");
                Cypher.Assert(effect.Effect < (int)SpellEffectName.TotalSpellEffects, $"TOTAL_SPELL_EFFECTS must be at least {effect.Effect}");
                Cypher.Assert(effect.EffectAura < (int)AuraType.Total, $"TOTAL_AURAS must be at least {effect.EffectAura}");
                Cypher.Assert(effect.ImplicitTarget[0] < (int)Targets.TotalSpellTargets, $"TOTAL_SPELL_TARGETS must be at least {effect.ImplicitTarget[0]}");
                Cypher.Assert(effect.ImplicitTarget[1] < (int)Targets.TotalSpellTargets, $"TOTAL_SPELL_TARGETS must be at least {effect.ImplicitTarget[1]}");

                if (!effectsBySpell.ContainsKey(effect.SpellID))
                    effectsBySpell[effect.SpellID] = new Dictionary<uint, SpellEffectRecord[]>();

                if (!effectsBySpell[effect.SpellID].ContainsKey(effect.DifficultyID))
                    effectsBySpell[effect.SpellID][effect.DifficultyID] = new SpellEffectRecord[SpellConst.MaxEffects];

                effectsBySpell[effect.SpellID][effect.DifficultyID][effect.EffectIndex] = effect;
            }

            foreach (var spell in CliDB.SpellNameStorage.Values)
                loadData[spell.Id] = new SpellInfoLoadHelper();

            foreach (SpellAuraOptionsRecord auraOptions in CliDB.SpellAuraOptionsStorage.Values)
                if (auraOptions.DifficultyID == 0)    // TODO: implement
                    loadData[auraOptions.SpellID].AuraOptions = auraOptions;

            CliDB.SpellAuraOptionsStorage.Clear();

            foreach (SpellAuraRestrictionsRecord auraRestrictions in CliDB.SpellAuraRestrictionsStorage.Values)
            {
                if (auraRestrictions.DifficultyID == 0)    // TODO: implement
                    loadData[auraRestrictions.SpellID].AuraRestrictions = auraRestrictions;
            }
            CliDB.SpellAuraRestrictionsStorage.Clear();

            foreach (SpellCastingRequirementsRecord castingRequirements in CliDB.SpellCastingRequirementsStorage.Values)
                loadData[castingRequirements.SpellID].CastingRequirements = castingRequirements;

            CliDB.SpellCastingRequirementsStorage.Clear();

            foreach (SpellCategoriesRecord categories in CliDB.SpellCategoriesStorage.Values)
            {
                if (categories.DifficultyID == 0)  // TODO: implement
                    loadData[categories.SpellID].Categories = categories;
            }
            CliDB.SpellCategoriesStorage.Clear();

            foreach (SpellClassOptionsRecord classOptions in CliDB.SpellClassOptionsStorage.Values)
                loadData[classOptions.SpellID].ClassOptions = classOptions;

            CliDB.SpellClassOptionsStorage.Clear();

            foreach (SpellCooldownsRecord cooldowns in CliDB.SpellCooldownsStorage.Values)
            {
                if (cooldowns.DifficultyID == 0)   // TODO: implement
                    loadData[cooldowns.SpellID].Cooldowns = cooldowns;
            }
            CliDB.SpellCooldownsStorage.Clear();

            foreach (SpellEquippedItemsRecord equippedItems in CliDB.SpellEquippedItemsStorage.Values)
                loadData[equippedItems.SpellID].EquippedItems = equippedItems;

            CliDB.SpellEquippedItemsStorage.Clear();

            foreach (SpellInterruptsRecord interrupts in CliDB.SpellInterruptsStorage.Values)
            {
                if (interrupts.DifficultyID == 0)  // TODO: implement
                    loadData[interrupts.SpellID].Interrupts = interrupts;
            }
            CliDB.SpellInterruptsStorage.Clear();

            foreach (SpellLevelsRecord levels in CliDB.SpellLevelsStorage.Values)
            {
                if (levels.DifficultyID == 0)  // TODO: implement
                    loadData[levels.SpellID].Levels = levels;
            }

            foreach (SpellMiscRecord misc in CliDB.SpellMiscStorage.Values)
                if (misc.DifficultyID == 0)
                    loadData[misc.SpellID].Misc = misc;

            foreach (SpellReagentsRecord reagents in CliDB.SpellReagentsStorage.Values)
                loadData[reagents.SpellID].Reagents = reagents;

            CliDB.SpellReagentsStorage.Clear();

            foreach (SpellScalingRecord scaling in CliDB.SpellScalingStorage.Values)
                loadData[scaling.SpellID].Scaling = scaling;

            CliDB.SpellScalingStorage.Clear();

            foreach (SpellShapeshiftRecord shapeshift in CliDB.SpellShapeshiftStorage.Values)
                loadData[shapeshift.SpellID].Shapeshift = shapeshift;

            CliDB.SpellShapeshiftStorage.Clear();

            foreach (SpellTargetRestrictionsRecord targetRestrictions in CliDB.SpellTargetRestrictionsStorage.Values)
            {
                if (targetRestrictions.DifficultyID == 0)  // TODO: implement
                    loadData[targetRestrictions.SpellID].TargetRestrictions = targetRestrictions;
            }
            CliDB.SpellTargetRestrictionsStorage.Clear();

            foreach (SpellTotemsRecord totems in CliDB.SpellTotemsStorage.Values)
                loadData[totems.SpellID].Totems = totems;

            CliDB.SpellTotemsStorage.Clear();

            foreach (var visual in CliDB.SpellXSpellVisualStorage.Values)
            {
                if (!visualsBySpell.ContainsKey(visual.SpellID))
                    visualsBySpell[visual.SpellID] = new MultiMap<uint, SpellXSpellVisualRecord>();

                visualsBySpell[visual.SpellID].Add(visual.DifficultyID, visual);
            }

            foreach (var spellEntry in CliDB.SpellNameStorage.Values)
            {
                loadData[spellEntry.Id].Entry = spellEntry;
                mSpellInfoMap[spellEntry.Id] = new SpellInfo(loadData[spellEntry.Id], effectsBySpell.LookupByKey(spellEntry.Id), visualsBySpell.LookupByKey(spellEntry.Id));
            }

            CliDB.SpellNameStorage.Clear();

            Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo store in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void UnloadSpellInfoImplicitTargetConditionLists()
        {
            foreach (var spell in mSpellInfoMap.Values)
                spell._UnloadImplicitTargetConditionLists();

        }

        public void LoadSpellInfoCustomAttributes()
        {
            uint oldMSTime = Time.GetMSTime();
            uint oldMSTime2 = oldMSTime;

            SQLResult result = DB.World.Query("SELECT entry, attributes FROM spell_custom_attr");
            if (result.IsEmpty())
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell custom attributes from DB. DB table `spell_custom_attr` is empty.");
            else
            {
                uint count = 0;
                do
                {
                    uint spellId = result.Read<uint>(0);
                    uint attributes = result.Read<uint>(1);

                    SpellInfo spellInfo = GetSpellInfo(spellId);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "Table `spell_custom_attr` has wrong spell (entry: {0}), ignored.", spellId);
                        continue;
                    }

                    // TODO: validate attributes
                    if (attributes.HasAnyFlag((uint)SpellCustomAttributes.ShareDamage))
                    {
                        if (!spellInfo.HasEffect(SpellEffectName.SchoolDamage))
                        {
                            Log.outError(LogFilter.Sql, "Spell {0} listed in table `spell_custom_attr` with SPELL_ATTR0_CU_SHARE_DAMAGE has no SPELL_EFFECT_SCHOOL_DAMAGE, ignored.", spellId);
                            continue;
                        }
                    }

                    spellInfo.AttributesCu |= (SpellCustomAttributes)attributes;
                    ++count;
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell custom attributes from DB in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime2));
            }

            List<uint> talentSpells = new List<uint>();
            foreach (var talentInfo in CliDB.TalentStorage.Values)
                talentSpells.Add(talentInfo.SpellID);

            foreach (var spellInfo in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null)
                        continue;

                    switch (effect.ApplyAuraName)
                    {
                        case AuraType.PeriodicHeal:
                        case AuraType.PeriodicDamage:
                        case AuraType.PeriodicDamagePercent:
                        case AuraType.PeriodicLeech:
                        case AuraType.PeriodicManaLeech:
                        case AuraType.PeriodicHealthFunnel:
                        case AuraType.PeriodicEnergize:
                        case AuraType.ObsModHealth:
                        case AuraType.ObsModPower:
                        case AuraType.PowerBurn:
                            spellInfo.AttributesCu |= SpellCustomAttributes.NoInitialThreat;
                            break;
                    }

                    switch (effect.Effect)
                    {
                        case SpellEffectName.SchoolDamage:
                        case SpellEffectName.WeaponDamage:
                        case SpellEffectName.WeaponDamageNoschool:
                        case SpellEffectName.NormalizedWeaponDmg:
                        case SpellEffectName.WeaponPercentDamage:
                        case SpellEffectName.Heal:
                            spellInfo.AttributesCu |= SpellCustomAttributes.DirectDamage;
                            break;
                        case SpellEffectName.PowerDrain:
                        case SpellEffectName.PowerBurn:
                        case SpellEffectName.HealMaxHealth:
                        case SpellEffectName.HealthLeech:
                        case SpellEffectName.HealPct:
                        case SpellEffectName.EnergizePct:
                        case SpellEffectName.Energize:
                        case SpellEffectName.HealMechanical:
                            spellInfo.AttributesCu |= SpellCustomAttributes.NoInitialThreat;
                            break;
                        case SpellEffectName.Charge:
                        case SpellEffectName.ChargeDest:
                        case SpellEffectName.Jump:
                        case SpellEffectName.JumpDest:
                        case SpellEffectName.LeapBack:
                            spellInfo.AttributesCu |= SpellCustomAttributes.Charge;
                            break;
                        case SpellEffectName.Pickpocket:
                            spellInfo.AttributesCu |= SpellCustomAttributes.PickPocket;
                            break;
                        case SpellEffectName.EnchantItem:
                        case SpellEffectName.EnchantItemTemporary:
                        case SpellEffectName.EnchantItemPrismatic:
                        case SpellEffectName.EnchantHeldItem:
                            {
                                // only enchanting profession enchantments procs can stack
                                if (IsPartOfSkillLine(SkillType.Enchanting, spellInfo.Id))
                                {
                                    uint enchantId = (uint)effect.MiscValue;
                                    var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);
                                    for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                                    {
                                        if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                                            continue;

                                        SpellInfo procInfo = GetSpellInfo(enchant.EffectArg[s]);
                                        if (procInfo == null)
                                            continue;

                                        // if proced directly from enchantment, not via proc aura
                                        // NOTE: Enchant Weapon - Blade Ward also has proc aura spell and is proced directly
                                        // however its not expected to stack so this check is good
                                        if (procInfo.HasAura(Difficulty.None, AuraType.ProcTriggerSpell))
                                            continue;

                                        procInfo.AttributesCu |= SpellCustomAttributes.EnchantProc;
                                    }
                                }
                                break;
                            }
                    }
                }

                if (!spellInfo._IsPositiveEffect(0, false))
                    spellInfo.AttributesCu |= SpellCustomAttributes.NegativeEff0;

                if (!spellInfo._IsPositiveEffect(1, false))
                    spellInfo.AttributesCu |= SpellCustomAttributes.NegativeEff1;

                if (!spellInfo._IsPositiveEffect(2, false))
                    spellInfo.AttributesCu |= SpellCustomAttributes.NegativeEff2;

                if (talentSpells.Contains(spellInfo.Id))
                    spellInfo.AttributesCu |= SpellCustomAttributes.IsTalent;

                spellInfo._InitializeExplicitTargetMask();
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded spell custom attributes in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellInfoCorrections()
        {
            uint oldMSTime = Time.GetMSTime();

            foreach (var spellInfo in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null)
                        continue;

                    switch (effect.Effect)
                    {
                        case SpellEffectName.Charge:
                        case SpellEffectName.ChargeDest:
                        case SpellEffectName.Jump:
                        case SpellEffectName.JumpDest:
                        case SpellEffectName.LeapBack:
                            if (spellInfo.Speed == 0 && spellInfo.SpellFamilyName == 0)
                                spellInfo.Speed = MotionMaster.SPEED_CHARGE;
                            break;
                    }

                    if (effect.TargetA.GetSelectionCategory() == SpellTargetSelectionCategories.Cone || effect.TargetB.GetSelectionCategory() == SpellTargetSelectionCategories.Cone)
                        if (MathFunctions.fuzzyEq(spellInfo.ConeAngle, 0.0f))
                            spellInfo.ConeAngle = 90.0f;
                }

                if (spellInfo.ActiveIconFileDataId == 135754)  // flight
                    spellInfo.Attributes |= SpellAttr0.Passive;

                switch (spellInfo.Id)
                {
                    case 63026: // Summon Aspirant Test NPC (HACK: Target shouldn't be changed)
                    case 63137: // Summon Valiant Test (HACK: Target shouldn't be changed; summon position should be untied from spell destination)
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.DestDb);
                        break;
                    case 52611: // Summon Skeletons
                    case 52612: // Summon Skeletons
                        spellInfo.GetEffect(0).MiscValueB = 64;
                        break;
                    case 40244: // Simon Game Visual
                    case 40245: // Simon Game Visual
                    case 40246: // Simon Game Visual
                    case 40247: // Simon Game Visual
                    case 42835: // Spout, remove damage effect, only anim is needed
                        spellInfo.GetEffect(0).Effect = 0;
                        break;
                    case 63665: // Charge (Argent Tournament emote on riders)
                    case 31298: // Sleep (needs target selection script)
                    case 51904: // Summon Ghouls On Scarlet Crusade (this should use conditions table, script for this spell needs to be fixed)
                    case 68933: // Wrath of Air Totem rank 2 (Aura)
                    case 29200: // Purify Helboar Meat
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitCaster);
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo();
                        break;
                    case 31344: // Howl of Azgalor
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards100); // 100yards instead of 50000?!
                        break;
                    case 42818: // Headless Horseman - Wisp Flight Port
                    case 42821: // Headless Horseman - Wisp Flight Missile
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6); // 100 yards
                        break;
                    case 36350: //They Must Burn Bomb Aura (self)
                        spellInfo.GetEffect(0).TriggerSpell = 36325; // They Must Burn Bomb Drop (DND)
                        break;
                    case 5308: // Execute
                        spellInfo.AttributesEx3 |= SpellAttr3.CantTriggerProc;
                        break;
                    case 31347: // Doom
                    case 36327: // Shoot Arcane Explosion Arrow
                    case 39365: // Thundering Storm
                    case 41071: // Raise Dead (HACK)
                    case 42442: // Vengeance Landing Cannonfire
                    case 42611: // Shoot
                    case 44978: // Wild Magic
                    case 45001: // Wild Magic
                    case 45002: // Wild Magic
                    case 45004: // Wild Magic
                    case 45006: // Wild Magic
                    case 45010: // Wild Magic
                    case 45761: // Shoot Gun
                    case 45863: // Cosmetic - Incinerate to Random Target
                    case 48246: // Ball of Flame
                    case 41635: // Prayer of Mending
                    case 44869: // Spectral Blast
                    case 45027: // Revitalize
                    case 45976: // Muru Portal Channel
                    case 52124: // Sky Darkener Assault
                    case 52479: // Gift of the Harvester
                    case 61588: // Blazing Harpoon
                    case 55479: // Force Obedience
                    case 28560: // Summon Blizzard (Sapphiron)
                    case 53096: // Quetz'lun's Judgment
                    case 70743: // AoD Special
                    case 70614: // AoD Special - Vegard
                    case 4020: // Safirdrang's Chill
                    case 52438: // Summon Skittering Swarmer (Force Cast)
                    case 52449: // Summon Skittering Infector (Force Cast)
                    case 53609: // Summon Anub'ar Assassin (Force Cast)
                    case 53457: // Summon Impale Trigger (AoE)
                        spellInfo.MaxAffectedTargets = 1;
                        break;
                    case 36384: // Skartax Purple Beam
                        spellInfo.MaxAffectedTargets = 2;
                        break;
                    case 28542: // Life Drain - Sapphiron
                    case 29213: // Curse of the Plaguebringer - Noth
                    case 29576: // Multi-Shot
                    case 37790: // Spread Shot
                    case 39992: // Needle Spine
                    case 40816: // Saber Lash
                    case 41303: // Soul Drain
                    case 41376: // Spite
                    case 45248: // Shadow Blades
                    case 46771: // Flame Sear
                    case 66588: // Flaming Spear
                        spellInfo.MaxAffectedTargets = 3;
                        break;
                    case 38310: // Multi-Shot
                    case 53385: // Divine Storm (Damage)
                        spellInfo.MaxAffectedTargets = 4;
                        break;
                    case 42005: // Bloodboil
                    case 38296: // Spitfire Totem
                    case 37676: // Insidious Whisper
                    case 46008: // Negative Energy
                    case 45641: // Fire Bloom
                    case 55665: // Life Drain - Sapphiron (H)
                    case 28796: // Poison Bolt Volly - Faerlina
                        spellInfo.MaxAffectedTargets = 5;
                        break;
                    case 54835: // Curse of the Plaguebringer - Noth (H)
                        spellInfo.MaxAffectedTargets = 8;
                        break;
                    case 40827: // Sinful Beam
                    case 40859: // Sinister Beam
                    case 40860: // Vile Beam
                    case 40861: // Wicked Beam
                    case 54098: // Poison Bolt Volly - Faerlina (H)
                        spellInfo.MaxAffectedTargets = 10;
                        break;
                    case 50312: // Unholy Frenzy
                        spellInfo.MaxAffectedTargets = 15;
                        break;
                    case 33711: // Murmur's Touch
                    case 38794:
                        spellInfo.MaxAffectedTargets = 1;
                        spellInfo.GetEffect(0).TriggerSpell = 33760;
                        break;
                    case 44544: // Fingers of Frost
                        spellInfo.GetEffect(0).SpellClassMask = new FlagArray128(685904631, 1151048, 0, 0);
                        break;
                    case 52212: // Death and Decay
                    case 41485: // Deadly Poison - Black Temple
                    case 41487:  // Envenom - Black Temple
                        spellInfo.AttributesEx6 |= SpellAttr6.CanTargetInvisible;
                        break;
                    case 37408: // Oscillation Field
                        spellInfo.AttributesEx3 |= SpellAttr3.StackForDiffCasters;
                        break;
                    case 51852: // The Eye of Acherus (no spawn in phase 2 in db)
                        spellInfo.GetEffect(0).MiscValue |= 1;
                        break;
                    case 51912: // Crafty's Ultra-Advanced Proto-Typical Shortening Blaster
                        spellInfo.GetEffect(0).ApplyAuraPeriod = 3000;
                        break;
                    case 30421: // Nether Portal - Perseverence
                        spellInfo.GetEffect(2).BasePoints += 30000;
                        break;
                    case 41913: // Parasitic Shadowfiend Passive
                        spellInfo.GetEffect(0).ApplyAuraName = AuraType.Dummy; // proc debuff, and summon infinite fiends
                        break;
                    case 27892: // To Anchor 1
                    case 27928: // To Anchor 1
                    case 27935: // To Anchor 1
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards10);
                        break;
                    // target allys instead of enemies, target A is src_caster, spells with effect like that have ally target
                    // this is the only known exception, probably just wrong data
                    case 29214: // Wrath of the Plaguebringer
                    case 54836: // Wrath of the Plaguebringer
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.UnitSrcAreaAlly);
                        spellInfo.GetEffect(1).TargetB = new SpellImplicitTargetInfo(Targets.UnitSrcAreaAlly);
                        break;
                    case 15290: // Vampiric Embrace
                        spellInfo.AttributesEx3 |= SpellAttr3.NoInitialAggro;
                        break;
                    case 6474: // Earthbind Totem (instant pulse)
                        spellInfo.AttributesEx5 |= SpellAttr5.StartPeriodicAtApply;
                        break;
                    case 70728: // Exploit Weakness (needs target selection script)
                    case 70840: // Devious Minds (needs target selection script)
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitCaster);
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.UnitPet);
                        break;
                    case 45602: // Ride Carpet
                        spellInfo.GetEffect(0).BasePoints = 0;// force seat 0, vehicle doesn't have the required seat flags for "no seat specified (-1)"
                        break;
                    case 61719: // Easter Lay Noblegarden Egg Aura - Interrupt flags copied from aura which this aura is linked with
                        spellInfo.AuraInterruptFlags[0] = (uint)(SpellAuraInterruptFlags.Hitbyspell | SpellAuraInterruptFlags.TakeDamage);
                        break;
                    case 71838: // Drain Life - Bryntroll Normal
                    case 71839: // Drain Life - Bryntroll Heroic
                        spellInfo.AttributesEx2 |= SpellAttr2.CantCrit;
                        break;
                    case 56606: // Ride Jokkum
                    case 61791: // Ride Vehicle (Yogg-Saron)
                                // @todo: remove this when basepoints of all Ride Vehicle auras are calculated correctly
                        spellInfo.GetEffect(0).BasePoints = 1;
                        break;
                    case 59630: // Black Magic
                        spellInfo.Attributes |= SpellAttr0.Passive;
                        break;
                    case 48278: // Paralyze
                        spellInfo.AttributesEx3 |= SpellAttr3.StackForDiffCasters;
                        break;
                    case 51798: // Brewfest - Relay Race - Intro - Quest Complete
                    case 47134: // Quest Complete
                                //! HACK: This spell break quest complete for alliance and on retail not used °_O
                        spellInfo.GetEffect(0).Effect = 0;
                        break;
                    case 85123: // Siege Cannon (Tol Barad)
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200);
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitSrcAreaEntry);
                        break;
                    case 198300: // Gathering Storms
                        spellInfo.ProcCharges = 1; // override proc charges, has 0 (unlimited) in db2
                        break;
                    case 42490: // Energized!
                    case 42492: // Cast Energized
                    case 43115: // Plague Vial
                        spellInfo.AttributesEx |= SpellAttr1.NoThreat;
                        break;
                    case 29726: // Test Ribbon Pole Channel
                        spellInfo.InterruptFlags &= ~SpellInterruptFlags.Interrupt;//AURA_INTERRUPT_FLAG_CAST
                        break;
                    case 42767: // Sic'em
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitNearbyEntry);
                        break;
                    case 42793: // Burn Body
                        spellInfo.GetEffect(2).MiscValue = 24008; // Fallen Combatant
                        break;
                    case 59544:// Gift of the Naaru (priest and monk variants)
                    case 121093:
                        spellInfo.SpellFamilyFlags[2] = 0x80000000;
                        break;
                    case 50661:// Weakened Resolve                    
                    case 68979:// Unleashed Souls
                    case 48714:// Compelled
                    case 7853: // The Art of Being a Water Terror: Force Cast on Player
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
                        break;
                    // VIOLET HOLD SPELLS
                    //
                    case 54258: // Water Globule (Ichoron)
                    case 54264: // Water Globule (Ichoron)
                    case 54265: // Water Globule (Ichoron)
                    case 54266: // Water Globule (Ichoron)
                    case 54267: // Water Globule (Ichoron)
                                // in 3.3.5 there is only one radius in dbc which is 0 yards in this case
                                // use max radius from 4.3.4
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards25);
                        break;
                    // ENDOF VIOLET HOLD
                    //
                    // ULDUAR SPELLS
                    //
                    case 62374: // Pursued (Flame Leviathan)
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards50000);   // 50000yd
                        break;
                    case 63342: // Focused Eyebeam Summon Trigger (Kologarn)
                        spellInfo.MaxAffectedTargets = 1;
                        break;
                    case 65584: // Growth of Nature (Freya)
                    case 64381: // Strength of the Pack (Auriaya)
                        spellInfo.AttributesEx3 |= SpellAttr3.StackForDiffCasters;
                        break;
                    case 63018: // Searing Light (XT-002)
                    case 65121: // Searing Light (25m) (XT-002)
                    case 63024: // Gravity Bomb (XT-002)
                    case 64234: // Gravity Bomb (25m) (XT-002)
                        spellInfo.MaxAffectedTargets = 1;
                        break;
                    case 62834: // Boom (XT-002)
                                // This hack is here because we suspect our implementation of spell effect execution on targets
                                // is done in the wrong order. We suspect that 0 needs to be applied on all targets,
                                // then 1, etc - instead of applying each effect on target1, then target2, etc.
                                // The above situation causes the visual for this spell to be bugged, so we remove the instakill
                                // effect and implement a script hack for that.
                        spellInfo.GetEffect(1).Effect = 0;
                        break;
                    case 64386: // Terrifying Screech (Auriaya)
                    case 64389: // Sentinel Blast (Auriaya)
                    case 64678: // Sentinel Blast (Auriaya)
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(28); // 5 seconds, wrong DBC data?
                        break;
                    case 64321: // Potent Pheromones (Freya)
                                // spell should dispel area aura, but doesn't have the attribute
                                // may be db data bug, or blizz may keep reapplying area auras every update with checking immunity
                                // that will be clear if we get more spells with problem like this
                        spellInfo.AttributesEx |= SpellAttr1.DispelAurasOnImmunity;
                        break;
                    case 63414: // Spinning Up (Mimiron)
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.UnitCaster);
                        spellInfo.ChannelInterruptFlags.Clear();
                        break;
                    case 63036: // Rocket Strike (Mimiron)
                        spellInfo.Speed = 0;
                        break;
                    case 64668: // Magnetic Field (Mimiron)
                        spellInfo.Mechanic = Mechanics.None;
                        break;
                    case 64468: // Empowering Shadows (Yogg-Saron)
                    case 64486: // Empowering Shadows (Yogg-Saron)
                        spellInfo.MaxAffectedTargets = 3;  // same for both modes?
                        break;
                    case 62301: // Cosmic Smash (Algalon the Observer)
                        spellInfo.MaxAffectedTargets = 1;
                        break;
                    case 64598: // Cosmic Smash (Algalon the Observer)
                        spellInfo.MaxAffectedTargets = 3;
                        break;
                    case 62293: // Cosmic Smash (Algalon the Observer)
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.DestCaster);
                        break;
                    case 62311: // Cosmic Smash (Algalon the Observer)
                    case 64596: // Cosmic Smash (Algalon the Observer)
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6);  // 100yd
                        break;
                    case 64014: // Expedition Base Camp Teleport
                    case 64024: // Conservatory Teleport
                    case 64025: // Halls of Invention Teleport
                    case 64028: // Colossal Forge Teleport
                    case 64029: // Shattered Walkway Teleport
                    case 64030: // Antechamber Teleport
                    case 64031: // Scrapyard Teleport
                    case 64032: // Formation Grounds Teleport
                    case 65042: // Prison of Yogg-Saron Teleport
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.DestDb);
                        break;
                    // ENDOF ULDUAR SPELLS
                    //
                    // TRIAL OF THE CRUSADER SPELLS
                    //
                    case 66258: // Infernal Eruption
                        // increase duration from 15 to 18 seconds because caster is already
                        // unsummoned when spell missile hits the ground so nothing happen in result
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(85);
                        break;
                    // ENDOF TRIAL OF THE CRUSADER SPELLS
                    //
                    // ICECROWN CITADEL SPELLS
                    //
                    // THESE SPELLS ARE WORKING CORRECTLY EVEN WITHOUT THIS HACK
                    // THE ONLY REASON ITS HERE IS THAT CURRENT GRID SYSTEM
                    // DOES NOT ALLOW FAR OBJECT SELECTION (dist > 333)
                    case 70781: // Light's Hammer Teleport
                    case 70856: // Oratory of the Damned Teleport
                    case 70857: // Rampart of Skulls Teleport
                    case 70858: // Deathbringer's Rise Teleport
                    case 70859: // Upper Spire Teleport
                    case 70860: // Frozen Throne Teleport
                    case 70861: // Sindragosa's Lair Teleport
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.DestDb);
                        break;
                    case 71169: // Shadow's Fate
                        spellInfo.AttributesEx3 |= SpellAttr3.StackForDiffCasters;
                        break;
                    case 72347: // Lock Players and Tap Chest
                        spellInfo.AttributesEx3 &= ~SpellAttr3.NoInitialAggro;
                        break;
                    case 72723: // Resistant Skin (Deathbringer Saurfang adds)
                        // this spell initially granted Shadow damage immunity, however it was removed but the data was left in client
                        spellInfo.GetEffect(2).Effect = 0;
                        break;
                    case 70460: // Coldflame Jets (Traps after Saurfang)
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(1); // 10 seconds
                        break;
                    case 71412: // Green Ooze Summon (Professor Putricide)
                    case 71415: // Orange Ooze Summon (Professor Putricide)
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitAny);
                        break;
                    case 71159: // Awaken Plagued Zombies
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(21);
                        break;
                    case 70530: // Volatile Ooze Beam Protection (Professor Putricide)
                        spellInfo.GetEffect(0).Effect = SpellEffectName.ApplyAura; // for an unknown reason this was SPELL_EFFECT_APPLY_AREA_AURA_RAID
                        break;
                    // THIS IS HERE BECAUSE COOLDOWN ON CREATURE PROCS IS NOT IMPLEMENTED
                    case 71604: // Mutated Strength (Professor Putricide)
                        spellInfo.GetEffect(1).Effect = 0;
                        break;
                    case 70911: // Unbound Plague (Professor Putricide) (needs target selection script)
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.UnitEnemy);
                        break;
                    case 71708: // Empowered Flare (Blood Prince Council)
                        spellInfo.AttributesEx3 |= SpellAttr3.NoDoneBonus;
                        break;
                    case 71266: // Swarming Shadows
                        spellInfo.RequiredAreasID = 0; // originally, these require area 4522, which is... outside of Icecrown Citadel
                        break;
                    case 70602: // Corruption
                        spellInfo.AttributesEx3 |= SpellAttr3.StackForDiffCasters;
                        break;
                    case 70715: // Column of Frost (visual marker)
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(32); // 6 seconds (missing)
                        break;
                    case 71085: // Mana Void (periodic aura)
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(9); // 30 seconds (missing)
                        break;
                    case 70936: // Summon Suppressor (needs target selection script)
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitAny);
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo();
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(157); // 90yd
                        break;
                    case 70598: // Sindragosa's Fury
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.DestDest);
                        break;
                    case 69846: // Frost Bomb
                        spellInfo.Speed = 0.0f;    // This spell's summon happens instantly
                        break;
                    case 71614: // Ice Lock
                        spellInfo.Mechanic = Mechanics.Stun;
                        break;
                    case 72762: // Defile
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(559); // 53 seconds
                        break;
                    case 72743: // Defile
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(22); // 45 seconds
                        break;
                    case 72754: // Defile
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200); // 200yd
                        spellInfo.GetEffect(1).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200); // 200yd
                        break;
                    case 69198: // Raging Spirit Visual
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
                        break;
                    case 73655: // Harvest Soul
                        spellInfo.AttributesEx3 |= SpellAttr3.NoDoneBonus;
                        break;
                    case 73540: // Summon Shadow Trap
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(23); // 90 seconds
                        break;
                    case 73530: // Shadow Trap (visual)
                        spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(28); // 5 seconds
                        break;
                    case 74302: // Summon Spirit Bomb
                        spellInfo.MaxAffectedTargets = 2;
                        break;
                    case 73579: // Summon Spirit Bomb
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards25); // 25yd
                        break;
                    case 72376: // Raise Dead
                        spellInfo.MaxAffectedTargets = 3;
                        break;
                    case 71809: // Jump
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(3); // 20yd
                        spellInfo.GetEffect(0).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards25); // 25yd
                        break;
                    case 72405: // Broken Frostmourne
                        spellInfo.GetEffect(1).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200); // 200yd
                        break;
                    // ENDOF ICECROWN CITADEL SPELLS
                    //
                    // RUBY SANCTUM SPELLS
                    //
                    case 74799: // Soul Consumption
                        spellInfo.GetEffect(1).RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards12);
                        break;
                    case 75509: // Twilight Mending
                        spellInfo.AttributesEx6 |= SpellAttr6.CanTargetInvisible;
                        spellInfo.AttributesEx2 |= SpellAttr2.CanTargetNotInLos;
                        break;
                    case 75888: // Awaken Flames
                        spellInfo.AttributesEx |= SpellAttr1.CantTargetSelf;
                        break;
                    // ENDOF RUBY SANCTUM SPELLS
                    //
                    // EYE OF ETERNITY SPELLS
                    // All spells below work even without these changes. The LOS attribute is due to problem
                    // from collision between maps & gos with active destroyed state.
                    case 57473: // Arcane Storm bonus explicit visual spell
                    case 57431: // Summon Static Field
                    case 56091: // Flame Spike (Wyrmrest Skytalon)
                    case 56092: // Engulf in Flames (Wyrmrest Skytalon)
                    case 57090: // Revivify (Wyrmrest Skytalon)
                    case 57143: // Life Burst (Wyrmrest Skytalon)
                        spellInfo.AttributesEx2 |= SpellAttr2.CanTargetNotInLos;
                        break;
                    // This would never crit on retail and it has attribute for SPELL_ATTR3_NO_DONE_BONUS because is handled from player,
                    // until someone figures how to make scions not critting without hack and without making them main casters this should stay here.
                    case 63934: // Arcane Barrage (cast by players and NONMELEEDAMAGELOG with caster Scion of Eternity (original caster)).
                        spellInfo.AttributesEx2 |= SpellAttr2.CantCrit;
                        break;
                    // ENDOF EYE OF ETERNITY SPELLS
                    //
                    case 40055: // Introspection
                    case 40165: // Introspection
                    case 40166: // Introspection
                    case 40167: // Introspection
                        spellInfo.Attributes |= SpellAttr0.Negative1;
                        break;
                    // Stonecore spells
                    case 95284: // Teleport (from entrance to Slabhide)
                    case 95285: // Teleport (from Slabhide to entrance)
                        spellInfo.GetEffect(0).TargetB = new SpellImplicitTargetInfo(Targets.DestDb);
                        break;
                    // Halls Of Origination spells
                    // Temple Guardian Anhuur
                    case 76606: // Disable Beacon Beams L
                    case 76608: // Disable Beacon Beams R
                                // Little hack, Increase the radius so it can hit the Cave In Stalkers in the platform.
                        spellInfo.GetEffect(0).MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards45);
                        break;
                    case 24314: // Threatening Gaze
                        spellInfo.AuraInterruptFlags[0] |= (uint)(SpellAuraInterruptFlags.Cast | SpellAuraInterruptFlags.Move | SpellAuraInterruptFlags.Jump);
                        break;
                    case 783:  // Travel Form (dummy) - cannot be cast indoors.
                        spellInfo.Attributes |= SpellAttr0.OutdoorsOnly;
                        break;
                    case 5420: // Tree of Life (Passive)
                        spellInfo.Stances = 1 << ((int)ShapeShiftForm.TreeOfLife - 1);
                        break;
                    case 49376: // Feral Charge (Cat Form)
                        spellInfo.AttributesEx3 &= ~SpellAttr3.CantTriggerProc;
                        break;
                    case 96942:  // Gaze of Occu'thar
                        spellInfo.AttributesEx &= ~SpellAttr1.Channeled1;
                        break;
                    case 75610: // Evolution
                        spellInfo.MaxAffectedTargets = 1;
                        break;
                    case 75697: // Evolution
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.UnitSrcAreaEntry);
                        break;
                    // ISLE OF CONQUEST SPELLS
                    //
                    case 66551: // Teleport
                        spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
                        break;
                    // ENDOF ISLE OF CONQUEST SPELLS
                    //
                    // FIRELANDS SPELLS
                    // Torment Searcher
                    case 99253:
                        spellInfo.GetEffect(0).MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards15);
                        break;
                    // Torment Damage
                    case 99256:
                        spellInfo.Attributes |= SpellAttr0.Negative1;
                        break;
                    // Blaze of Glory
                    case 99252:
                        spellInfo.AuraInterruptFlags[0] |= (uint)SpellAuraInterruptFlags.ChangeMap;
                        break;
                    // ENDOF FIRELANDS SPELLS
                    case 102445: // Summon Master Li Fei
                        spellInfo.GetEffect(0).TargetA = new SpellImplicitTargetInfo(Targets.DestDb);
                        break;
                }
            }

            foreach (var spellInfo in mSpellInfoMap.Values)
            {
                foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null)
                        continue;
                    switch (effect.Effect)
                    {
                        case SpellEffectName.Charge:
                        case SpellEffectName.ChargeDest:
                        case SpellEffectName.Jump:
                        case SpellEffectName.JumpDest:
                        case SpellEffectName.LeapBack:
                            if (spellInfo.Speed == 0 && spellInfo.SpellFamilyName == 0)
                                spellInfo.Speed = MotionMaster.SPEED_CHARGE;
                            break;
                    }

                    if (effect.TargetA.GetSelectionCategory() == SpellTargetSelectionCategories.Cone || effect.TargetB.GetSelectionCategory() == SpellTargetSelectionCategories.Cone)
                        if (MathFunctions.fuzzyEq(spellInfo.ConeAngle, 0.0f))
                            spellInfo.ConeAngle = 90.0f;
                }

                if (spellInfo.ActiveIconFileDataId == 135754)  // flight
                    spellInfo.Attributes |= SpellAttr0.Passive;
            }

            SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(121);
            if (properties != null)
                properties.Title = SummonType.Totem;
            properties = CliDB.SummonPropertiesStorage.LookupByKey(647); // 52893
            if (properties != null)
                properties.Title = SummonType.Totem;
            properties = CliDB.SummonPropertiesStorage.LookupByKey(628);
            if (properties != null) // Hungry Plaguehound
                properties.Control = SummonCategory.Pet;

            Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo corrections in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellInfoSpellSpecificAndAuraState()
        {
            uint oldMSTime = Time.GetMSTime();

            foreach (SpellInfo spellInfo in mSpellInfoMap.Values)
            {
                // AuraState depends on SpellSpecific
                spellInfo._LoadSpellSpecific();
                spellInfo._LoadAuraState();
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded SpellInfo SpellSpecific and AuraState in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void LoadSpellInfoDiminishing()
        {
            uint oldMSTime = Time.GetMSTime();

            foreach (SpellInfo spellInfo in mSpellInfoMap.Values)
            {
                if (spellInfo == null)
                    continue;

                spellInfo._LoadSpellDiminishInfo();
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo diminishing infos in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSpellInfoImmunities()
        {
            uint oldMSTime = Time.GetMSTime();

            foreach (SpellInfo spellInfo in mSpellInfoMap.Values)
            {
                if (spellInfo == null)
                    continue;

                spellInfo._LoadImmunityInfo();
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo immunity infos in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadPetFamilySpellsStore()
        {
            Dictionary<uint, SpellLevelsRecord> levelsBySpell = new Dictionary<uint, SpellLevelsRecord>();
            foreach (SpellLevelsRecord levels in CliDB.SpellLevelsStorage.Values)
                if (levels.DifficultyID == 0)
                    levelsBySpell[levels.SpellID] = levels;

            foreach (var skillLine in CliDB.SkillLineAbilityStorage.Values)
            {
                SpellInfo spellInfo = GetSpellInfo(skillLine.Spell);
                if (spellInfo == null)
                    continue;

                var levels = levelsBySpell.LookupByKey(skillLine.Spell);
                if (levels != null && levels.SpellLevel != 0)
                    continue;

                if (spellInfo.IsPassive())
                {
                    foreach (CreatureFamilyRecord cFamily in CliDB.CreatureFamilyStorage.Values)
                    {
                        if (skillLine.SkillLine != cFamily.SkillLine[0] && skillLine.SkillLine != cFamily.SkillLine[1])
                            continue;

                        if (skillLine.AcquireMethod != AbilityLearnType.OnSkillLearn)
                            continue;

                        Global.SpellMgr.PetFamilySpellsStorage.Add(cFamily.Id, spellInfo.Id);
                    }
                }
            }
        }

        public void LoadSpellTotemModel()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.World.Query("SELECT SpellID, RaceID, DisplayID from spell_totem_model");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell totem model records. DB table `spell_totem_model` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint spellId = result.Read<uint>(0);
                byte race = result.Read<byte>(1);
                uint displayId = result.Read<uint>(2);

                SpellInfo spellEntry = GetSpellInfo(spellId);
                if (spellEntry == null)
                {
                    Log.outError(LogFilter.Sql, $"SpellID: {spellId} in `spell_totem_model` table could not be found in dbc, skipped.");
                    continue;
                }

                if (!CliDB.ChrRacesStorage.ContainsKey(race))
                {
                    Log.outError(LogFilter.Sql, $"Race {race} defined in `spell_totem_model` does not exists, skipped.");
                    continue;
                }

                if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
                {
                    Log.outError(LogFilter.Sql, $"SpellID: {spellId} defined in `spell_totem_model` has non-existing model ({displayId}).");
                    continue;
                }

                mSpellTotemModel[Tuple.Create(spellId, race)] = displayId;
                ++count;

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} spell totem model records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

        }
        #endregion

        bool isTriggerAura(AuraType type)
        {
            switch (type)
            {
                case AuraType.Dummy:
                case AuraType.ModConfuse:
                case AuraType.ModThreat:
                case AuraType.ModStun:
                case AuraType.ModDamageDone:
                case AuraType.ModDamageTaken:
                case AuraType.ModResistance:
                case AuraType.ModStealth:
                case AuraType.ModFear:
                case AuraType.ModRoot:
                case AuraType.Transform:
                case AuraType.ReflectSpells:
                case AuraType.DamageImmunity:
                case AuraType.ProcTriggerSpell:
                case AuraType.ProcTriggerDamage:
                case AuraType.ModCastingSpeedNotStack:
                case AuraType.SchoolAbsorb:
                case AuraType.ModPowerCostSchoolPct:
                case AuraType.ModPowerCostSchool:
                case AuraType.ReflectSpellsSchool:
                case AuraType.MechanicImmunity:
                case AuraType.ModDamagePercentTaken:
                case AuraType.SpellMagnet:
                case AuraType.ModAttackPower:
                case AuraType.ModPowerRegenPercent:
                case AuraType.InterceptMeleeRangedAttacks:
                case AuraType.OverrideClassScripts:
                case AuraType.ModMechanicResistance:
                case AuraType.MeleeAttackPowerAttackerBonus:
                case AuraType.ModMeleeHaste:
                case AuraType.ModMeleeHaste3:
                case AuraType.ModAttackerMeleeHitChance:
                case AuraType.ProcTriggerSpellWithValue:
                case AuraType.ModSpellDamageFromCaster:
                case AuraType.AbilityIgnoreAurastate:
                case AuraType.ModInvisibility:
                case AuraType.ForceReaction:
                case AuraType.ModTaunt:
                case AuraType.ModDetaunt:
                case AuraType.ModDamagePercentDone:
                case AuraType.ModAttackPowerPct:
                case AuraType.ModHitChance:
                case AuraType.ModWeaponCritPercent:
                case AuraType.ModBlockPercent:
                case AuraType.ModRoot2:
                    return true;
            }
            return false;
        }
        bool isAlwaysTriggeredAura(AuraType type)
        {
            switch (type)
            {
                case AuraType.OverrideClassScripts:
                case AuraType.ModStealth:
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModRoot:
                case AuraType.ModStun:
                case AuraType.Transform:
                case AuraType.ModInvisibility:
                case AuraType.SpellMagnet:
                case AuraType.SchoolAbsorb:
                case AuraType.ModRoot2:
                    return true;
            }
            return false;
        }
        ProcFlagsSpellType getSpellTypeMask(AuraType type)
        {
            switch (type)
            {
                case AuraType.ModStealth:
                    return ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModRoot:
                case AuraType.ModRoot2:
                case AuraType.ModStun:
                case AuraType.Transform:
                case AuraType.ModInvisibility:
                    return ProcFlagsSpellType.Damage;
                default:
                    return ProcFlagsSpellType.MaskAll;
            }
        }

        // SpellInfo object management
        public SpellInfo GetSpellInfo(uint spellId)
        {
            return mSpellInfoMap.LookupByKey(spellId);
        }
        public bool HasSpellInfo(uint spellId)
        {
            return mSpellInfoMap.ContainsKey(spellId);
        }
        public Dictionary<uint, SpellInfo> GetSpellInfoStorage()
        {
            return mSpellInfoMap;
        }

        //Extra Shit
        public SpellEffectHandler GetSpellEffectHandler(SpellEffectName eff)
        {
            if (!SpellEffectsHandlers.ContainsKey(eff))
            {
                Log.outError(LogFilter.Spells, "No defined handler for SpellEffect {0}", eff);
                return SpellEffectsHandlers[SpellEffectName.Null];
            }

            return SpellEffectsHandlers[eff];
        }

        public AuraEffectHandler GetAuraEffectHandler(AuraType type)
        {
            if (!AuraEffectHandlers.ContainsKey(type))
            {
                Log.outError(LogFilter.Spells, "No defined handler for AuraEffect {0}", type);
                return AuraEffectHandlers[AuraType.None];
            }

            return AuraEffectHandlers[type];
        }

        public SkillRangeType GetSkillRangeType(SkillRaceClassInfoRecord rcEntry)
        {
            SkillLineRecord skill = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);
            if (skill == null)
                return SkillRangeType.None;

            if (Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID) != null)
                return SkillRangeType.Rank;

            if (rcEntry.SkillID == (uint)SkillType.Runeforging)
                return SkillRangeType.Mono;

            switch (skill.CategoryID)
            {
                case SkillCategory.Armor:
                    return SkillRangeType.Mono;
                case SkillCategory.Languages:
                    return SkillRangeType.Language;
            }
            return SkillRangeType.Level;
        }

        public bool IsPrimaryProfessionSkill(uint skill)
        {
            SkillLineRecord pSkill = CliDB.SkillLineStorage.LookupByKey(skill);
            return pSkill != null && pSkill.CategoryID == SkillCategory.Profession;
        }

        public bool IsWeaponSkill(uint skill)
        {
            var pSkill = CliDB.SkillLineStorage.LookupByKey(skill);
            return pSkill != null && pSkill.CategoryID == SkillCategory.Weapon;
        }

        public bool IsProfessionOrRidingSkill(uint skill)
        {
            return IsProfessionSkill(skill) || skill == (uint)SkillType.Riding;
        }

        public bool IsProfessionSkill(uint skill)
        {
            return IsPrimaryProfessionSkill(skill) || skill == (uint)SkillType.Fishing || skill == (uint)SkillType.Cooking;
        }

        public bool IsPartOfSkillLine(SkillType skillId, uint spellId)
        {
            var skillBounds = GetSkillLineAbilityMapBounds(spellId);
            if (skillBounds != null)
            {
                foreach (var skill in skillBounds)
                    if (skill.SkillLine == (uint)skillId)
                        return true;
            }

            return false;
        }

        public SpellSchools GetFirstSchoolInMask(SpellSchoolMask mask)
        {
            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean((int)mask & (1 << i)))
                    return (SpellSchools)i;

            return SpellSchools.Normal;
        }

        public uint GetModelForTotem(uint spellId, Race race)
        {
            return mSpellTotemModel.LookupByKey(Tuple.Create(spellId, (byte)race));
        }

        #region Fields
        //private:
        Dictionary<uint, SpellChainNode> mSpellChains = new Dictionary<uint, SpellChainNode>();
        MultiMap<uint, uint> mSpellsReqSpell = new MultiMap<uint, uint>();
        MultiMap<uint, uint> mSpellReq = new MultiMap<uint, uint>();
        Dictionary<uint, SpellLearnSkillNode> mSpellLearnSkills = new Dictionary<uint, SpellLearnSkillNode>();
        MultiMap<uint, SpellLearnSpellNode> mSpellLearnSpells = new MultiMap<uint, SpellLearnSpellNode>();
        Dictionary<KeyValuePair<uint, uint>, SpellTargetPosition> mSpellTargetPositions = new Dictionary<KeyValuePair<uint, uint>, SpellTargetPosition>();
        MultiMap<uint, SpellGroup> mSpellSpellGroup = new MultiMap<uint, SpellGroup>();
        MultiMap<SpellGroup, int> mSpellGroupSpell = new MultiMap<SpellGroup, int>();
        Dictionary<SpellGroup, SpellGroupStackRule> mSpellGroupStack = new Dictionary<SpellGroup, SpellGroupStackRule>();
        Dictionary<uint, SpellProcEntry> mSpellProcMap = new Dictionary<uint, SpellProcEntry>();
        Dictionary<uint, SpellThreatEntry> mSpellThreatMap = new Dictionary<uint, SpellThreatEntry>();
        Dictionary<uint, PetAura> mSpellPetAuraMap = new Dictionary<uint, PetAura>();
        MultiMap<int, int> mSpellLinkedMap = new MultiMap<int, int>();
        Dictionary<uint, SpellEnchantProcEntry> mSpellEnchantProcEventMap = new Dictionary<uint, SpellEnchantProcEntry>();
        Dictionary<int, bool> mEnchantCustomAttr = new Dictionary<int, bool>();
        MultiMap<uint, SpellArea> mSpellAreaMap = new MultiMap<uint, SpellArea>();
        MultiMap<uint, SpellArea> mSpellAreaForQuestMap = new MultiMap<uint, SpellArea>();
        MultiMap<uint, SpellArea> mSpellAreaForQuestEndMap = new MultiMap<uint, SpellArea>();
        MultiMap<uint, SpellArea> mSpellAreaForAuraMap = new MultiMap<uint, SpellArea>();
        MultiMap<uint, SpellArea> mSpellAreaForAreaMap = new MultiMap<uint, SpellArea>();
        MultiMap<Tuple<uint, uint>, SpellArea> mSpellAreaForQuestAreaMap = new MultiMap<Tuple<uint, uint>, SpellArea>();
        MultiMap<uint, SkillLineAbilityRecord> mSkillLineAbilityMap = new MultiMap<uint, SkillLineAbilityRecord>();
        Dictionary<uint, MultiMap<uint, uint>> mPetLevelupSpellMap = new Dictionary<uint, MultiMap<uint, uint>>();
        Dictionary<uint, PetDefaultSpellsEntry> mPetDefaultSpellsMap = new Dictionary<uint, PetDefaultSpellsEntry>();           // only spells not listed in related mPetLevelupSpellMap entry
        Dictionary<uint, SpellInfo> mSpellInfoMap = new Dictionary<uint, SpellInfo>();
        Dictionary<Tuple<uint, byte>, uint> mSpellTotemModel = new Dictionary<Tuple<uint, byte>, uint>();

        public delegate void AuraEffectHandler(AuraEffect effect, AuraApplication aurApp, AuraEffectHandleModes mode, bool apply);
        Dictionary<AuraType, AuraEffectHandler> AuraEffectHandlers = new Dictionary<AuraType, AuraEffectHandler>();
        public delegate void SpellEffectHandler(Spell spell, uint effectIndex);
        Dictionary<SpellEffectName, SpellEffectHandler> SpellEffectsHandlers = new Dictionary<SpellEffectName, SpellEffectHandler>();

        public MultiMap<uint, uint> PetFamilySpellsStorage = new MultiMap<uint, uint>();
        #endregion
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AuraEffectHandlerAttribute : Attribute
    {
        public AuraEffectHandlerAttribute(AuraType type)
        {
            auraType = type;
        }

        public AuraType auraType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SpellEffectHandlerAttribute : Attribute
    {
        public SpellEffectHandlerAttribute(SpellEffectName effectName)
        {
            this.effectName = effectName;
        }

        public SpellEffectName effectName { get; set; }
    }

    public class SpellInfoLoadHelper
    {
        public SpellNameRecord Entry;

        public SpellAuraOptionsRecord AuraOptions;
        public SpellAuraRestrictionsRecord AuraRestrictions;
        public SpellCastingRequirementsRecord CastingRequirements;
        public SpellCategoriesRecord Categories;
        public SpellClassOptionsRecord ClassOptions;
        public SpellCooldownsRecord Cooldowns;
        public SpellEquippedItemsRecord EquippedItems;
        public SpellInterruptsRecord Interrupts;
        public SpellLevelsRecord Levels;
        public SpellMiscRecord Misc;
        public SpellReagentsRecord Reagents;
        public SpellScalingRecord Scaling;
        public SpellShapeshiftRecord Shapeshift;
        public SpellTargetRestrictionsRecord TargetRestrictions;
        public SpellTotemsRecord Totems;
    }

    public class SpellThreatEntry
    {
        public int flatMod;                                    // flat threat-value for this Spell  - default: 0
        public float pctMod;                                     // threat-multiplier for this Spell  - default: 1.0f
        public float apPctMod;                                   // Pct of AP that is added as Threat - default: 0.0f
    }

    public class SpellProcEntry
    {
        public SpellSchoolMask SchoolMask { get; set; }                                 // if nonzero - bitmask for matching proc condition based on spell's school
        public SpellFamilyNames SpellFamilyName { get; set; }                            // if nonzero - for matching proc condition based on candidate spell's SpellFamilyName
        public FlagArray128 SpellFamilyMask { get; set; } = new FlagArray128();    // if nonzero - bitmask for matching proc condition based on candidate spell's SpellFamilyFlags
        public ProcFlags ProcFlags { get; set; }                                   // if nonzero - owerwrite procFlags field for given Spell.dbc entry, bitmask for matching proc condition, see enum ProcFlags
        public ProcFlagsSpellType SpellTypeMask { get; set; }                              // if nonzero - bitmask for matching proc condition based on candidate spell's damage/heal effects, see enum ProcFlagsSpellType
        public ProcFlagsSpellPhase SpellPhaseMask { get; set; }                             // if nonzero - bitmask for matching phase of a spellcast on which proc occurs, see enum ProcFlagsSpellPhase
        public ProcFlagsHit HitMask { get; set; }                                    // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
        public ProcAttributes AttributesMask { get; set; }                             // bitmask, see ProcAttributes
        public float ProcsPerMinute { get; set; }                              // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
        public float Chance { get; set; }                                     // if nonzero - owerwrite procChance field for given Spell.dbc entry, defines chance of proc to occur, not used if ProcsPerMinute set
        public uint Cooldown { get; set; }                                   // if nonzero - cooldown in secs for aura proc, applied to aura
        public uint Charges { get; set; }                                   // if nonzero - owerwrite procCharges field for given Spell.dbc entry, defines how many times proc can occur before aura remove, 0 - infinite
    }

    public class PetDefaultSpellsEntry
    {
        public uint[] spellid = new uint[4];
    }

    public class SpellArea
    {
        public uint spellId;
        public uint areaId;                                         // zone/subzone/or 0 is not limited to zone
        public uint questStart;                                     // quest start (quest must be active or rewarded for spell apply)
        public uint questEnd;                                       // quest end (quest must not be rewarded for spell apply)
        public int auraSpell;                                       // spell aura must be applied for spell apply)if possitive) and it must not be applied in other case
        public ulong raceMask;                                      // can be applied only to races
        public Gender gender;                                       // can be applied only to gender
        public uint questStartStatus;                               // QuestStatus that quest_start must have in order to keep the spell
        public uint questEndStatus;                                 // QuestStatus that the quest_end must have in order to keep the spell (if the quest_end's status is different than this, the spell will be dropped)
        public SpellAreaFlag flags;                                 // if SPELL_AREA_FLAG_AUTOCAST then auto applied at area enter, in other case just allowed to cast || if SPELL_AREA_FLAG_AUTOREMOVE then auto removed inside area (will allways be removed on leaved even without flag)

        // helpers
        public bool IsFitToRequirements(Player player, uint newZone, uint newArea)
        {
            if (gender != Gender.None)                   // not in expected gender
                if (player == null || gender != player.GetGender())
                    return false;

            if (raceMask != 0)                                // not in expected race
                if (player == null || !Convert.ToBoolean(raceMask & player.getRaceMask()))
                    return false;

            if (areaId != 0)                                  // not in expected zone
                if (newZone != areaId && newArea != areaId)
                    return false;

            if (questStart != 0)                              // not in expected required quest state
                if (player == null || (((1 << (int)player.GetQuestStatus(questStart)) & questStartStatus) == 0))
                    return false;

             if (questEnd != 0)                                // not in expected forbidden quest state
                 if (player == null || (((1 << (int)player.GetQuestStatus(questEnd)) & questEndStatus) == 0))
                     return false;

            if (auraSpell != 0)                               // not have expected aura
                if (player == null || (auraSpell > 0 && !player.HasAura((uint)auraSpell)) || (auraSpell < 0 && player.HasAura((uint)-auraSpell)))
                    return false;

            if (player)
            {
                Battleground bg = player.GetBattleground();
                if (bg)
                    return bg.IsSpellAllowed(spellId, player);
            }

            // Extra conditions -- leaving the possibility add extra conditions...
            switch (spellId)
            {
                case 91604: // No fly Zone - Wintergrasp
                    {
                        if (!player)
                            return false;

                        BattleField Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
                        if (Bf == null || Bf.CanFlyIn() || (!player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !player.HasAuraType(AuraType.Fly)))
                            return false;
                        break;
                    }
                case 56618: // Horde Controls Factory Phase Shift
                case 56617: // Alliance Controls Factory Phase Shift
                    {
                        if (!player)
                            return false;

                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());

                        if (bf == null || bf.GetTypeId() != (int)BattleFieldTypes.WinterGrasp)
                            return false;

                        // team that controls the workshop in the specified area
                        uint team = bf.GetData(newArea);

                        if (team == TeamId.Horde)
                            return spellId == 56618;
                        else if (team == TeamId.Alliance)
                            return spellId == 56617;
                        break;
                    }
                case 57940: // Essence of Wintergrasp - Northrend
                case 58045: // Essence of Wintergrasp - Wintergrasp
                    {
                        if (!player)
                            return false;
                        
                        BattleField battlefieldWG = Global.BattleFieldMgr.GetBattlefieldByBattleId(1);
                        if (battlefieldWG != null)
                            return battlefieldWG.IsEnabled() && (player.GetTeamId() == battlefieldWG.GetDefenderTeam()) && !battlefieldWG.IsWarTime();
                        break;
                    }
                case 74411: // Battleground- Dampening
                    {
                        if (!player)
                            return false;
                        
                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
                        if (bf != null)
                            return bf.IsWarTime();
                        break;
                    }
            }
            return true;
        }
    }

    public class PetAura
    {
        public PetAura()
        {
            removeOnChangePet = false;
            damage = 0;
        }

        public PetAura(uint petEntry, uint aura, bool _removeOnChangePet, int _damage)
        {
            removeOnChangePet = _removeOnChangePet;
            damage = _damage;

            auras[petEntry] = aura;
        }

        public uint GetAura(uint petEntry)
        {
            var auraId = auras.LookupByKey(petEntry);
            if (auraId != 0)
                return auraId;

            auraId = auras.LookupByKey(0);
            if (auraId != 0)
                return auraId;

            return 0;
        }

        public void AddAura(uint petEntry, uint aura)
        {
            auras[petEntry] = aura;
        }

        public bool IsRemovedOnChangePet()
        {
            return removeOnChangePet;
        }

        public int GetDamage()
        {
            return damage;
        }

        Dictionary<uint, uint> auras = new Dictionary<uint, uint>();
        bool removeOnChangePet;
        int damage;
    }

    public class SpellEnchantProcEntry
    {
        public uint customChance;
        public float PPMChance;
        public uint procEx;
    }

    public class SpellTargetPosition
    {
        public uint target_mapId;
        public float target_X;
        public float target_Y;
        public float target_Z;
        public float target_Orientation;
    }
}
