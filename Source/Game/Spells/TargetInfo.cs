// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;

namespace Game.Spells
{
    public class TargetInfo : TargetInfoBase
    {
        public int[] AuraBasePoints = new int[SpellConst.MaxEffects];
        public int AuraDuration;
        public int Damage { get; set; }

        // info set at PreprocessTarget, used by DoTargetSpellHit
        public DiminishingGroup DRGroup { get; set; }
        public int Healing { get; set; }
        public UnitAura HitAura { get; set; }

        public bool IsAlive { get; set; }
        public bool IsCrit { get; set; }

        public SpellMissInfo MissCondition { get; set; }
        public bool Positive { get; set; } = true;
        public SpellMissInfo ReflectResult { get; set; }
        public ObjectGuid TargetGUID;
        public ulong TimeDelay { get; set; }
        private bool _enablePVP; // need to enable PVP at DoDamageAndTriggers?

        private Unit _spellHitTarget; // changed for example by reflect

        public override void PreprocessTarget(Spell spell)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

            if (unit == null)
                return;

            // Need init unitTarget by default unit (can changed in code on reflect)
            spell.UnitTarget = unit;

            // Reset Damage/healing counter
            spell.EffectDamage = Damage;
            spell.EffectHealing = Healing;

            _spellHitTarget = null;

            if (MissCondition == SpellMissInfo.None ||
                (MissCondition == SpellMissInfo.Block && !spell.GetSpellInfo().HasAttribute(SpellAttr3.CompletelyBlocked)))
                _spellHitTarget = unit;
            else if (MissCondition == SpellMissInfo.Reflect &&
                     ReflectResult == SpellMissInfo.None)
                _spellHitTarget = spell.GetCaster().ToUnit();

            if (spell.GetOriginalCaster() &&
                MissCondition != SpellMissInfo.Evade &&
                !spell.GetOriginalCaster().IsFriendlyTo(unit) &&
                (!spell.SpellInfo.IsPositive() || spell.SpellInfo.HasEffect(SpellEffectName.Dispel)) &&
                (spell.SpellInfo.HasInitialAggro() || unit.IsEngaged()))
                unit.SetInCombatWith(spell.GetOriginalCaster());

            // if Target is flagged for pvp also flag caster if a player
            // but respect current pvp rules (buffing/healing npcs flagged for pvp only Flags you if they are in combat)
            _enablePVP = (MissCondition == SpellMissInfo.None || spell.SpellInfo.HasAttribute(SpellAttr3.PvpEnabling)) && unit.IsPvP() && (unit.IsInCombat() || unit.IsCharmedOwnedByPlayerOrPlayer()) && spell.GetCaster().IsPlayer(); // need to check PvP State before spell effects, but act on it afterwards

            if (_spellHitTarget)
            {
                SpellMissInfo missInfo = spell.PreprocessSpellHit(_spellHitTarget, this);

                if (missInfo != SpellMissInfo.None)
                {
                    if (missInfo != SpellMissInfo.Miss)
                        spell.GetCaster().SendSpellMiss(unit, spell.SpellInfo.Id, missInfo);

                    spell.EffectDamage = 0;
                    spell.EffectHealing = 0;
                    _spellHitTarget = null;
                }
            }

            spell.CallScriptOnHitHandlers();

            // scripts can modify Damage/healing for current Target, save them
            Damage = spell.EffectDamage;
            Healing = spell.EffectHealing;
        }

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

            if (unit == null)
                return;

            // Need init unitTarget by default unit (can changed in code on reflect)
            // Or on missInfo != SPELL_MISS_NONE unitTarget undefined (but need in trigger subsystem)
            spell.UnitTarget = unit;
            spell.TargetMissInfo = MissCondition;

            // Reset Damage/healing counter
            spell.EffectDamage = Damage;
            spell.EffectHealing = Healing;

            if (unit.IsAlive() != IsAlive)
                return;

            if (spell.GetState() == SpellState.Delayed &&
                !spell.IsPositive() &&
                (GameTime.GetGameTimeMS() - TimeDelay) <= unit.LastSanctuaryTime)
                return; // No missinfo in that case

            if (_spellHitTarget)
                spell.DoSpellEffectHit(_spellHitTarget, spellEffectInfo, this);

            // scripts can modify Damage/healing for current Target, save them
            Damage = spell.EffectDamage;
            Healing = spell.EffectHealing;
        }

        public override void DoDamageAndTriggers(Spell spell)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

            if (unit == null)
                return;

            // other targets executed before this one changed pointer
            spell.UnitTarget = unit;

            if (_spellHitTarget)
                spell.UnitTarget = _spellHitTarget;

            // Reset Damage/healing counter
            spell.EffectDamage = Damage;
            spell.EffectHealing = Healing;

            // Get original caster (if exist) and calculate Damage/healing from him _data
            // Skip if _originalCaster not available
            Unit caster = spell.GetOriginalCaster() ? spell.GetOriginalCaster() : spell.GetCaster().ToUnit();

            if (caster != null)
            {
                // Fill base trigger info
                ProcFlagsInit procAttacker = spell.ProcAttacker;
                ProcFlagsInit procVictim = spell.ProcVictim;
                ProcFlagsSpellType procSpellType = ProcFlagsSpellType.None;
                ProcFlagsHit hitMask = ProcFlagsHit.None;

                // Spells with this flag cannot trigger if effect is cast on self
                bool canEffectTrigger = (!spell.SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs) || !spell.SpellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs)) && spell.UnitTarget.CanProc();

                // Trigger info was not filled in Spell::prepareDataForTriggerSystem - we do it now
                if (canEffectTrigger &&
                    !procAttacker &&
                    !procVictim)
                {
                    bool positive = true;

                    if (spell.EffectDamage > 0)
                        positive = false;
                    else if (spell.EffectHealing == 0)
                        for (uint i = 0; i < spell.SpellInfo.GetEffects().Count; ++i)
                        {
                            // in case of immunity, check all effects to choose correct procFlags, as none has technically hit
                            if (EffectMask != 0 &&
                                (EffectMask & (1 << (int)i)) == 0)
                                continue;

                            if (!spell.SpellInfo.IsPositiveEffect(i))
                            {
                                positive = false;

                                break;
                            }
                        }

                    switch (spell.SpellInfo.DmgClass)
                    {
                        case SpellDmgClass.None:
                        case SpellDmgClass.Magic:
                            if (spell.SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                                    procVictim.Or(ProcFlags.TakeHelpfulPeriodic);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                                    procVictim.Or(ProcFlags.TakeHarmfulPeriodic);
                                }
                            }
                            else if (spell.SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulAbility);
                                    procVictim.Or(ProcFlags.TakeHelpfulAbility);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulAbility);
                                    procVictim.Or(ProcFlags.TakeHarmfulAbility);
                                }
                            }
                            else
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulSpell);
                                    procVictim.Or(ProcFlags.TakeHelpfulSpell);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulSpell);
                                    procVictim.Or(ProcFlags.TakeHarmfulSpell);
                                }
                            }

                            break;
                    }
                }

                // All calculated do it!
                // Do healing
                bool hasHealing = false;
                DamageInfo spellDamageInfo = null;
                HealInfo healInfo = null;

                if (spell.EffectHealing > 0)
                {
                    hasHealing = true;
                    int addhealth = spell.EffectHealing;

                    if (IsCrit)
                    {
                        hitMask |= ProcFlagsHit.Critical;
                        addhealth = Unit.SpellCriticalHealingBonus(caster, spell.SpellInfo, addhealth, null);
                    }
                    else
                    {
                        hitMask |= ProcFlagsHit.Normal;
                    }

                    healInfo = new HealInfo(caster, spell.UnitTarget, (uint)addhealth, spell.SpellInfo, spell.SpellInfo.GetSchoolMask());
                    caster.HealBySpell(healInfo, IsCrit);
                    spell.UnitTarget.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, spell.SpellInfo);
                    spell.EffectHealing = (int)healInfo.GetEffectiveHeal();

                    procSpellType |= ProcFlagsSpellType.Heal;
                }

                // Do Damage
                bool hasDamage = false;

                if (spell.EffectDamage > 0)
                {
                    hasDamage = true;
                    // Fill base Damage struct (unitTarget - is real spell Target)
                    SpellNonMeleeDamage damageInfo = new(caster, spell.UnitTarget, spell.SpellInfo, spell.SpellVisual, spell.SpellSchoolMask, spell.CastId);

                    // Check Damage immunity
                    if (spell.UnitTarget.IsImmunedToDamage(spell.SpellInfo))
                    {
                        hitMask = ProcFlagsHit.Immune;
                        spell.EffectDamage = 0;

                        // no packet found in sniffs
                    }
                    else
                    {
                        caster.SetLastDamagedTargetGuid(spell.UnitTarget.GetGUID());

                        // Add bonuses and fill damageInfo struct
                        caster.CalculateSpellDamageTaken(damageInfo, spell.EffectDamage, spell.SpellInfo, spell.AttackType, IsCrit, MissCondition == SpellMissInfo.Block, spell);
                        Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);

                        hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                        procVictim.Or(ProcFlags.TakeAnyDamage);

                        spell.EffectDamage = (int)damageInfo.Damage;

                        caster.DealSpellDamage(damageInfo, true);

                        // Send log Damage message to client
                        caster.SendSpellNonMeleeDamageLog(damageInfo);
                    }

                    // Do triggers for unit
                    if (canEffectTrigger)
                    {
                        spellDamageInfo = new DamageInfo(damageInfo, DamageEffectType.SpellDirect, spell.AttackType, hitMask);
                        procSpellType |= ProcFlagsSpellType.Damage;
                    }
                }

                // Passive spell hits/misses or active spells only misses (only triggers)
                if (!hasHealing &&
                    !hasDamage)
                {
                    // Fill base Damage struct (unitTarget - is real spell Target)
                    SpellNonMeleeDamage damageInfo = new(caster, spell.UnitTarget, spell.SpellInfo, spell.SpellVisual, spell.SpellSchoolMask);
                    hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);

                    // Do triggers for unit
                    if (canEffectTrigger)
                    {
                        spellDamageInfo = new DamageInfo(damageInfo, DamageEffectType.NoDamage, spell.AttackType, hitMask);
                        procSpellType |= ProcFlagsSpellType.NoDmgHeal;
                    }

                    // Failed Pickpocket, reveal rogue
                    if (MissCondition == SpellMissInfo.Resist &&
                        spell.SpellInfo.HasAttribute(SpellCustomAttributes.PickPocket) &&
                        spell.UnitTarget.IsCreature())
                    {
                        Unit unitCaster = spell.GetCaster().ToUnit();
                        unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);
                        spell.UnitTarget.ToCreature().EngageWithTarget(unitCaster);
                    }
                }

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    if (spell.SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
                        procAttacker = new ProcFlagsInit();

                    if (spell.SpellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs))
                        procVictim = new ProcFlagsInit();

                    Unit.ProcSkillsAndAuras(caster, spell.UnitTarget, procAttacker, procVictim, procSpellType, ProcFlagsSpellPhase.Hit, hitMask, spell, spellDamageInfo, healInfo);

                    // Item spells (spell hit of non-Damage spell may also activate items, for example seal of corruption hidden hit)
                    if (caster.IsPlayer() &&
                        procSpellType.HasAnyFlag(ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal))
                        if (spell.SpellInfo.DmgClass == SpellDmgClass.Melee ||
                            spell.SpellInfo.DmgClass == SpellDmgClass.Ranged)
                            if (!spell.SpellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat) &&
                                !spell.SpellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs))
                                caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
                }

                // set hitmask for finish procs
                spell.HitMask |= hitMask;

                // Do not take combo points on dodge and miss
                if (MissCondition != SpellMissInfo.None &&
                    spell.NeedComboPoints &&
                    spell.Targets.GetUnitTargetGUID() == TargetGUID)
                    spell.NeedComboPoints = false;

                // _spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
                if (MissCondition != SpellMissInfo.Evade &&
                    _spellHitTarget &&
                    !spell.GetCaster().IsFriendlyTo(unit) &&
                    (!spell.IsPositive() || spell.SpellInfo.HasEffect(SpellEffectName.Dispel)))
                {
                    Unit unitCaster = spell.GetCaster().ToUnit();

                    if (unitCaster != null)
                    {
                        unitCaster.AtTargetAttacked(unit, spell.SpellInfo.HasInitialAggro());

                        if (spell.SpellInfo.HasAttribute(SpellAttr6.TapsImmediately))
                        {
                            Creature targetCreature = unit.ToCreature();

                            if (targetCreature != null)
                                if (unitCaster.IsPlayer())
                                    targetCreature.SetTappedBy(unitCaster);
                        }
                    }

                    if (!spell.SpellInfo.HasAttribute(SpellAttr3.DoNotTriggerTargetStand) &&
                        !unit.IsStandState())
                        unit.SetStandState(UnitStandStateType.Stand);
                }

                // Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
                if (MissCondition == SpellMissInfo.None &&
                    spell.SpellInfo.HasAttribute(SpellAttr7.InterruptOnlyNonplayer) &&
                    !unit.IsPlayer())
                    caster.CastSpell(unit, 32747, new CastSpellExtraArgs(spell));
            }

            if (_spellHitTarget)
            {
                //AI functions
                Creature cHitTarget = _spellHitTarget.ToCreature();

                if (cHitTarget != null)
                {
                    CreatureAI hitTargetAI = cHitTarget.GetAI();

                    hitTargetAI?.SpellHit(spell.GetCaster(), spell.SpellInfo);
                }

                if (spell.GetCaster().IsCreature() &&
                    spell.GetCaster().ToCreature().IsAIEnabled())
                    spell.GetCaster().ToCreature().GetAI().SpellHitTarget(_spellHitTarget, spell.SpellInfo);
                else if (spell.GetCaster().IsGameObject() &&
                         spell.GetCaster().ToGameObject().GetAI() != null)
                    spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(_spellHitTarget, spell.SpellInfo);

                if (HitAura != null)
                {
                    AuraApplication aurApp = HitAura.GetApplicationOfTarget(_spellHitTarget.GetGUID());

                    if (aurApp != null)
                    {
                        // only apply unapplied effects (for reapply case)
                        uint effMask = EffectMask & aurApp.GetEffectsToApply();

                        for (uint i = 0; i < spell.SpellInfo.GetEffects().Count; ++i)
                            if ((effMask & (1 << (int)i)) != 0 &&
                                aurApp.HasEffect(i))
                                effMask &= ~(1u << (int)i);

                        if (effMask != 0)
                            _spellHitTarget._ApplyAura(aurApp, effMask);
                    }
                }

                // Needs to be called after dealing Damage/healing to not remove breaking on Damage Auras
                spell.DoTriggersOnSpellHit(_spellHitTarget);
            }

            if (_enablePVP)
                spell.GetCaster().ToPlayer().UpdatePvP(true);

            spell.SpellAura = HitAura;
            spell.CallScriptAfterHitHandlers();
            spell.SpellAura = null;
        }
    }
}