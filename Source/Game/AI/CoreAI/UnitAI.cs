// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        static Dictionary<(uint id, Difficulty difficulty), AISpellInfoType> _aiSpellInfo = new();

        protected Unit me { get; private set; }

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
            targets.Sort(new ObjectDistanceOrderPred(me, ascending));
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
                me.AttackerStateUpdate(victim);
                me.ResetAttackTimer();
            }

            if (me.HaveOffhandWeapon() && me.IsAttackReady(WeaponAttackType.OffAttack))
            {
                me.AttackerStateUpdate(victim, WeaponAttackType.OffAttack);
                me.ResetAttackTimer(WeaponAttackType.OffAttack);
            }
        }

        public bool DoSpellAttackIfReady(uint spellId)
        {
            if (me.HasUnitState(UnitState.Casting) || !me.IsAttackReady())
                return true;

            var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, me.GetMap().GetDifficultyID());
            if (spellInfo != null)
            {
                if (me.IsWithinCombatRange(me.GetVictim(), spellInfo.GetMaxRange(false)))
                {
                    me.CastSpell(me.GetVictim(), spellId, new CastSpellExtraArgs(me.GetMap().GetDifficultyID()));
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
        public Unit SelectTarget(SelectTargetMethod targetType, uint offset = 0, float dist = 0.0f, bool playerOnly = false, bool withTank = true, int aura = 0)
        {
            return SelectTarget(targetType, offset, new DefaultTargetSelector(me, dist, playerOnly, withTank, aura));
        }

        public Unit SelectTarget(SelectTargetMethod targetType, uint offset, ICheck<Unit> selector)
        {
            return SelectTarget(targetType, offset, selector.Invoke);
        }

        /// <summary>
        /// Select the best target (in <targetType> order) satisfying <predicate> from the threat list.
        /// If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
        /// </summary>
        public delegate bool SelectTargetDelegate(Unit unit);
        public Unit SelectTarget(SelectTargetMethod targetType, uint offset, SelectTargetDelegate selector)
        {
            ThreatManager mgr = GetThreatManager();
            // shortcut: if we ignore the first <offset> elements, and there are at most <offset> elements, then we ignore ALL elements
            if (mgr.GetThreatListSize() <= offset)
                return null;

            List<Unit> targetList = SelectTargetList((uint)mgr.GetThreatListSize(), targetType, offset, selector);

            // maybe nothing fulfills the predicate
            if (targetList.Empty())
                return null;

            return targetType switch
            {
                SelectTargetMethod.MaxThreat or SelectTargetMethod.MinThreat or SelectTargetMethod.MaxDistance or SelectTargetMethod.MinDistance => targetList[0],
                SelectTargetMethod.Random => targetList.SelectRandom(),
                _ => null,
            };
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
        public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset = 0, float dist = 0f, bool playerOnly = false, bool withTank = true, int aura = 0)
        {
            return SelectTargetList(num, targetType, offset, new DefaultTargetSelector(me, dist, playerOnly, withTank, aura).Invoke);
        }

        /// <summary>
        /// Select the best (up to) <num> targets (in <targetType> order) satisfying <predicate> from the threat list and stores them in <targetList> (which is cleared first).
        /// If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
        /// </summary>
        public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset, SelectTargetDelegate selector)
        {
            var targetList = new List<Unit>();

            ThreatManager mgr = GetThreatManager();
            // shortcut: we're gonna ignore the first <offset> elements, and there's at most <offset> elements, so we ignore them all - nothing to do here
            if (mgr.GetThreatListSize() <= offset)
                return targetList;

            if (targetType == SelectTargetMethod.MaxDistance || targetType == SelectTargetMethod.MinDistance)
            {
                foreach (ThreatReference refe in mgr.GetSortedThreatList())
                {
                    if (!refe.IsOnline())
                        continue;

                    targetList.Add(refe.GetVictim());
                }
            }
            else
            {
                Unit currentVictim = mgr.GetCurrentVictim();
                if (currentVictim != null)
                    targetList.Add(currentVictim);

                foreach (ThreatReference refe in mgr.GetSortedThreatList())
                {
                    if (!refe.IsOnline())
                        continue;

                    Unit thisTarget = refe.GetVictim();
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
            if (targetType == SelectTargetMethod.MaxDistance || targetType == SelectTargetMethod.MinDistance)
                SortByDistance(targetList, targetType == SelectTargetMethod.MinDistance);

            // now the list is MAX sorted, reverse for MIN types
            if (targetType == SelectTargetMethod.MinThreat)
                targetList.Reverse();

            // ignore the first <offset> elements
            while (offset != 0)
            {
                targetList.RemoveAt(0);
                --offset;
            }

            // then finally filter by predicate
            targetList.RemoveAll(unit => !selector(unit));

            if (targetList.Count <= num)
                return targetList;

            if (targetType == SelectTargetMethod.Random)
                targetList = targetList.SelectRandom(num).ToList();
            else
                targetList.Resize(num);

            return targetList;
        }

        public SpellCastResult DoCast(uint spellId)
        {
            Unit target = null;
            AITarget aiTargetType = AITarget.Self;

            AISpellInfoType info = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());
            if (info != null)
                aiTargetType = info.target;

            switch (aiTargetType)
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
                        DefaultTargetSelector targetSelectorInner = new(me, spellInfo.GetMaxRange(false), false, true, 0);
                        bool targetSelector(Unit candidate)
                        {
                            if (!candidate.IsPlayer())
                            {
                                if (spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                                    return false;

                                if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && candidate.IsControlledByPlayer())
                                    return false;
                            }
                            else if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                                return false;

                            return targetSelectorInner.Invoke(candidate);
                        };
                        target = SelectTarget(SelectTargetMethod.Random, 0, targetSelector);
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
                        float range = spellInfo.GetMaxRange(false);

                        DefaultTargetSelector targetSelectorInner = new(me, range, false, true, -(int)spellId);
                        bool targetSelector(Unit candidate)
                        {
                            if (!candidate.IsPlayer())
                            {
                                if (spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                                    return false;

                                if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && candidate.IsControlledByPlayer())
                                    return false;
                            }
                            else if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                                return false;

                            return targetSelectorInner.Invoke(candidate);
                        };
                        if (!spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.NotVictim) && targetSelector(me.GetVictim()))
                            target = me.GetVictim();
                        else
                            target = SelectTarget(SelectTargetMethod.Random, 0, targetSelector);
                    }
                    break;
                }
            }

            if (target != null)
                return me.CastSpell(target, spellId, false);

            return SpellCastResult.BadTargets;
        }

        public SpellCastResult DoCast(Unit victim, uint spellId, CastSpellExtraArgs args = null)
        {
            args = args ?? new CastSpellExtraArgs();

            if (me.HasUnitState(UnitState.Casting) && !args.TriggerFlags.HasAnyFlag(TriggerCastFlags.IgnoreCastInProgress))
                return SpellCastResult.SpellInProgress;

            return me.CastSpell(victim, spellId, args);
        }

        public SpellCastResult DoCastSelf(uint spellId, CastSpellExtraArgs args = null) { return DoCast(me, spellId, args); }

        public SpellCastResult DoCastVictim(uint spellId, CastSpellExtraArgs args = null)
        {
            Unit victim = me.GetVictim();
            if (victim != null)
                return DoCast(victim, spellId, args);

            return SpellCastResult.BadTargets;
        }

        public SpellCastResult DoCastAOE(uint spellId, CastSpellExtraArgs args = null) { return DoCast(null, spellId, args); }

        public static void FillAISpellInfo()
        {
            Global.SpellMgr.ForEachSpellInfo(spellInfo =>
            {
                AISpellInfoType AIInfo = new();
                if (spellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead))
                    AIInfo.condition = AICondition.Die;
                else if (spellInfo.IsPassive() || spellInfo.GetDuration() == -1)
                    AIInfo.condition = AICondition.Aggro;
                else
                    AIInfo.condition = AICondition.Combat;

                if (AIInfo.cooldown.TotalMilliseconds < spellInfo.RecoveryTime)
                    AIInfo.cooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);

                if (spellInfo.GetMaxRange(false) != 0)
                {
                    foreach (var spellEffectInfo in spellInfo.GetEffects())
                    {
                        var targetType = spellEffectInfo.TargetA.GetTarget();
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

                        if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
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

                AIInfo.realCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime + spellInfo.StartRecoveryTime);
                AIInfo.maxRange = spellInfo.GetMaxRange(false) * 3 / 4;

                AIInfo.Effects = 0;
                AIInfo.Targets = 0;

                foreach (var spellEffectInfo in spellInfo.GetEffects())
                {
                    // Spell targets self.
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.Self - 1);

                    // Spell targets a single enemy.
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.DestTargetEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleEnemy - 1);

                    // Spell targets AoE at enemy.
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.SrcCaster ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeEnemy - 1);

                    // Spell targets an enemy.
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.DestTargetEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.SrcCaster ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyEnemy - 1);

                    // Spell targets a single friend (or self).
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetAlly ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetParty)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleFriend - 1);

                    // Spell targets AoE friends.
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCasterAreaParty ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitLastTargetAreaParty ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.SrcCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeFriend - 1);

                    // Spell targets any friend (or self).
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetAlly ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitTargetParty ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitCasterAreaParty ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.UnitLastTargetAreaParty ||
                        spellEffectInfo.TargetA.GetTarget() == Targets.SrcCaster)
                        AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyFriend - 1);

                    // Make sure that this spell includes a damage effect.
                    if (spellEffectInfo.Effect == SpellEffectName.SchoolDamage ||
                        spellEffectInfo.Effect == SpellEffectName.Instakill ||
                        spellEffectInfo.Effect == SpellEffectName.EnvironmentalDamage ||
                        spellEffectInfo.Effect == SpellEffectName.HealthLeech)
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Damage - 1);

                    // Make sure that this spell includes a healing effect (or an apply aura with a periodic heal).
                    if (spellEffectInfo.Effect == SpellEffectName.Heal ||
                        spellEffectInfo.Effect == SpellEffectName.HealMaxHealth ||
                        spellEffectInfo.Effect == SpellEffectName.HealMechanical ||
                        (spellEffectInfo.Effect == SpellEffectName.ApplyAura && spellEffectInfo.ApplyAuraName == AuraType.PeriodicHeal))
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Healing - 1);

                    // Make sure that this spell applies an aura.
                    if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                        AIInfo.Effects |= 1 << ((int)SelectEffect.Aura - 1);
                }

                _aiSpellInfo[(spellInfo.Id, spellInfo.Difficulty)] = AIInfo;
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

        /// <summary>
        // Called when unit's charm state changes with isNew = false
        // Implementation should call me->ScheduleAIChange() if AI replacement is desired
        // If this call is made, AI will be replaced on the next tick
        // When replacement is made, OnCharmed is called with isNew = true
        /// </summary>
        /// <param name="apply"></param>
        public virtual void OnCharmed(bool isNew)
        {
            if (!isNew)
                me.ScheduleAIChange();
        }

        public virtual bool ShouldSparWith(Unit target) { return false; }

        public virtual void DoAction(int action) { }
        public virtual uint GetData(uint id = 0) { return 0; }
        public virtual void SetData(uint id, uint value) { }
        public virtual void SetGUID(ObjectGuid guid, int id = 0) { }
        public virtual ObjectGuid GetGUID(int id = 0) { return ObjectGuid.Empty; }

        // Called when the unit enters combat
        // (NOTE: Creature engage logic should NOT be here, but in JustEngagedWith, which happens once threat is established!)
        public virtual void JustEnteredCombat(Unit who) { }

        // Called when the unit leaves combat
        public virtual void JustExitedCombat() { }

        // Called when the unit is about to be removed from the world (despawn, grid unload, corpse disappearing, player logging out etc.)
        public virtual void OnDespawn() { }

        // Called at any Damage to any victim (before damage apply)
        public virtual void DamageDealt(Unit victim, ref uint damage, DamageEffectType damageType) { }
        public virtual void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null) { }
        public virtual void HealReceived(Unit by, uint addhealth) { }
        public virtual void HealDone(Unit to, uint addhealth) { }
        public virtual void SpellInterrupted(uint spellId, uint unTimeMs) { }

        /// <summary>
        /// Called when a game event starts or ends
        /// </summary>
        public virtual void OnGameEvent(bool start, ushort eventId) { }

        public virtual string GetDebugInfo()
        {
            return $"Me: {(me != null ? me.GetDebugInfo() : "NULL")}";
        }

        public static AISpellInfoType GetAISpellInfo(uint spellId, Difficulty difficulty)
        {
            return _aiSpellInfo.LookupByKey((spellId, difficulty));
        }
    }
    
    public enum SelectTargetMethod
    {
        Random = 0,  // just pick a random target
        MaxThreat,   // prefer targets higher in the threat list
        MinThreat,   // prefer targets lower in the threat list
        MaxDistance, // prefer targets further from us
        MinDistance  // prefer targets closer to us
    }

    // default predicate function to select target based on distance, player and/or aura criteria
    public class DefaultTargetSelector : ICheck<Unit>
    {
        Unit _me;
        float _dist;
        bool _playerOnly;
        Unit _exception;
        int _aura;

        /// <param name="unit">the reference unit</param>
        /// <param name="dist">if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit</param>
        /// <param name="playerOnly">self explaining</param>
        /// <param name="withTank">allow current tank to be selected</param>
        /// <param name="aura">if 0: ignored, if > 0: the target shall have the aura, if < 0, the target shall NOT have the aura</param>
        public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
        {
            _me = unit;
            _dist = dist;
            _playerOnly = playerOnly;
            _exception = !withTank ? unit.GetThreatManager().GetLastVictim() : null;
            _aura = aura;
        }

        public bool Invoke(Unit target)
        {
            if (_me == null)
                return false;

            if (target == null)
                return false;

            if (_exception != null && target == _exception)
                return false;

            if (_playerOnly && !target.IsTypeId(TypeId.Player))
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
                return false;

            if (_aura != 0)
            {
                if (_aura > 0)
                {
                    if (!target.HasAura((uint)_aura))
                        return false;
                }
                else
                {
                    if (target.HasAura((uint)-_aura))
                        return false;
                }
            }

            return false;
        }
    }

    // Target selector for spell casts checking range, auras and attributes
    // todo Add more checks from Spell.CheckCast
    public class SpellTargetSelector : ICheck<Unit>
    {
        Unit _caster;
        SpellInfo _spellInfo;

        public SpellTargetSelector(Unit caster, uint spellId)
        {
            _caster = caster;
            _spellInfo = Global.SpellMgr.GetSpellInfo(spellId, caster.GetMap().GetDifficultyID());

            Cypher.Assert(_spellInfo != null);
        }

        public bool Invoke(Unit target)
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
    }

    // Very simple target selector, will just skip main target
    // NOTE: When passing to UnitAI.SelectTarget remember to use 0 as position for random selection
    //       because tank will not be in the temporary list
    public class NonTankTargetSelector : ICheck<Unit>
    {
        Unit _source;
        bool _playerOnly;

        public NonTankTargetSelector(Unit source, bool playerOnly = true)
        {
            _source = source;
            _playerOnly = playerOnly;
        }

        public bool Invoke(Unit target)
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
    }

    // Simple selector for units using mana
    class PowerUsersSelector : ICheck<Unit>
    {
        Unit _me;
        PowerType _power;
        float _dist;
        bool _playerOnly;

        public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
        {
            _me = unit;
            _power = power;
            _dist = dist;
            _playerOnly = playerOnly;
        }

        public bool Invoke(Unit target)
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
    }

    class FarthestTargetSelector : ICheck<Unit>
    {
        Unit _me;
        float _dist;
        bool _playerOnly;
        bool _inLos;

        public FarthestTargetSelector(Unit unit, float dist, bool playerOnly, bool inLos)
        {
            _me = unit;
            _dist = dist;
            _playerOnly = playerOnly;
            _inLos = inLos;
        }

        public bool Invoke(Unit target)
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
    }
}
