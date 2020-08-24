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
using Game.Combat;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class UnitAI
    {
        public UnitAI(Unit _unit)
        {
            me = _unit;
        }

        public virtual void AttackStart(Unit victim)
        {
            if (victim != null && me.Attack(victim, true))
            {
                // Clear distracted state on attacking
                if (me.HasUnitState(UnitState.Distracted))
                {
                    me.ClearUnitState(UnitState.Distracted);
                    me.GetMotionMaster().Clear();
                }
                me.GetMotionMaster().MoveChase(victim);
            }
        }

        public void AttackStartCaster(Unit victim, float dist)
        {
            if (victim != null && me.Attack(victim, false))
                me.GetMotionMaster().MoveChase(victim, dist);
        }

        ThreatManager GetThreatManager()
        {
            return me.GetThreatManager();
        }

        void SortByDistance(List<Unit> targets, bool ascending)
        {
            targets.Sort(new ObjectDistanceOrderPred(me, true));
        }

        public void DoMeleeAttackIfReady()
        {
            if (me.HasUnitState(UnitState.Casting))
                return;

            Unit victim = me.GetVictim();

            if (!me.IsWithinMeleeRange(victim))
                return;

            //Make sure our attack is ready and we aren't currently casting before checking distance
            if (me.IsAttackReady())
            {
                if (ShouldSparWith(victim))
                    me.FakeAttackerStateUpdate(victim);
                else
                    me.AttackerStateUpdate(victim);

                me.ResetAttackTimer();
            }

            if (me.HaveOffhandWeapon() && me.IsAttackReady(WeaponAttackType.OffAttack))
            {
                if (ShouldSparWith(victim))
                    me.FakeAttackerStateUpdate(victim, WeaponAttackType.OffAttack);
                else
                    me.AttackerStateUpdate(victim, WeaponAttackType.OffAttack);

                me.ResetAttackTimer(WeaponAttackType.OffAttack);
            }
        }

        public bool DoSpellAttackIfReady(uint spell)
        {
            if (me.HasUnitState(UnitState.Casting) || !me.IsAttackReady())
                return true;

            var spellInfo = Global.SpellMgr.GetSpellInfo(spell, me.GetMap().GetDifficultyID());
            if (spellInfo != null)
            {
                if (me.IsWithinCombatRange(me.GetVictim(), spellInfo.GetMaxRange(false)))
                {
                    me.CastSpell(me.GetVictim(), spellInfo, TriggerCastFlags.None);
                    me.ResetAttackTimer();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Select the best target (in <targetType> order) from the threat list that fulfill the following:
        /// - Not among the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM).
        /// - Within at most <dist> yards (if dist > 0.0f)
        /// - At least -<dist> yards away (if dist < 0.0f)
        /// - Is a player (if playerOnly = true)
        /// - Not the current tank (if withTank = false)
        /// - Has aura with ID <aura> (if aura > 0)
        /// - Does not have aura with ID -<aura> (if aura < 0)
        /// </summary>
        public Unit SelectTarget(SelectAggroTarget targetType, uint offset = 0, float dist = 0.0f, bool playerOnly = false, bool withTank = true, int aura = 0)
        {
            return SelectTarget(targetType, offset, new DefaultTargetSelector(me, dist, playerOnly, withTank, aura));
        }

        /// <summary>
        /// Select the best target (in <targetType> order) satisfying <predicate> from the threat list.
        /// If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
        /// </summary>
        public Unit SelectTarget(SelectAggroTarget targetType, uint offset, ISelector selector)
        {
            ThreatManager mgr = GetThreatManager();
            // shortcut: if we ignore the first <offset> elements, and there are at most <offset> elements, then we ignore ALL elements
            if (mgr.GetThreatListSize() <= offset)
                return null;

            List<Unit> targetList = SelectTargetList((uint)mgr.GetThreatListSize(), targetType, offset, selector);

            // maybe nothing fulfills the predicate
            if (targetList.Empty())
                return null;

            switch (targetType)
            {
                case SelectAggroTarget.MaxThreat:
                case SelectAggroTarget.MinThreat:
                case SelectAggroTarget.MaxDistance:
                case SelectAggroTarget.MinDistance:
                    return targetList[0];
                case SelectAggroTarget.Random:
                        return targetList.SelectRandom();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Select the best (up to) <num> targets (in <targetType> order) from the threat list that fulfill the following:
        /// - Not among the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM).
        /// - Within at most <dist> yards (if dist > 0.0f)
        /// - At least -<dist> yards away (if dist < 0.0f)
        /// - Is a player (if playerOnly = true)
        /// - Not the current tank (if withTank = false)
        /// - Has aura with ID <aura> (if aura > 0)
        /// - Does not have aura with ID -<aura> (if aura < 0)
        /// The resulting targets are stored in <targetList> (which is cleared first).
        /// </summary>
        public List<Unit> SelectTargetList(uint num, SelectAggroTarget targetType, uint offset, float dist, bool playerOnly, bool withTank, int aura = 0)
        {
            return SelectTargetList(num, targetType, offset, new DefaultTargetSelector(me, dist, playerOnly, withTank, aura));
        }

        /// <summary>
        /// Select the best (up to) <num> targets (in <targetType> order) satisfying <predicate> from the threat list and stores them in <targetList> (which is cleared first).
        /// If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
        /// </summary>
        public List<Unit> SelectTargetList(uint num, SelectAggroTarget targetType, uint offset, ISelector selector)
        {
            var targetList = new List<Unit>();

            ThreatManager mgr = GetThreatManager();
            // shortcut: we're gonna ignore the first <offset> elements, and there's at most <offset> elements, so we ignore them all - nothing to do here
            if (mgr.GetThreatListSize() <= offset)
                return targetList;

            if (targetType == SelectAggroTarget.MaxDistance || targetType == SelectAggroTarget.MinDistance)
            {
                foreach (HostileReference refe in mgr.GetThreatList())
                {
                    if (!refe.IsOnline())
                        continue;

                    targetList.Add(refe.GetTarget());
                }
            }
            else
            {
                Unit currentVictim = mgr.GetCurrentVictim();
                if (currentVictim != null)
                    targetList.Add(currentVictim);

                foreach (HostileReference refe in mgr.GetThreatList())
                {
                    if (!refe.IsOnline())
                        continue;

                    Unit thisTarget = refe.GetTarget();
                    if (thisTarget != currentVictim)
                        targetList.Add(thisTarget);
                }
            }

            // shortcut: the list isn't gonna get any larger
            if (targetList.Count <= offset)
            {
                targetList.Clear();
                return targetList;
            }

            // right now, list is unsorted for DISTANCE types - re-sort by MAXDISTANCE
            if (targetType == SelectAggroTarget.MaxDistance || targetType == SelectAggroTarget.MinDistance)
                SortByDistance(targetList, targetType == SelectAggroTarget.MinDistance);

            // now the list is MAX sorted, reverse for MIN types
            if (targetType == SelectAggroTarget.MinThreat)
                targetList.Reverse();

            // ignore the first <offset> elements
            while (offset != 0)
            {
                targetList.RemoveAt(0);
                --offset;
            }

            // then finally filter by predicate
            targetList.RemoveAll(target => { return !selector.Check(target); });

            if (targetList.Count <= num)
                return targetList;

            if (targetType == SelectAggroTarget.Random)
                targetList = targetList.SelectRandom(num).ToList();
            else
                targetList.Resize(num);

            return targetList;
        }

        public void DoCast(uint spellId)
        {
            Unit target = null;

            AISpellInfoType info = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());
            switch (info.target)
            {
                default:
                case AITarget.Self:
                    target = me;
                    break;
                case AITarget.Victim:
                    target = me.GetVictim();
                    break;
                case AITarget.Enemy:
                    {
                        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, me.GetMap().GetDifficultyID());
                        if (spellInfo != null)
                        {
                            bool playerOnly = spellInfo.HasAttribute(SpellAttr3.OnlyTargetPlayers);
                            target = SelectTarget(SelectAggroTarget.Random, 0, spellInfo.GetMaxRange(false), playerOnly);
                        }
                        break;
                    }
                case AITarget.Ally:
                case AITarget.Buff:
                    target = me;
                    break;
                case AITarget.Debuff:
                    {
                        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, me.GetMap().GetDifficultyID());
                        if (spellInfo != null)
                        {
                            bool playerOnly = spellInfo.HasAttribute(SpellAttr3.OnlyTargetPlayers);
                            float range = spellInfo.GetMaxRange(false);

                            DefaultTargetSelector targetSelector = new DefaultTargetSelector(me, range, playerOnly, true, -(int)spellId);
                            if (!spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.NotVictim)
                            && targetSelector.Check(me.GetVictim()))
                                target = me.GetVictim();
                            else
                                target = SelectTarget(SelectAggroTarget.Random, 0, targetSelector);
                        }
                        break;
                    }
            }

            if (target != null)
                me.CastSpell(target, spellId, false);
        }

        public void DoCast(Unit victim, uint spellId, bool triggered = false)
        {
            if (victim == null || (me.HasUnitState(UnitState.Casting) && !triggered))
                return;

            me.CastSpell(victim, spellId, triggered);
        }

        public void DoCastSelf(uint spellId, bool triggered = false) { DoCast(me, spellId, triggered); }

        public void DoCastVictim(uint spellId, bool triggered = false)
        {
            if (me.GetVictim() == null || (me.HasUnitState(UnitState.Casting) && !triggered))
                return;

            me.CastSpell(me.GetVictim(), spellId, triggered);
        }

        public void DoCastAOE(uint spellId, bool triggered = false)
        {
            if (!triggered && me.HasUnitState(UnitState.Casting))
                return;

            me.CastSpell((Unit)null, spellId, triggered);
        }

        public static void FillAISpellInfo()
        {
            //AISpellInfo = new AISpellInfoType[spellStorage.Keys.Max() + 1];

            Global.SpellMgr.ForEachSpellInfo(spellInfo =>
            {
                AISpellInfoType AIInfo = new AISpellInfoType(); 
                if (spellInfo.HasAttribute(SpellAttr0.CastableWhileDead))
                    AIInfo.condition = AICondition.Die;
                else if (spellInfo.IsPassive() || spellInfo.GetDuration() == -1)
                    AIInfo.condition = AICondition.Aggro;
                else
                    AIInfo.condition = AICondition.Combat;

                if (AIInfo.cooldown < spellInfo.RecoveryTime)
                    AIInfo.cooldown = spellInfo.RecoveryTime;

                if (spellInfo.GetMaxRange(false) == 0)
                {
                    if (AIInfo.target < AITarget.Self)
                        AIInfo.target = AITarget.Self;
                }
                else
                {
                    foreach (SpellEffectInfo effect in spellInfo.GetEffects())
                    {
                        if (effect == null)
                            continue;

                        var targetType = effect.TargetA.GetTarget();

                        if (targetType == Targets.UnitTargetEnemy || targetType == Targets.DestTargetEnemy)
                        {
                            if (AIInfo.target < AITarget.Victim)
                                AIInfo.target = AITarget.Victim;
                        }
                        else if (targetType == Targets.UnitDestAreaEnemy)
                        {
                            if (AIInfo.target < AITarget.Enemy)
                                AIInfo.target = AITarget.Enemy;
                        }

                        if (effect.Effect == SpellEffectName.ApplyAura)
                        {
                            if (targetType == Targets.UnitTargetEnemy)
                            {
                                if (AIInfo.target < AITarget.Debuff)
                                    AIInfo.target = AITarget.Debuff;
                            }
                            else if (spellInfo.IsPositive())
                            {
                                if (AIInfo.target < AITarget.Buff)
                                    AIInfo.target = AITarget.Buff;
                            }
                        }
                    }
                }
                AIInfo.realCooldown = spellInfo.RecoveryTime + spellInfo.StartRecoveryTime;
                AIInfo.maxRange = spellInfo.GetMaxRange(false) * 3 / 4;

                AIInfo.Effects = 0;
                AIInfo.Targets = 0;

                foreach (SpellEffectInfo effect in spellInfo.GetEffects())
                {
                    if (effect == null)
                        continue;

                    // Spell targets self.
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.Self - 1);

                    // Spell targets a single enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitTargetEnemy ||
                        effect.TargetA.GetTarget() == Targets.DestTargetEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleEnemy - 1);

                    // Spell targets AoE at enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster ||
                        effect.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeEnemy - 1);

                    // Spell targets an enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitTargetEnemy ||
                        effect.TargetA.GetTarget() == Targets.DestTargetEnemy ||
                        effect.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster ||
                        effect.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyEnemy - 1);

                    // Spell targets a single friend (or self).
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster ||
                        effect.TargetA.GetTarget() == Targets.UnitTargetAlly ||
                        effect.TargetA.GetTarget() == Targets.UnitTargetParty)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleFriend - 1);

                    // Spell targets AoE friends.
                    if (effect.TargetA.GetTarget() == Targets.UnitCasterAreaParty ||
                        effect.TargetA.GetTarget() == Targets.UnitLastTargetAreaParty ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeFriend - 1);

                    // Spell targets any friend (or self).
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster ||
                        effect.TargetA.GetTarget() == Targets.UnitTargetAlly ||
                        effect.TargetA.GetTarget() == Targets.UnitTargetParty ||
                        effect.TargetA.GetTarget() == Targets.UnitCasterAreaParty ||
                        effect.TargetA.GetTarget() == Targets.UnitLastTargetAreaParty ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyFriend - 1);

                    // Make sure that this spell includes a damage effect.
                    if (effect.Effect == SpellEffectName.SchoolDamage ||
                        effect.Effect == SpellEffectName.Instakill ||
                        effect.Effect == SpellEffectName.EnvironmentalDamage ||
                        effect.Effect == SpellEffectName.HealthLeech)
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Damage - 1);

                    // Make sure that this spell includes a healing effect (or an apply aura with a periodic heal).
                    if (effect.Effect == SpellEffectName.Heal ||
                        effect.Effect == SpellEffectName.HealMaxHealth ||
                        effect.Effect == SpellEffectName.HealMechanical ||
                        (effect.Effect == SpellEffectName.ApplyAura && effect.ApplyAuraName == AuraType.PeriodicHeal))
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Healing - 1);

                    // Make sure that this spell applies an aura.
                    if (effect.Effect == SpellEffectName.ApplyAura)
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Aura - 1);
                }

                AISpellInfo[(spellInfo.Id, spellInfo.Difficulty)] = AIInfo;
            });
        }

        public virtual bool CanAIAttack(Unit victim) { return true; }

        public virtual void UpdateAI(uint diff) { }

        public virtual void InitializeAI()
        {
            if (!me.IsDead()) 
                Reset();
        }
        
        public virtual void Reset() { }

        public virtual void OnCharmed(bool apply) { }

        public virtual bool ShouldSparWith(Unit target) { return false;   }
        
        public virtual void DoAction(int action) { }
        public virtual uint GetData(uint id = 0) { return 0; }
        public virtual void SetData(uint id, uint value) { }
        public virtual void SetGUID(ObjectGuid guid, int id = 0) { }
        public virtual ObjectGuid GetGUID(int id = 0) { return ObjectGuid.Empty; }

        public virtual void DamageDealt(Unit victim, ref uint damage, DamageEffectType damageType) { }
        public virtual void DamageTaken(Unit attacker, ref uint damage) { }
        public virtual void HealReceived(Unit by, uint addhealth) { }
        public virtual void HealDone(Unit to, uint addhealth) { }
        public virtual void SpellInterrupted(uint spellId, uint unTimeMs) {}

        /// <summary>
        /// Called when a player opens a gossip dialog with the creature.
        /// </summary>
        public virtual bool GossipHello(Player player) { return false; }

        /// <summary>
        /// Called when a player selects a gossip item in the creature's gossip menu.
        /// </summary>
        public virtual bool GossipSelect(Player player, uint menuId, uint gossipListId) { return false; }

        /// <summary>
        /// Called when a player selects a gossip with a code in the creature's gossip menu.
        /// </summary>
        public virtual bool GossipSelectCode(Player player, uint menuId, uint gossipListId, string code) { return false; }

        /// <summary>
        /// Called when a player accepts a quest from the creature.
        /// </summary>
        public virtual void QuestAccept(Player player, Quest quest) { }

        /// <summary>
        /// Called when a player completes a quest and is rewarded, opt is the selected item's index or 0
        /// </summary>
        public virtual void QuestReward(Player player, Quest quest, uint opt) { }

        /// <summary>
        /// Called when a game event starts or ends
        /// </summary>
        public virtual void OnGameEvent(bool start, ushort eventId) { }

        // Called when the dialog status between a player and the creature is requested.
        public virtual QuestGiverStatus GetDialogStatus(Player player) { return QuestGiverStatus.ScriptedNoStatus; }

        public virtual void WaypointStarted(uint nodeId, uint pathId) { }

        public virtual void WaypointReached(uint nodeId, uint pathId) { }

        public AISpellInfoType GetAISpellInfo(uint spellId, Difficulty difficulty)
        {
            return AISpellInfo.LookupByKey((spellId, difficulty));
        }

        public static Dictionary<(uint id, Difficulty difficulty), AISpellInfoType> AISpellInfo = new Dictionary<(uint id, Difficulty difficulty), AISpellInfoType>();

        protected Unit me { get; private set; }
    }
    
    public enum SelectAggroTarget
    {
        Random = 0,  // just pick a random target
        MaxThreat,   // prefer targets higher in the threat list
        MinThreat,   // prefer targets lower in the threat list
        MaxDistance, // prefer targets further from us
        MinDistance  // prefer targets closer to us
    }

    public interface ISelector
    {
        bool Check(Unit target);
    }

    // default predicate function to select target based on distance, player and/or aura criteria
    public class DefaultTargetSelector : ISelector
    {
        Unit me;
        float m_dist;
        bool m_playerOnly;
        Unit except;
        int m_aura;

        /// <param name="unit">the reference unit</param>
        /// <param name="dist">if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit</param>
        /// <param name="playerOnly">self explaining</param>
        /// <param name="withTank">allow current tank to be selected</param>
        /// <param name="aura">if 0: ignored, if > 0: the target shall have the aura, if < 0, the target shall NOT have the aura</param>
        public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
        {
            me = unit;
            m_dist = dist;
            m_playerOnly = playerOnly;
            except = !withTank ? me.GetThreatManager().GetCurrentVictim() : null;
            m_aura = aura;
        }

        public bool Check(Unit target)
        {
            if (me == null)
                return false;

            if (target == null)
                return false;

            if (except != null && target == except)
                return false;

            if (m_playerOnly && !target.IsTypeId(TypeId.Player))
                return false;

            if (m_dist > 0.0f && !me.IsWithinCombatRange(target, m_dist))
                return false;

            if (m_dist < 0.0f && me.IsWithinCombatRange(target, -m_dist))
                return false;

            if (m_aura != 0)
            {
                if (m_aura > 0)
                {
                    if (!target.HasAura((uint)m_aura))
                        return false;
                }
                else
                {
                    if (target.HasAura((uint)-m_aura))
                        return false;
                }
            }

            return true;
        }
    }

    // Target selector for spell casts checking range, auras and attributes
    // todo Add more checks from Spell.CheckCast
    public class SpellTargetSelector : ISelector
    {
        public SpellTargetSelector(Unit caster, uint spellId)
        {
            _caster = caster;
            _spellInfo = Global.SpellMgr.GetSpellInfo(spellId, caster.GetMap().GetDifficultyID());

            Cypher.Assert(_spellInfo != null);
        }

        public bool Check(Unit target)
        {
            if (target == null)
                return false;

            if (_spellInfo.CheckTarget(_caster, target) != SpellCastResult.SpellCastOk)
                return false;

            // copypasta from Spell.CheckRange
            float minRange = 0.0f;
            float maxRange = 0.0f;
            float rangeMod = 0.0f;
            if (_spellInfo.RangeEntry != null)
            {
                if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
                {
                    rangeMod = _caster.GetCombatReach() + 4.0f / 3.0f;
                    rangeMod += target.GetCombatReach();

                    rangeMod = Math.Max(rangeMod, SharedConst.NominalMeleeRange);
                }
                else
                {
                    float meleeRange = 0.0f;
                    if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                    {
                        meleeRange = _caster.GetCombatReach() + 4.0f / 3.0f;
                        meleeRange += target.GetCombatReach();

                        meleeRange = Math.Max(meleeRange, SharedConst.NominalMeleeRange);
                    }

                    minRange = _caster.GetSpellMinRangeForTarget(target, _spellInfo) + meleeRange;
                    maxRange = _caster.GetSpellMaxRangeForTarget(target, _spellInfo);

                    rangeMod = _caster.GetCombatReach();
                    rangeMod += target.GetCombatReach();

                    if (minRange > 0.0f && !_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                        minRange += rangeMod;
                }

                if (_caster.IsMoving() && target.IsMoving() && !_caster.IsWalking() && !target.IsWalking() &&
                    (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee) || target.IsTypeId(TypeId.Player)))
                    rangeMod += 8.0f / 3.0f;
            }

            maxRange += rangeMod;

            minRange *= minRange;
            maxRange *= maxRange;

            if (target != _caster)
            {
                if (_caster.GetExactDistSq(target) > maxRange)
                    return false;

                if (minRange > 0.0f && _caster.GetExactDistSq(target) < minRange)
                    return false;
            }

            return true;
        }

        Unit _caster;
        SpellInfo _spellInfo;
    }

    // Very simple target selector, will just skip main target
    // NOTE: When passing to UnitAI.SelectTarget remember to use 0 as position for random selection
    //       because tank will not be in the temporary list
    public class NonTankTargetSelector : ISelector
    {
        public NonTankTargetSelector(Unit source, bool playerOnly = true)
        {
            _source = source;
            _playerOnly = playerOnly;
        }

        public bool Check(Unit target)
        {
            if (target == null)
                return false;

            if (_playerOnly && !target.IsTypeId(TypeId.Player))
                return false;

            Unit currentVictim = _source.GetThreatManager().GetCurrentVictim();
            if (currentVictim != null)
                return target != currentVictim;

            return target != _source.GetVictim();
        }

        Unit _source;
        bool _playerOnly;
    }

    // Simple selector for units using mana
    class PowerUsersSelector : ISelector
    {
        public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
        {
            _me = unit;
            _power = power;
            _dist = dist;
            _playerOnly = playerOnly;
        }

        public bool Check(Unit target)
        {
            if (_me == null || target == null)
                return false;

            if (target.GetPowerType() != _power)
                return false;

            if (_playerOnly && target.GetTypeId() != TypeId.Player)
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
                return false;

            return true;
        }

        Unit _me;
        PowerType _power;
        float _dist;
        bool _playerOnly;
    }

    class FarthestTargetSelector : ISelector
    {
        public FarthestTargetSelector(Unit unit, float dist, bool playerOnly, bool inLos)
        {
            _me = unit;
            _dist = dist;
            _playerOnly = playerOnly;
            _inLos = inLos;
        }

        public bool Check(Unit target)
        {
            if (_me == null || target == null)
                return false;

            if (_playerOnly && target.GetTypeId() != TypeId.Player)
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_inLos && !_me.IsWithinLOSInMap(target))
                return false;

            return true;
        }

        Unit _me;
        float _dist;
        bool _playerOnly;
        bool _inLos;
    }
}
