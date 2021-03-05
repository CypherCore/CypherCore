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
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Combat;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking.Packets;
using Game.PvP;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit
    {
        // Check if unit in combat with specific unit
        public bool IsInCombatWith(Unit who)
        {
            // Check target exists
            if (!who)
                return false;

            // Search in threat list
            var guid = who.GetGUID();
            foreach (var refe in GetThreatManager().GetThreatList())
            {
                // Return true if the unit matches
                if (refe != null && refe.GetUnitGuid() == guid)
                    return true;
            }

            // Nothing found, false.
            return false;
        }

        public ThreatManager GetThreatManager() { return threatManager; }

        public bool CanDualWield() { return m_canDualWield; }

        public void SendChangeCurrentVictim(HostileReference pHostileReference)
        {
            if (!GetThreatManager().IsThreatListEmpty())
            {
                var packet = new HighestThreatUpdate();
                packet.UnitGUID = GetGUID();
                packet.HighestThreatGUID = pHostileReference.GetUnitGuid();

                var refeList = GetThreatManager().GetThreatList();
                foreach (var refe in refeList)
                {
                    var info = new ThreatInfo();
                    info.UnitGUID = refe.GetUnitGuid();
                    info.Threat = (long)refe.GetThreat() * 100;
                    packet.ThreatList.Add(info);
                }
                SendMessageToSet(packet, false);
            }
        }

        public void StopAttackFaction(uint factionId)
        {
            var victim = GetVictim();
            if (victim != null)
            {
                if (victim.GetFactionTemplateEntry().Faction == factionId)
                {
                    AttackStop();
                    if (IsNonMeleeSpellCast(false))
                        InterruptNonMeleeSpells(false);

                    // melee and ranged forced attack cancel
                    if (IsTypeId(TypeId.Player))
                        ToPlayer().SendAttackSwingCancelAttack();
                }
            }

            var attackers = GetAttackers();
            for (var i = 0; i < attackers.Count;)
            {
                var unit = attackers[i];
                if (unit.GetFactionTemplateEntry().Faction == factionId)
                {
                    unit.AttackStop();
                    i = 0;
                }
                else
                    ++i;
            }

            GetHostileRefManager().DeleteReferencesForFaction(factionId);

            foreach (var control in m_Controlled)
                control.StopAttackFaction(factionId);
        }

        public void HandleProcExtraAttackFor(Unit victim)
        {
            while (ExtraAttacks != 0)
            {
                AttackerStateUpdate(victim, WeaponAttackType.BaseAttack, true);
                --ExtraAttacks;
            }
        }

        public virtual void SetCanDualWield(bool value) { m_canDualWield = value; }

        public void SendClearThreatList()
        {
            var packet = new ThreatClear();
            packet.UnitGUID = GetGUID();
            SendMessageToSet(packet, false);
        }

        public void SendRemoveFromThreatList(HostileReference pHostileReference)
        {
            var packet = new ThreatRemove();
            packet.UnitGUID = GetGUID();
            packet.AboutGUID = pHostileReference.GetUnitGuid();
            SendMessageToSet(packet, false);
        }

        void SendThreatListUpdate()
        {
            if (!GetThreatManager().IsThreatListEmpty())
            {
                var packet = new ThreatUpdate();
                packet.UnitGUID = GetGUID();
                var tlist = GetThreatManager().GetThreatList();
                foreach (var refe in tlist)
                {
                    var info = new ThreatInfo();
                    info.UnitGUID = refe.GetUnitGuid();
                    info.Threat = (long)refe.GetThreat() * 100;
                    packet.ThreatList.Add(info);
                }
                SendMessageToSet(packet, false);
            }
        }

        public void TauntApply(Unit taunter)
        {
            Cypher.Assert(IsTypeId(TypeId.Unit));

            if (!taunter || (taunter.IsTypeId(TypeId.Player) && taunter.ToPlayer().IsGameMaster()))
                return;

            if (!CanHaveThreatList())
                return;

            var creature = ToCreature();

            if (creature.HasReactState(ReactStates.Passive))
                return;

            var target = GetVictim();
            if (target && target == taunter)
                return;

            if (!IsFocusing(null, true))
                SetInFront(taunter);

            if (creature.IsAIEnabled)
                creature.GetAI().AttackStart(taunter);
        }

        public void TauntFadeOut(Unit taunter)
        {
            Cypher.Assert(IsTypeId(TypeId.Unit));

            if (!taunter || (taunter.IsTypeId(TypeId.Player) && taunter.ToPlayer().IsGameMaster()))
                return;

            if (!CanHaveThreatList())
                return;

            var creature = ToCreature();

            if (creature.HasReactState(ReactStates.Passive))
                return;

            var target = GetVictim();
            if (!target || target != taunter)
                return;

            if (threatManager.IsThreatListEmpty())
            {
                if (creature.IsAIEnabled)
                    creature.GetAI().EnterEvadeMode(EvadeReason.NoHostiles);
                return;
            }

            target = creature.SelectVictim();  // might have more taunt auras remaining

            if (target && target != taunter)
            {
                if (!IsFocusing(null, true))
                    SetInFront(target);
                if (creature.IsAIEnabled)
                    creature.GetAI().AttackStart(target);
            }
        }

        public void ValidateAttackersAndOwnTarget()
        {
            // iterate attackers
            var toRemove = new List<Unit>();
            foreach (var attacker in GetAttackers())
                if (!attacker.IsValidAttackTarget(this))
                    toRemove.Add(attacker);

            foreach (var attacker in toRemove)
                attacker.AttackStop();

            // remove our own victim
            var victim = GetVictim();
            if (victim != null)
                if (!IsValidAttackTarget(victim))
                    AttackStop();
        }

        public void CombatStop(bool includingCast = false)
        {
            if (includingCast && IsNonMeleeSpellCast(false))
                InterruptNonMeleeSpells(false);

            AttackStop();
            RemoveAllAttackers();
            if (IsTypeId(TypeId.Player))
                ToPlayer().SendAttackSwingCancelAttack();     // melee and ranged forced attack cancel
            ClearInCombat();

            // just in case
            if (IsPetInCombat() && GetTypeId() != TypeId.Player)
                ClearInPetCombat();
        }
        public void CombatStopWithPets(bool includingCast = false)
        {
            CombatStop(includingCast);

            foreach (var control in m_Controlled)
                control.CombatStop(includingCast);
        }
        public void ClearInCombat()
        {
            combatTimer = 0;
            RemoveUnitFlag(UnitFlags.InCombat);

            // Player's state will be cleared in Player.UpdateContestedPvP
            var creature = ToCreature();
            if (creature != null)
            {
                ClearUnitState(UnitState.AttackPlayer);
                if (HasDynamicFlag(UnitDynFlags.Tapped))
                    SetDynamicFlags((UnitDynFlags)creature.GetCreatureTemplate().DynamicFlags);

                if (creature.IsPet() || creature.IsGuardian())
                {
                    var owner = GetOwner();
                    if (owner)
                        for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                            if (owner.GetSpeedRate(i) > GetSpeedRate(i))
                                SetSpeedRate(i, owner.GetSpeedRate(i));
                }
                else if (!IsCharmed())
                    return;
            }
            else
                ToPlayer().OnCombatExit();

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeaveCombat);
        }

        public void ClearInPetCombat()
        {
            RemoveUnitFlag(UnitFlags.PetInCombat);
            var owner = GetOwner();
            if (owner != null)
                owner.RemoveUnitFlag(UnitFlags.PetInCombat);
        }

        public void RemoveAllAttackers()
        {
            while (!attackerList.Empty())
            {
                var iter = attackerList.First();
                if (!iter.AttackStop())
                {
                    Log.outError(LogFilter.Unit, "WORLD: Unit has an attacker that isn't attacking it!");
                    attackerList.Remove(iter);
                }
            }
        }

        public void AddHatedBy(HostileReference pHostileReference)
        {
            hostileRefManager.InsertFirst(pHostileReference);
        }
        public void RemoveHatedBy(HostileReference pHostileReference) { } //nothing to do yet

        public float ApplyTotalThreatModifier(float fThreat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal)
        {
            if (!HasAuraType(AuraType.ModThreat) || fThreat < 0)
                return fThreat;

            var school = Global.SpellMgr.GetFirstSchoolInMask(schoolMask);

            return fThreat * m_threatModifier[(int)school];
        }

        public bool IsTargetableForAttack(bool checkFakeDeath = true)
        {
            if (!IsAlive())
                return false;

            if (HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.NotSelectable))
                return false;

            if (IsTypeId(TypeId.Player) && ToPlayer().IsGameMaster())
                return false;

            return !HasUnitState(UnitState.Unattackable) && (!checkFakeDeath || !HasUnitState(UnitState.Died));
        }

        public DeathState GetDeathState()
        {
            return m_deathState;
        }

        public bool IsEngaged()  { return IsInCombat();    }
        public bool IsEngagedBy(Unit who) { return IsInCombatWith(who); }
        public void EngageWithTarget(Unit who)
        {
            SetInCombatWith(who);
            who.SetInCombatWith(this);
            GetThreatManager().AddThreat(who, 0.0f);
        }
        public bool IsThreatened() { return CanHaveThreatList() && !GetThreatManager().IsThreatListEmpty(); }
        public bool IsThreatenedBy(Unit who) { return who != null && CanHaveThreatList() && GetThreatManager().IsThreatenedBy(who); }

        public bool IsInCombat() { return HasUnitFlag(UnitFlags.InCombat); }
        public bool IsPetInCombat() { return HasUnitFlag(UnitFlags.PetInCombat); }

        public bool Attack(Unit victim, bool meleeAttack)
        {
            if (victim == null || victim.GetGUID() == GetGUID())
                return false;

            // dead units can neither attack nor be attacked
            if (!IsAlive() || !victim.IsInWorld || !victim.IsAlive())
                return false;

            // player cannot attack in mount state
            if (IsTypeId(TypeId.Player) && IsMounted())
                return false;

            var creature = ToCreature();
            // creatures cannot attack while evading
            if (creature != null && creature.IsInEvadeMode())
                return false;

            if (HasUnitFlag(UnitFlags.Pacified))
                return false;

            if (HasAuraType(AuraType.DisableAttackingExceptAbilities))
                return false;

            // nobody can attack GM in GM-mode
            if (victim.IsTypeId(TypeId.Player))
            {
                if (victim.ToPlayer().IsGameMaster())
                    return false;
            }
            else
            {
                if (victim.ToCreature().IsEvadingAttacks())
                    return false;
            }

            // remove SPELL_AURA_MOD_UNATTACKABLE at attack (in case non-interruptible spells stun aura applied also that not let attack)
            if (HasAuraType(AuraType.ModUnattackable))
                RemoveAurasByType(AuraType.ModUnattackable);

            if (attacking != null)
            {
                if (attacking == victim)
                {
                    // switch to melee attack from ranged/magic
                    if (meleeAttack)
                    {
                        if (!HasUnitState(UnitState.MeleeAttacking))
                        {
                            AddUnitState(UnitState.MeleeAttacking);
                            SendMeleeAttackStart(victim);
                            return true;
                        }
                    }
                    else if (HasUnitState(UnitState.MeleeAttacking))
                    {
                        ClearUnitState(UnitState.MeleeAttacking);
                        SendMeleeAttackStop(victim);
                        return true;
                    }
                    return false;
                }

                // switch target
                InterruptSpell(CurrentSpellTypes.Melee);
                if (!meleeAttack)
                    ClearUnitState(UnitState.MeleeAttacking);
            }

            if (attacking != null)
                attacking._removeAttacker(this);

            attacking = victim;
            attacking._addAttacker(this);

            // Set our target
            SetTarget(victim.GetGUID());

            if (meleeAttack)
                AddUnitState(UnitState.MeleeAttacking);

            if (creature != null && !IsPet())
            {
                // should not let player enter combat by right clicking target - doesn't helps
                GetThreatManager().AddThreat(victim, 0.0f);
                SetInCombatWith(victim);

                if (victim.IsTypeId(TypeId.Player))
                    victim.SetInCombatWith(this);

                var owner = victim.GetOwner();
                if (owner != null)
                {
                    GetThreatManager().AddThreat(owner, 0.0f);
                    SetInCombatWith(owner);
                    if (owner.GetTypeId() == TypeId.Player)
                        owner.SetInCombatWith(this);
                }

                creature.SendAIReaction(AiReaction.Hostile);
                creature.CallAssistance();

                // Remove emote state - will be restored on creature reset
                SetEmoteState(Emote.OneshotNone);
            }

            // delay offhand weapon attack by 50% of the base attack time
            if (HaveOffhandWeapon() && GetTypeId() != TypeId.Player)
                SetAttackTimer(WeaponAttackType.OffAttack, Math.Max(GetAttackTimer(WeaponAttackType.OffAttack), GetAttackTimer(WeaponAttackType.BaseAttack) + MathFunctions.CalculatePct(GetBaseAttackTime(WeaponAttackType.BaseAttack), 50)));

            if (meleeAttack)
                SendMeleeAttackStart(victim);

            // Let the pet know we've started attacking someting. Handles melee attacks only
            // Spells such as auto-shot and others handled in WorldSession.HandleCastSpellOpcode
            if (IsTypeId(TypeId.Player))
            {
                foreach (var controlled in m_Controlled)
                {
                    var cControlled = controlled.ToCreature();
                    if (cControlled != null)
                        if (cControlled.IsAIEnabled)
                            cControlled.GetAI().OwnerAttacked(victim);
                }
            }
            return true;
        }
        public void SendMeleeAttackStart(Unit victim)
        {
            var packet = new AttackStart();
            packet.Attacker = GetGUID();
            packet.Victim = victim.GetGUID();
            SendMessageToSet(packet, true);
        }
        public void SendMeleeAttackStop(Unit victim = null)
        {
            SendMessageToSet(new SAttackStop(this, victim), true);

            if (victim)
                Log.outInfo(LogFilter.Unit, "{0} {1} stopped attacking {2} {3}", (IsTypeId(TypeId.Player) ? "Player" : "Creature"), GetGUID().ToString(),
                    (victim.IsTypeId(TypeId.Player) ? "player" : "creature"), victim.GetGUID().ToString());
            else
                Log.outInfo(LogFilter.Unit, "{0} {1} stopped attacking", (IsTypeId(TypeId.Player) ? "Player" : "Creature"), GetGUID().ToString());
        }
        public ObjectGuid GetTarget() { return m_unitData.Target; }
        public virtual void SetTarget(ObjectGuid guid) { }
        public bool AttackStop()
        {
            if (attacking == null)
                return false;

            var victim = attacking;

            attacking._removeAttacker(this);
            attacking = null;

            // Clear our target
            SetTarget(ObjectGuid.Empty);

            ClearUnitState(UnitState.MeleeAttacking);

            InterruptSpell(CurrentSpellTypes.Melee);

            // reset only at real combat stop
            var creature = ToCreature();
            if (creature != null)
            {
                creature.SetNoCallAssistance(false);

                if (creature.HasSearchedAssistance())
                {
                    creature.SetNoSearchAssistance(false);
                    UpdateSpeed(UnitMoveType.Run);
                }
            }

            SendMeleeAttackStop(victim);
            return true;
        }
        void _addAttacker(Unit pAttacker)
        {
            attackerList.Add(pAttacker);
        }
        void _removeAttacker(Unit pAttacker)
        {
            attackerList.Remove(pAttacker);
        }
        public Unit GetVictim()
        {
            return attacking;
        }
        public Unit GetAttackerForHelper()
        {
            if (!IsEngaged())
                return null;

            var victim = GetVictim();
            if (victim != null)
                if ((!IsPet() && GetPlayerMovingMe() == null) || IsInCombatWith(victim) || victim.IsInCombatWith(this))
                    return victim;

            if (!attackerList.Empty())
                return attackerList[0];

            var owner = GetCharmerOrOwnerPlayerOrPlayerItself();
            if (owner != null)
            {
                var refe = owner.GetHostileRefManager().GetFirst();
                while (refe != null)
                {
                    var hostile = refe.GetSource().GetOwner();
                    if (hostile != null)
                        return hostile;

                    refe = refe.Next();
                }
            }

            return null;
        }
        public List<Unit> GetAttackers()
        {
            return attackerList;
        }

        public override float GetCombatReach() { return m_unitData.CombatReach; }
        public void SetCombatReach(float combatReach) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CombatReach), combatReach); }
        public float GetBoundingRadius() { return m_unitData.BoundingRadius; }
        public void SetBoundingRadius(float boundingRadius) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.BoundingRadius), boundingRadius); }

        public bool HaveOffhandWeapon()
        {
            if (IsTypeId(TypeId.Player))
                return ToPlayer().GetWeaponForAttack(WeaponAttackType.OffAttack, true) != null;
            else
                return m_canDualWield;
        }
        public void ResetAttackTimer(WeaponAttackType type = WeaponAttackType.BaseAttack)
        {
            m_attackTimer[(int)type] = (uint)(GetBaseAttackTime(type) * m_modAttackSpeedPct[(int)type]);
        }
        public void SetAttackTimer(WeaponAttackType type, uint time)
        {
            m_attackTimer[(int)type] = time;
        }
        public uint GetAttackTimer(WeaponAttackType type)
        {
            return m_attackTimer[(int)type];
        }
        public bool IsAttackReady(WeaponAttackType type = WeaponAttackType.BaseAttack)
        {
            return m_attackTimer[(int)type] == 0;
        }
        public uint GetBaseAttackTime(WeaponAttackType att)
        {
            return m_baseAttackSpeed[(int)att];
        }
        public void AttackerStateUpdate(Unit victim, WeaponAttackType attType = WeaponAttackType.BaseAttack, bool extra = false)
        {
            if (HasUnitState(UnitState.CannotAutoattack) || HasUnitFlag(UnitFlags.Pacified))
                return;

            if (!victim.IsAlive())
                return;

            if ((attType == WeaponAttackType.BaseAttack || attType == WeaponAttackType.OffAttack) && !IsWithinLOSInMap(victim))
                return;

            CombatStart(victim);
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.MeleeAttack);

            // ignore ranged case
            if (attType != WeaponAttackType.BaseAttack && attType != WeaponAttackType.OffAttack)
                return;

            if (IsTypeId(TypeId.Unit) && !HasUnitFlag(UnitFlags.PlayerControlled))
                SetFacingToObject(victim, false); // update client side facing to face the target (prevents visual glitches when casting untargeted spells)

            // melee attack spell casted at main hand attack only - no normal melee dmg dealt
            if (attType == WeaponAttackType.BaseAttack && GetCurrentSpell(CurrentSpellTypes.Melee) != null && !extra)
                m_currentSpells[CurrentSpellTypes.Melee].Cast();
            else
            {
                // attack can be redirected to another target
                victim = GetMeleeHitRedirectTarget(victim);

                var meleeAttackOverrides = GetAuraEffectsByType(AuraType.OverrideAutoattackWithMeleeSpell);
                AuraEffect meleeAttackAuraEffect = null;
                uint meleeAttackSpellId = 0;
                if (attType == WeaponAttackType.BaseAttack)
                {
                    if (!meleeAttackOverrides.Empty())
                    {
                        meleeAttackAuraEffect = meleeAttackOverrides.First();
                        meleeAttackSpellId = meleeAttackAuraEffect.GetSpellEffectInfo().TriggerSpell;
                    }
                }
                else
                {
                    var auraEffect = meleeAttackOverrides.Find(aurEff =>
                    {
                        return aurEff.GetSpellEffectInfo().MiscValue != 0;
                    });

                    if (auraEffect != null)
                    {
                        meleeAttackAuraEffect = auraEffect;
                        meleeAttackSpellId = (uint)meleeAttackAuraEffect.GetSpellEffectInfo().MiscValue;
                    }
                }

                if (meleeAttackAuraEffect == null)
                {
                    CalcDamageInfo damageInfo;
                    CalculateMeleeDamage(victim, 0, out damageInfo, attType);
                    // Send log damage message to client
                    DealDamageMods(victim, ref damageInfo.damage, ref damageInfo.absorb);
                    SendAttackStateUpdate(damageInfo);

                    DealMeleeDamage(damageInfo, true);

                    var dmgInfo = new DamageInfo(damageInfo);
                    ProcSkillsAndAuras(damageInfo.target, damageInfo.procAttacker, damageInfo.procVictim, ProcFlagsSpellType.None, ProcFlagsSpellPhase.None, dmgInfo.GetHitMask(), null, dmgInfo, null);
                    Log.outDebug(LogFilter.Unit, "AttackerStateUpdate: {0} attacked {1} for {2} dmg, absorbed {3}, blocked {4}, resisted {5}.",
                        GetGUID().ToString(), victim.GetGUID().ToString(), damageInfo.damage, damageInfo.absorb, damageInfo.blocked_amount, damageInfo.resist);
                }
                else
                {
                    CastSpell(victim, meleeAttackSpellId, true, null, meleeAttackAuraEffect);

                    var hitInfo = HitInfo.AffectsVictim | HitInfo.NoAnimation;
                    if (attType == WeaponAttackType.OffAttack)
                        hitInfo |= HitInfo.OffHand;

                    SendAttackStateUpdate(hitInfo, victim, GetMeleeDamageSchoolMask(), 0, 0, 0, VictimState.Hit, 0);
                }
            }
        }

        public void FakeAttackerStateUpdate(Unit victim, WeaponAttackType attType = WeaponAttackType.BaseAttack)
        {
            if (HasUnitState(UnitState.CannotAutoattack) || HasUnitFlag(UnitFlags.Pacified))
                return;

            if (!victim.IsAlive())
                return;

            if ((attType == WeaponAttackType.BaseAttack || attType == WeaponAttackType.OffAttack) && !IsWithinLOSInMap(victim))
                return;

            CombatStart(victim);
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.MeleeAttack);

            if (attType != WeaponAttackType.BaseAttack && attType != WeaponAttackType.OffAttack)
                return;                                             // ignore ranged case

            if (IsTypeId(TypeId.Unit) && !HasUnitFlag(UnitFlags.PlayerControlled))
                SetFacingToObject(victim, false); // update client side facing to face the target (prevents visual glitches when casting untargeted spells)

            var damageInfo = new CalcDamageInfo();
            damageInfo.attacker = this;
            damageInfo.target = victim;
            damageInfo.damageSchoolMask = (uint)GetMeleeDamageSchoolMask();
            damageInfo.attackType = attType;
            damageInfo.damage = 0;
            damageInfo.originalDamage = 0;
            damageInfo.cleanDamage = 0;
            damageInfo.absorb = 0;
            damageInfo.resist = 0;
            damageInfo.blocked_amount = 0;

            damageInfo.TargetState = VictimState.Hit;
            damageInfo.HitInfo = HitInfo.AffectsVictim | HitInfo.NormalSwing | HitInfo.FakeDamage;
            if (attType == WeaponAttackType.OffAttack)
                damageInfo.HitInfo |= HitInfo.OffHand;

            damageInfo.procAttacker = ProcFlags.None;
            damageInfo.procVictim = ProcFlags.None;
            damageInfo.hitOutCome = MeleeHitOutcome.Normal;

            SendAttackStateUpdate(damageInfo);
        }

        public void SetBaseWeaponDamage(WeaponAttackType attType, WeaponDamageRange damageRange, float value) { m_weaponDamage[(int)attType][(int)damageRange] = value; }

        void StartReactiveTimer(ReactiveType reactive) { m_reactiveTimer[reactive] = 4000; }

        public Unit GetMagicHitRedirectTarget(Unit victim, SpellInfo spellInfo)
        {
            // Patch 1.2 notes: Spell Reflection no longer reflects abilities
            if (spellInfo.HasAttribute(SpellAttr0.Ability) || spellInfo.HasAttribute(SpellAttr1.CantBeRedirected) || spellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return victim;

            var magnetAuras = victim.GetAuraEffectsByType(AuraType.SpellMagnet);
            foreach (var eff in magnetAuras)
            {
                var magnet = eff.GetBase().GetCaster();
                if (magnet != null)
                {
                    if (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && _IsValidAttackTarget(magnet, spellInfo))
                    {
                        // @todo handle this charge drop by proc in cast phase on explicit target
                        if (spellInfo.HasHitDelay())
                        {
                            // Set up missile speed based delay
                            var hitDelay = spellInfo.LaunchDelay;
                            if (spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                                hitDelay += spellInfo.Speed;
                            else if (spellInfo.Speed > 0.0f)
                                hitDelay += Math.Max(victim.GetDistance(this), 5.0f) / spellInfo.Speed;

                            var delay = (uint)Math.Floor(hitDelay * 1000.0f);
                            // Schedule charge drop
                            eff.GetBase().DropChargeDelayed(delay, AuraRemoveMode.Expire);
                        }
                        else
                            eff.GetBase().DropCharge(AuraRemoveMode.Expire);
                        return magnet;
                    }
                }
            }
            return victim;
        }
        public Unit GetMeleeHitRedirectTarget(Unit victim, SpellInfo spellInfo = null)
        {
            var interceptAuras = victim.GetAuraEffectsByType(AuraType.InterceptMeleeRangedAttacks);
            foreach (var i in interceptAuras)
            {
                var magnet = i.GetCaster();
                if (magnet != null)
                    if (_IsValidAttackTarget(magnet, spellInfo) && magnet.IsWithinLOSInMap(this)
                       && (spellInfo == null || (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk
                       && spellInfo.CheckTarget(this, magnet, false) == SpellCastResult.SpellCastOk)))
                    {
                        i.GetBase().DropCharge(AuraRemoveMode.Expire);
                        return magnet;
                    }
            }
            return victim;
        }
        public bool IsValidAttackTarget(Unit target)
        {
            return _IsValidAttackTarget(target, null);
        }

        void DealDamageMods(Unit victim, ref uint damage)
        {
            if (victim == null || !victim.IsAlive() || victim.HasUnitState(UnitState.InFlight)
                || (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsInEvadeMode()))
            {
                damage = 0;
            }
        }
        public void DealDamageMods(Unit victim, ref uint damage, ref uint absorb)
        {
            if (victim == null || !victim.IsAlive() || victim.HasUnitState(UnitState.InFlight)
                || (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks()))
            {
                absorb += damage;
                damage = 0;
                return;
            }

           damage = (uint)(damage * GetDamageMultiplierForTarget(victim));
        }
        void DealMeleeDamage(CalcDamageInfo damageInfo, bool durabilityLoss)
        {
            var victim = damageInfo.target;

            if (!victim.IsAlive() || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks()))
                return;

            // Hmmmm dont like this emotes client must by self do all animations
            if (damageInfo.HitInfo.HasAnyFlag(HitInfo.CriticalHit))
                victim.HandleEmoteCommand(Emote.OneshotWoundCritical);
            if (damageInfo.blocked_amount != 0 && damageInfo.TargetState != VictimState.Blocks)
                victim.HandleEmoteCommand(Emote.OneshotParryShield);

            if (damageInfo.TargetState == VictimState.Parry &&
                (!IsTypeId(TypeId.Unit) || !ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoParryHasten)))
            {
                // Get attack timers
                float offtime = victim.GetAttackTimer(WeaponAttackType.OffAttack);
                float basetime = victim.GetAttackTimer(WeaponAttackType.BaseAttack);
                // Reduce attack time
                if (victim.HaveOffhandWeapon() && offtime < basetime)
                {
                    var percent20 = victim.GetBaseAttackTime(WeaponAttackType.OffAttack) * 0.20f;
                    var percent60 = 3.0f * percent20;
                    if (offtime > percent20 && offtime <= percent60)
                        victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)percent20);
                    else if (offtime > percent60)
                    {
                        offtime -= 2.0f * percent20;
                        victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)offtime);
                    }
                }
                else
                {
                    var percent20 = victim.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 0.20f;
                    var percent60 = 3.0f * percent20;
                    if (basetime > percent20 && basetime <= percent60)
                        victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)percent20);
                    else if (basetime > percent60)
                    {
                        basetime -= 2.0f * percent20;
                        victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)basetime);
                    }
                }
            }

            // Call default DealDamage
            var cleanDamage = new CleanDamage(damageInfo.cleanDamage, damageInfo.absorb, damageInfo.attackType, damageInfo.hitOutCome);
            DealDamage(victim, damageInfo.damage, cleanDamage, DamageEffectType.Direct, (SpellSchoolMask)damageInfo.damageSchoolMask, null, durabilityLoss);

            // If this is a creature and it attacks from behind it has a probability to daze it's victim
            if ((damageInfo.hitOutCome == MeleeHitOutcome.Crit || damageInfo.hitOutCome == MeleeHitOutcome.Crushing || damageInfo.hitOutCome == MeleeHitOutcome.Normal || damageInfo.hitOutCome == MeleeHitOutcome.Glancing) &&
                !IsTypeId(TypeId.Player) && !ToCreature().IsControlledByPlayer() && !victim.HasInArc(MathFunctions.PI, this)
                && (victim.IsTypeId(TypeId.Player) || !victim.ToCreature().IsWorldBoss()) && !victim.IsVehicle())
            {
                // 20% base chance
                var chance = 20.0f;

                // there is a newbie protection, at level 10 just 7% base chance; assuming linear function
                if (victim.GetLevel() < 30)
                    chance = 0.65f * victim.GetLevelForTarget(this) + 0.5f;

                uint victimDefense = victim.GetMaxSkillValueForLevel(this);
                uint attackerMeleeSkill = GetMaxSkillValueForLevel();

                chance *= attackerMeleeSkill / (float)victimDefense * 0.16f;

                // -probability is between 0% and 40%
                MathFunctions.RoundToInterval(ref chance, 0.0f, 40.0f);

                if (RandomHelper.randChance(chance))
                    CastSpell(victim, 1604, true);
            }

            if (IsTypeId(TypeId.Player))
            {
                var dmgInfo = new DamageInfo(damageInfo);
                ToPlayer().CastItemCombatSpell(dmgInfo);
            }

            // Do effect if any damage done to target
            if (damageInfo.damage != 0)
            {
                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vDamageShieldsCopy = victim.GetAuraEffectsByType(AuraType.DamageShield);
                foreach (var dmgShield in vDamageShieldsCopy)
                {
                    var spellInfo = dmgShield.GetSpellInfo();

                    // Damage shield can be resisted...
                    var missInfo = victim.SpellHitResult(this, spellInfo, false);
                    if (missInfo != SpellMissInfo.None)
                    {
                        victim.SendSpellMiss(this, spellInfo.Id, missInfo);
                        continue;
                    }

                    // ...or immuned
                    if (IsImmunedToDamage(spellInfo))
                    {
                        victim.SendSpellDamageImmune(this, spellInfo.Id, false);
                        continue;
                    }

                    var damage = (uint)dmgShield.GetAmount();
                    var caster = dmgShield.GetCaster();
                    if (caster)
                    {
                        damage = caster.SpellDamageBonusDone(this, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
                        damage = SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
                    }

                    var damageInfo1 = new DamageInfo(this, victim, damage, spellInfo, spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                    victim.CalcAbsorbResist(damageInfo1);
                    damage = damageInfo1.GetDamage();

                    victim.DealDamageMods(this, ref damage);

                    var damageShield = new SpellDamageShield();
                    damageShield.Attacker = victim.GetGUID();
                    damageShield.Defender = GetGUID();
                    damageShield.SpellID = spellInfo.Id;
                    damageShield.TotalDamage = damage;
                    damageShield.OriginalDamage = (int)damageInfo.originalDamage;
                    damageShield.OverKill = (uint)Math.Max(damage - GetHealth(), 0);
                    damageShield.SchoolMask = (uint)spellInfo.SchoolMask;
                    damageShield.LogAbsorbed = damageInfo1.GetAbsorb();

                    victim.DealDamage(this, damage, null, DamageEffectType.SpellDirect, spellInfo.GetSchoolMask(), spellInfo, true);
                    damageShield.LogData.Initialize(this);

                    victim.SendCombatLogMessage(damageShield);
                }
            }
        }
        public uint DealDamage(Unit victim, uint damage, CleanDamage cleanDamage = null, DamageEffectType damagetype = DamageEffectType.Direct, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal, SpellInfo spellProto = null, bool durabilityLoss = true)
        {
            if (victim.IsAIEnabled)
                victim.GetAI().DamageTaken(this, ref damage);

            if (IsAIEnabled)
                GetAI().DamageDealt(victim, ref damage, damagetype);

            // Hook for OnDamage Event
            Global.ScriptMgr.OnDamage(this, victim, ref damage);

            if (victim.IsTypeId(TypeId.Player) && this != victim)
            {
                // Signal to pets that their owner was attacked - except when DOT.
                if (damagetype != DamageEffectType.DOT)
                {
                    foreach (var controlled in victim.m_Controlled)
                    {
                        var cControlled = controlled.ToCreature();
                        if (cControlled != null)
                            if (cControlled.IsAIEnabled)
                                cControlled.GetAI().OwnerAttackedBy(this);
                    }
                }

                if (victim.ToPlayer().GetCommandStatus(PlayerCommandStates.God))
                    return 0;
            }

            if (damagetype != DamageEffectType.NoDamage)
            {
                // interrupting auras with AURA_INTERRUPT_FLAG_DAMAGE before checking !damage (absorbed damage breaks that type of auras)
                if (spellProto != null)
                {
                    if (!spellProto.HasAttribute(SpellAttr4.DamageDoesntBreakAuras))
                        victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.TakeDamage, spellProto.Id);
                }
                else
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.TakeDamage, 0);

                // interrupt spells with SPELL_INTERRUPT_FLAG_ABORT_ON_DMG on absorbed damage (no dots)
                if (damage == 0 && damagetype != DamageEffectType.DOT && cleanDamage != null && cleanDamage.absorbed_damage != 0)
                {
                    if (victim != this && victim.IsTypeId(TypeId.Player))
                    {
                        var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                        if (spell)
                        {
                            if (spell.GetState() == SpellState.Preparing)
                            {
                                var interruptFlags = spell.m_spellInfo.InterruptFlags;
                                if (interruptFlags.HasAnyFlag(SpellInterruptFlags.AbortOnDmg))
                                    victim.InterruptNonMeleeSpells(false);
                            }
                        }
                    }
                }

                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vCopyDamageCopy = victim.GetAuraEffectsByType(AuraType.ShareDamagePct);
                // copy damage to casters of this aura
                foreach (var aura in vCopyDamageCopy)
                {
                    // Check if aura was removed during iteration - we don't need to work on such auras
                    if (!(aura.GetBase().IsAppliedOnTarget(victim.GetGUID())))
                        continue;

                    // check damage school mask
                    if ((aura.GetMiscValue() & (int)damageSchoolMask) == 0)
                        continue;

                    var shareDamageTarget = aura.GetCaster();
                    if (shareDamageTarget == null)
                        continue;
                    var spell = aura.GetSpellInfo();

                    var share = MathFunctions.CalculatePct(damage, aura.GetAmount());

                    // @todo check packets if damage is done by victim, or by attacker of victim
                    DealDamageMods(shareDamageTarget, ref share);
                    DealDamage(shareDamageTarget, share, null, DamageEffectType.NoDamage, spell.GetSchoolMask(), spell, false);
                }
            }

            // Rage from Damage made (only from direct weapon damage)
            if (cleanDamage != null && (cleanDamage.attackType == WeaponAttackType.BaseAttack || cleanDamage.attackType == WeaponAttackType.OffAttack) && damagetype == DamageEffectType.Direct && this != victim && GetPowerType() == PowerType.Rage)
            {
                var rage = (uint)(GetBaseAttackTime(cleanDamage.attackType) / 1000.0f * 1.75f);
                if (cleanDamage.attackType == WeaponAttackType.OffAttack)
                    rage /= 2;
                RewardRage(rage);
            }

            if (damage == 0)
                return 0;

            Log.outDebug(LogFilter.Unit, "DealDamageStart");

            var health = (uint)victim.GetHealth();
            Log.outDebug(LogFilter.Unit, "Unit {0} dealt {1} damage to unit {2}", GetGUID(), damage, victim.GetGUID());

            // duel ends when player has 1 or less hp
            var duel_hasEnded = false;
            var duel_wasMounted = false;
            if (victim.IsTypeId(TypeId.Player) && victim.ToPlayer().duel != null && damage >= (health - 1))
            {
                // prevent kill only if killed in duel and killed by opponent or opponent controlled creature
                if (victim.ToPlayer().duel.opponent == this || victim.ToPlayer().duel.opponent.GetGUID() == GetOwnerGUID())
                    damage = health - 1;

                duel_hasEnded = true;
            }
            else if (victim.IsVehicle() && damage >= (health - 1) && victim.GetCharmer() != null && victim.GetCharmer().IsTypeId(TypeId.Player))
            {
                var victimRider = victim.GetCharmer().ToPlayer();
                if (victimRider != null && victimRider.duel != null && victimRider.duel.isMounted)
                {
                    // prevent kill only if killed in duel and killed by opponent or opponent controlled creature
                    if (victimRider.duel.opponent == this || victimRider.duel.opponent.GetGUID() == GetCharmerGUID())
                        damage = health - 1;

                    duel_wasMounted = true;
                    duel_hasEnded = true;
                }
            }

            if (IsTypeId(TypeId.Player) && this != victim)
            {
                var killer = ToPlayer();

                // in bg, count dmg if victim is also a player
                if (victim.IsTypeId(TypeId.Player))
                {
                    var bg = killer.GetBattleground();
                    if (bg)
                        bg.UpdatePlayerScore(killer, ScoreType.DamageDone, damage);
                }

                killer.UpdateCriteria(CriteriaTypes.DamageDone, health > damage ? damage : health, 0, 0, victim);
                killer.UpdateCriteria(CriteriaTypes.HighestHitDealt, damage);
            }

            if (victim.IsTypeId(TypeId.Player))
            {
                victim.ToPlayer().UpdateCriteria(CriteriaTypes.HighestHitReceived, damage);
            }

            damage /= (uint)victim.GetHealthMultiplierForTarget(this);

            if (victim.GetTypeId() != TypeId.Player && (!victim.IsControlledByPlayer() || victim.IsVehicle()))
            {
                if (!victim.ToCreature().HasLootRecipient())
                    victim.ToCreature().SetLootRecipient(this);

                if (IsControlledByPlayer())
                    victim.ToCreature().LowerPlayerDamageReq(health < damage ? health : damage);
            }

            if (health <= damage)
            {
                Log.outDebug(LogFilter.Unit, "DealDamage: victim just died");

                if (victim.IsTypeId(TypeId.Player) && victim != this)
                    victim.ToPlayer().UpdateCriteria(CriteriaTypes.TotalDamageReceived, health);

                Kill(victim, durabilityLoss);
            }
            else
            {
                Log.outDebug(LogFilter.Unit, "DealDamageAlive");

                if (victim.IsTypeId(TypeId.Player))
                    victim.ToPlayer().UpdateCriteria(CriteriaTypes.TotalDamageReceived, damage);

                victim.ModifyHealth(-(int)damage);

                if (damagetype == DamageEffectType.Direct || damagetype == DamageEffectType.SpellDirect)
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.DirectDamage, spellProto != null ? spellProto.Id : 0);

                if (!victim.IsTypeId(TypeId.Player))
                {
                    // Part of Evade mechanics. DoT's and Thorns / Retribution Aura do not contribute to this
                    if (damagetype != DamageEffectType.DOT && damage > 0 && !victim.GetOwnerGUID().IsPlayer() && (spellProto == null || !spellProto.HasAura(AuraType.DamageShield)))
                        victim.ToCreature().SetLastDamagedTime(GameTime.GetGameTime() + SharedConst.MaxAggroResetTime);

                    victim.GetThreatManager().AddThreat(this, damage, spellProto);
                }
                else                                                // victim is a player
                {
                    // random durability for items (HIT TAKEN)
                    if (WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage) > RandomHelper.randChance())
                    {
                        var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                        victim.ToPlayer().DurabilityPointLossForEquipSlot(slot);
                    }
                }

                if (IsTypeId(TypeId.Player))
                {
                    // random durability for items (HIT DONE)
                    if (RandomHelper.randChance(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage)))
                    {
                        var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                        ToPlayer().DurabilityPointLossForEquipSlot(slot);
                    }
                }

                if (damagetype != DamageEffectType.NoDamage && damage != 0)
                {
                    if (victim != this && victim.IsTypeId(TypeId.Player) && // does not support creature push_back
                        (spellProto == null || !(spellProto.HasAttribute(SpellAttr7.NoPushbackOnDamage))))
                    {
                        if (damagetype != DamageEffectType.DOT)
                        {
                            var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                            if (spell != null)
                                if (spell.GetState() == SpellState.Preparing)
                                {
                                    var interruptFlags = spell.m_spellInfo.InterruptFlags;
                                    if (interruptFlags.HasAnyFlag(SpellInterruptFlags.AbortOnDmg))
                                        victim.InterruptNonMeleeSpells(false);
                                    else if (interruptFlags.HasAnyFlag(SpellInterruptFlags.PushBack))
                                        spell.Delayed();
                                }
                        }
                        var spell1 = victim.GetCurrentSpell(CurrentSpellTypes.Channeled);
                        if (spell1 != null)
                            if (spell1.GetState() == SpellState.Casting && spell1.m_spellInfo.HasChannelInterruptFlag(SpellChannelInterruptFlags.Delay) && damagetype != DamageEffectType.DOT)
                                spell1.DelayedChannel();
                    }
                }
                // last damage from duel opponent
                if (duel_hasEnded)
                {
                    var he = duel_wasMounted ? victim.GetCharmer().ToPlayer() : victim.ToPlayer();

                    Cypher.Assert(he && he.duel != null);

                    if (duel_wasMounted) // In this case victim==mount
                        victim.SetHealth(1);
                    else
                        he.SetHealth(1);

                    he.duel.opponent.CombatStopWithPets(true);
                    he.CombatStopWithPets(true);

                    he.CastSpell(he, 7267, true);                  // beg
                    he.DuelComplete(DuelCompleteType.Won);
                }
            }

            Log.outDebug(LogFilter.Unit, "DealDamageEnd returned {0} damage", damage);

            return damage;
        }

        public long ModifyHealth(long dVal)
        {
            long gain = 0;

            if (dVal == 0)
                return 0;

            var curHealth = (long)GetHealth();

            var val = dVal + curHealth;
            if (val <= 0)
            {
                SetHealth(0);
                return -curHealth;
            }

            var maxHealth = (long)GetMaxHealth();
            if (val < maxHealth)
            {
                SetHealth((ulong)val);
                gain = val - curHealth;
            }
            else if (curHealth != maxHealth)
            {
                SetHealth((ulong)maxHealth);
                gain = maxHealth - curHealth;
            }

            if (dVal < 0)
            {
                var packet = new HealthUpdate();
                packet.Guid = GetGUID();
                packet.Health = (long)GetHealth();

                var player = GetCharmerOrOwnerPlayerOrPlayerItself();
                if (player)
                    player.SendPacket(packet);
            }

            return gain;
        }
        public long GetHealthGain(long dVal)
        {
            long gain = 0;

            if (dVal == 0)
                return 0;

            var curHealth = (long)GetHealth();

            var val = dVal + curHealth;
            if (val <= 0)
            {
                return -curHealth;
            }

            var maxHealth = (long)GetMaxHealth();

            if (val < maxHealth)
                gain = dVal;
            else if (curHealth != maxHealth)
                gain = maxHealth - curHealth;

            return gain;
        }

        public void SendAttackStateUpdate(HitInfo HitInfo, Unit target, SpellSchoolMask damageSchoolMask, uint Damage, uint AbsorbDamage, uint Resist, VictimState TargetState, uint BlockedAmount)
        {
            var dmgInfo = new CalcDamageInfo();
            dmgInfo.HitInfo = HitInfo;
            dmgInfo.attacker = this;
            dmgInfo.target = target;
            dmgInfo.damage = Damage - AbsorbDamage - Resist - BlockedAmount;
            dmgInfo.originalDamage = Damage;
            dmgInfo.damageSchoolMask = (uint)damageSchoolMask;
            dmgInfo.absorb = AbsorbDamage;
            dmgInfo.resist = Resist;
            dmgInfo.TargetState = TargetState;
            dmgInfo.blocked_amount = BlockedAmount;
            SendAttackStateUpdate(dmgInfo);
        }
        public void SendAttackStateUpdate(CalcDamageInfo damageInfo)
        {
            var packet = new AttackerStateUpdate();
            packet.hitInfo = damageInfo.HitInfo;
            packet.AttackerGUID = damageInfo.attacker.GetGUID();
            packet.VictimGUID = damageInfo.target.GetGUID();
            packet.Damage = (int)damageInfo.damage;
            packet.OriginalDamage = (int)damageInfo.originalDamage;
            var overkill = (int)(damageInfo.damage - damageInfo.target.GetHealth());
            packet.OverDamage = (overkill < 0 ? -1 : overkill);

            var subDmg = new SubDamage();
            subDmg.SchoolMask = (int)damageInfo.damageSchoolMask;   // School of sub damage
            subDmg.FDamage = damageInfo.damage;                // sub damage
            subDmg.Damage = (int)damageInfo.damage;                 // Sub Damage
            subDmg.Absorbed = (int)damageInfo.absorb;
            subDmg.Resisted = (int)damageInfo.resist;
            packet.SubDmg.Set(subDmg);

            packet.VictimState = (byte)damageInfo.TargetState;
            packet.BlockAmount = (int)damageInfo.blocked_amount;
            packet.LogData.Initialize(damageInfo.attacker);

            var contentTuningParams = new ContentTuningParams();
            if (contentTuningParams.GenerateDataForUnits(damageInfo.attacker, damageInfo.target))
                packet.ContentTuning = contentTuningParams;

            SendCombatLogMessage(packet);
        }
        public void CombatStart(Unit target, bool initialAggro = true)
        {
            if (initialAggro)
            {
                if (!target.IsStandState())
                    target.SetStandState(UnitStandStateType.Stand);

                if (!target.IsInCombat() && !target.IsTypeId(TypeId.Player)
                    && !target.ToCreature().HasReactState(ReactStates.Passive) && target.ToCreature().IsAIEnabled)
                        target.ToCreature().GetAI().AttackStart(this);

                SetInCombatWith(target);
                target.SetInCombatWith(this);
            }

            var me = GetCharmerOrOwnerPlayerOrPlayerItself();
            var who = target.GetCharmerOrOwnerOrSelf();
            if (me != null && who.IsTypeId(TypeId.Player))
                me.SetContestedPvP(who.ToPlayer());

            if (me != null && who.IsPvP() && (!who.IsTypeId(TypeId.Player) || me.duel == null || me.duel.opponent != who))
            {
                me.UpdatePvP(true);
                me.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);
            }
        }
        public void SetInCombatWith(Unit enemy)
        {
            var eOwner = enemy.GetCharmerOrOwnerOrSelf();
            if (eOwner.IsPvP())
            {
                SetInCombatState(true, enemy);
                return;
            }

            // check for duel
            if (eOwner.IsTypeId(TypeId.Player) && eOwner.ToPlayer().duel != null)
            {
                var myOwner = GetCharmerOrOwnerOrSelf();
                if (((Player)eOwner).duel.opponent == myOwner)
                {
                    SetInCombatState(true, enemy);
                    return;
                }
            }
            SetInCombatState(false, enemy);
        }
        public void SetInCombatState(bool PvP, Unit enemy = null)
        {
            // only alive units can be in combat
            if (!IsAlive())
                return;

            if (PvP)
            {
                combatTimer = 5000;
                var me = ToPlayer();
                if (me)
                    me.EnablePvpRules(true);
            }

            if (IsInCombat() || HasUnitState(UnitState.Evade))
                return;

            AddUnitFlag(UnitFlags.InCombat);

            var creature = ToCreature();
            if (creature != null)
            {
                // Set home position at place of engaging combat for escorted creatures
                if ((IsAIEnabled && creature.GetAI().IsEscorted()) || GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint ||
                    GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                    creature.SetHomePosition(GetPositionX(), GetPositionY(), GetPositionZ(), Orientation);

                if (enemy != null)
                {
                    if (IsAIEnabled)
                        creature.GetAI().EnterCombat(enemy);

                    if (creature.GetFormation() != null)
                        creature.GetFormation().MemberEngagingTarget(creature, enemy);
                }

                if (IsPet())
                {
                    UpdateSpeed(UnitMoveType.Run);
                    UpdateSpeed(UnitMoveType.Swim);
                    UpdateSpeed(UnitMoveType.Flight);
                }

                if (!creature.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.MountedCombatAllowed))
                    Dismount();
            }

            foreach (var unit in m_Controlled)
                unit.SetInCombatState(PvP, enemy);

            ProcSkillsAndAuras(enemy, ProcFlags.EnterCombat, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        internal void SendCombatLogMessage(CombatLogServerPacket combatLog)
        {
            var notifier = new CombatLogSender(this, combatLog, GetVisibilityRange());
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public void Kill(Unit victim, bool durabilityLoss = true)
        {
            // Prevent killing unit twice (and giving reward from kill twice)
            if (victim.GetHealth() == 0)
                return;

            // find player: owner of controlled `this` or `this` itself maybe
            var player = GetCharmerOrOwnerPlayerOrPlayerItself();
            var creature = victim.ToCreature();

            var isRewardAllowed = true;
            if (creature != null)
            {
                isRewardAllowed = creature.IsDamageEnoughForLootingAndReward();
                if (!isRewardAllowed)
                    creature.SetLootRecipient(null);
            }

            if (isRewardAllowed && creature != null && creature.GetLootRecipient() != null)
                player = creature.GetLootRecipient();

            // Exploit fix
            if (creature && creature.IsPet() && creature.GetOwnerGUID().IsPlayer())
                isRewardAllowed = false;

            // Reward player, his pets, and group/raid members
            // call kill spell proc event (before real die and combat stop to triggering auras removed at death/combat stop)
            if (isRewardAllowed && player != null && player != victim)
            {
                var partyKillLog = new PartyKillLog();
                partyKillLog.Player = player.GetGUID();
                partyKillLog.Victim = victim.GetGUID();

                var looter = player;
                var group = player.GetGroup();
                var hasLooterGuid = false;
                if (group)
                {
                    group.BroadcastPacket(partyKillLog, group.GetMemberGroup(player.GetGUID()) != 0);

                    if (creature)
                    {
                        group.UpdateLooterGuid(creature, true);
                        if (!group.GetLooterGuid().IsEmpty())
                        {
                            looter = Global.ObjAccessor.FindPlayer(group.GetLooterGuid());
                            if (looter)
                            {
                                hasLooterGuid = true;
                                creature.SetLootRecipient(looter);   // update creature loot recipient to the allowed looter.
                            }
                        }
                    }
                }
                else
                {
                    player.SendPacket(partyKillLog);

                    if (creature != null)
                    {
                        var lootList = new LootList();
                        lootList.Owner = creature.GetGUID();
                        lootList.LootObj = creature.loot.GetGUID();
                        player.SendMessageToSet(lootList, true);
                    }
                }

                if (creature)
                {
                    var loot = creature.loot;

                    loot.Clear();
                    var lootid = creature.GetCreatureTemplate().LootId;
                    if (lootid != 0)
                        loot.FillLoot(lootid, LootStorage.Creature, looter, false, false, creature.GetLootMode(), GetMap().GetDifficultyLootItemContext());

                    if (creature.GetLootMode() > 0)
                        loot.GenerateMoneyLoot(creature.GetCreatureTemplate().MinGold, creature.GetCreatureTemplate().MaxGold);

                    if (group)
                    {
                        if (hasLooterGuid)
                            group.SendLooter(creature, looter);
                        else
                            group.SendLooter(creature, null);

                        // Update round robin looter only if the creature had loot
                        if (!loot.Empty())
                            group.UpdateLooterGuid(creature);
                    }
                }

                player.RewardPlayerAndGroupAtKill(victim, false);
            }

            // Do KILL and KILLED procs. KILL proc is called only for the unit who landed the killing blow (and its owner - for pets and totems) regardless of who tapped the victim
            if (IsPet() || IsTotem())
            {
                // proc only once for victim
                var owner = GetOwner();
                if (owner != null)
                    owner.ProcSkillsAndAuras(victim, ProcFlags.Kill, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
            }

            if (!victim.IsCritter())
                ProcSkillsAndAuras(victim, ProcFlags.Kill, ProcFlags.Killed, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

            // Proc auras on death - must be before aura/combat remove
            victim.ProcSkillsAndAuras(victim, ProcFlags.None, ProcFlags.Death, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

            // update get killing blow achievements, must be done before setDeathState to be able to require auras on target
            // and before Spirit of Redemption as it also removes auras
            if (player != null)
                player.UpdateCriteria(CriteriaTypes.GetKillingBlows, 1, 0, 0, victim);

            Log.outDebug(LogFilter.Unit, "SET JUST_DIED");
            victim.SetDeathState(DeathState.JustDied);

            // Inform pets (if any) when player kills target)
            // MUST come after victim.setDeathState(JUST_DIED); or pet next target
            // selection will get stuck on same target and break pet react state
            if (player != null)
            {
                var pet = player.GetPet();
                if (pet != null && pet.IsAlive() && pet.IsControlled())
                    pet.GetAI().KilledUnit(victim);
            }

            // 10% durability loss on death
            // clean InHateListOf
            var plrVictim = victim.ToPlayer();
            if (plrVictim != null)
            {
                // remember victim PvP death for corpse type and corpse reclaim delay
                // at original death (not at SpiritOfRedemtionTalent timeout)
                plrVictim.SetPvPDeath(player != null);

                // only if not player and not controlled by player pet. And not at BG
                if ((durabilityLoss && player == null && !victim.ToPlayer().InBattleground()) || (player != null && WorldConfig.GetBoolValue(WorldCfg.DurabilityLossInPvp)))
                {
                    double baseLoss = WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath);
                    var loss = (uint)(baseLoss - (baseLoss * plrVictim.GetTotalAuraMultiplier(AuraType.ModDurabilityLoss)));
                    Log.outDebug(LogFilter.Unit, "We are dead, losing {0} percent durability", loss);
                    // Durability loss is calculated more accurately again for each item in Player.DurabilityLoss
                    plrVictim.DurabilityLossAll(baseLoss, false);
                    // durability lost message
                    SendDurabilityLoss(plrVictim, loss);
                }
                // Call KilledUnit for creatures
                if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                    ToCreature().GetAI().KilledUnit(victim);

                // last damage from non duel opponent or opponent controlled creature
                if (plrVictim.duel != null)
                {
                    plrVictim.duel.opponent.CombatStopWithPets(true);
                    plrVictim.CombatStopWithPets(true);
                    plrVictim.DuelComplete(DuelCompleteType.Interrupted);
                }
            }
            else                                                // creature died
            {
                Log.outDebug(LogFilter.Unit, "DealDamageNotPlayer");

                if (!creature.IsPet())
                {
                    creature.GetThreatManager().ClearAllThreat();

                    // must be after setDeathState which resets dynamic flags
                    if (!creature.loot.IsLooted())
                        creature.AddDynamicFlag(UnitDynFlags.Lootable);
                    else
                        creature.AllLootRemovedFromCorpse();
                }

                // Call KilledUnit for creatures, this needs to be called after the lootable flag is set
                if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                    ToCreature().GetAI().KilledUnit(victim);

                // Call creature just died function
                if (creature.IsAIEnabled)
                    creature.GetAI().JustDied(this);

                var summon = creature.ToTempSummon();
                if (summon != null)
                {
                    var summoner = summon.GetSummoner();
                    if (summoner != null)
                        if (summoner.IsTypeId(TypeId.Unit) && summoner.IsAIEnabled)
                            summoner.ToCreature().GetAI().SummonedCreatureDies(creature, this);
                }

                // Dungeon specific stuff, only applies to players killing creatures
                if (creature.GetInstanceId() != 0)
                {
                    var instanceMap = creature.GetMap();
                    var creditedPlayer = GetCharmerOrOwnerPlayerOrPlayerItself();
                    // @todo do instance binding anyway if the charmer/owner is offline

                    if (instanceMap.IsDungeon() && (creditedPlayer || this == victim))
                    {
                        if (instanceMap.IsRaidOrHeroicDungeon())
                        {
                            if (creature.GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.InstanceBind))
                                ((InstanceMap)instanceMap).PermBindAllPlayers();
                        }
                        else
                        {
                            // the reset time is set but not added to the scheduler
                            // until the players leave the instance
                            var resettime = GameTime.GetGameTime() + 2 * Time.Hour;
                            var save = Global.InstanceSaveMgr.GetInstanceSave(creature.GetInstanceId());
                            if (save != null)
                                if (save.GetResetTime() < resettime)
                                    save.SetResetTime(resettime);
                        }
                    }
                }
            }

            // outdoor pvp things, do these after setting the death state, else the player activity notify won't work... doh...
            // handle player kill only if not suicide (spirit of redemption for example)
            if (player != null && this != victim)
            {
                var pvp = player.GetOutdoorPvP();
                if (pvp != null)
                    pvp.HandleKill(player, victim);

                var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
                if (bf != null)
                    bf.HandleKill(player, victim);
            }

            // Battlegroundthings (do this at the end, so the death state flag will be properly set to handle in the bg.handlekill)
            if (player != null && player.InBattleground())
            {
                var bg = player.GetBattleground();
                if (bg)
                {
                    var playerVictim = victim.ToPlayer();
                    if (playerVictim)
                        bg.HandleKillPlayer(playerVictim, player);
                    else
                        bg.HandleKillUnit(victim.ToCreature(), player);
                }
            }

            // achievement stuff
            if (victim.IsTypeId(TypeId.Player))
            {
                if (IsTypeId(TypeId.Unit))
                    victim.ToPlayer().UpdateCriteria(CriteriaTypes.KilledByCreature, GetEntry());
                else if (IsTypeId(TypeId.Player) && victim != this)
                    victim.ToPlayer().UpdateCriteria(CriteriaTypes.KilledByPlayer, 1, (ulong)ToPlayer().GetTeam());
            }

            // Hook for OnPVPKill Event
            var killerPlr = ToPlayer();
            var killerCre = ToCreature();
            if (killerPlr != null)
            {
                var killedPlr = victim.ToPlayer();
                var killedCre = victim.ToCreature();
                if (killedPlr != null)
                    Global.ScriptMgr.OnPVPKill(killerPlr, killedPlr);
                else if (killedCre != null)
                    Global.ScriptMgr.OnCreatureKill(killerPlr, killedCre);
            }
            else if (killerCre != null)
            {
                var killed = victim.ToPlayer();
                if (killed != null)
                    Global.ScriptMgr.OnPlayerKilledByCreature(killerCre, killed);
            }
        }

        public void KillSelf(bool durabilityLoss = true) { Kill(this, durabilityLoss); }

        public virtual float GetBlockPercent(uint attackerLevel) { return 30.0f; }

        void UpdateReactives(uint p_time)
        {
            for (ReactiveType reactive = 0; reactive < ReactiveType.Max; ++reactive)
            {
                if (!m_reactiveTimer.ContainsKey(reactive))
                    continue;

                if (m_reactiveTimer[reactive] <= p_time)
                {
                    m_reactiveTimer[reactive] = 0;

                    switch (reactive)
                    {
                        case ReactiveType.Defense:
                            if (HasAuraState(AuraStateType.Defense))
                                ModifyAuraState(AuraStateType.Defense, false);
                            break;
                        case ReactiveType.HunterParry:
                            if (GetClass() == Class.Hunter && HasAuraState(AuraStateType.HunterParry))
                                ModifyAuraState(AuraStateType.HunterParry, false);
                            break;
                        case ReactiveType.OverPower:
                            if (GetClass() == Class.Warrior && IsTypeId(TypeId.Player))
                                ToPlayer().ClearComboPoints();
                            break;
                    }
                }
                else
                {
                    m_reactiveTimer[reactive] -= p_time;
                }
            }
        }

        public void RewardRage(uint baseRage)
        {
            float addRage = baseRage;

            // talent who gave more rage on attack
            MathFunctions.AddPct(ref addRage, GetTotalAuraModifier(AuraType.ModRageFromDamageDealt));

            addRage *= WorldConfig.GetFloatValue(WorldCfg.RatePowerRageIncome);

            ModifyPower(PowerType.Rage, (int)(addRage * 10));
        }

        public float GetPPMProcChance(uint WeaponSpeed, float PPM, SpellInfo spellProto)
        {
            // proc per minute chance calculation
            if (PPM <= 0)
                return 0.0f;

            // Apply chance modifer aura
            if (spellProto != null)
            {
                var modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellProto, SpellModOp.ProcPerMinute, ref PPM);
            }

            return (float)Math.Floor((WeaponSpeed * PPM) / 600.0f);   // result is chance in percents (probability = Speed_in_sec * (PPM / 60))
        }

        public Unit GetNextRandomRaidMemberOrPet(float radius)
        {
            Player player = null;
            if (IsTypeId(TypeId.Player))
                player = ToPlayer();
            // Should we enable this also for charmed units?
            else if (IsTypeId(TypeId.Unit) && IsPet())
                player = GetOwner().ToPlayer();

            if (player == null)
                return null;
            var group = player.GetGroup();
            // When there is no group check pet presence
            if (!group)
            {
                // We are pet now, return owner
                if (player != this)
                    return IsWithinDistInMap(player, radius) ? player : null;
                Unit pet = GetGuardianPet();
                // No pet, no group, nothing to return
                if (pet == null)
                    return null;
                // We are owner now, return pet
                return IsWithinDistInMap(pet, radius) ? pet : null;
            }

            var nearMembers = new List<Unit>();
            // reserve place for players and pets because resizing vector every unit push is unefficient (vector is reallocated then)

            for (var refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                var target = refe.GetSource();
                if (target)
                {
                    // IsHostileTo check duel and controlled by enemy
                    if (target != this && IsWithinDistInMap(target, radius) && target.IsAlive() && !IsHostileTo(target))
                        nearMembers.Add(target);

                    // Push player's pet to vector
                    Unit pet = target.GetGuardianPet();
                    if (pet)
                        if (pet != this && IsWithinDistInMap(pet, radius) && pet.IsAlive() && !IsHostileTo(pet))
                            nearMembers.Add(pet);
                }
            }

            if (nearMembers.Empty())
                return null;

            var randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);
            return nearMembers[randTarget];
        }

        public void ClearAllReactives()
        {
            for (ReactiveType i = 0; i < ReactiveType.Max; ++i)
                m_reactiveTimer[i] = 0;

            if (HasAuraState(AuraStateType.Defense))
                ModifyAuraState(AuraStateType.Defense, false);
            if (GetClass() == Class.Hunter && HasAuraState(AuraStateType.HunterParry))
                ModifyAuraState(AuraStateType.HunterParry, false);
            if (GetClass() == Class.Warrior && IsTypeId(TypeId.Player))
                ToPlayer().ClearComboPoints();
        }

        public virtual bool CanUseAttackType(WeaponAttackType attacktype)
        {
            switch (attacktype)
            {
                case WeaponAttackType.BaseAttack:
                    return !HasUnitFlag(UnitFlags.Disarmed);
                case WeaponAttackType.OffAttack:
                    return !HasUnitFlag2(UnitFlags2.DisarmOffhand);
                case WeaponAttackType.RangedAttack:
                    return !HasUnitFlag2(UnitFlags2.DisarmRanged);
                default:
                    return true;
            }
        }

        // TODO for melee need create structure as in
        void CalculateMeleeDamage(Unit victim, uint damage, out CalcDamageInfo damageInfo, WeaponAttackType attackType)
        {
            damageInfo = new CalcDamageInfo();

            damageInfo.attacker = this;
            damageInfo.target = victim;
            damageInfo.damageSchoolMask = (uint)SpellSchoolMask.Normal;
            damageInfo.attackType = attackType;
            damageInfo.damage = 0;
            damageInfo.originalDamage = 0;
            damageInfo.cleanDamage = 0;
            damageInfo.absorb = 0;
            damageInfo.resist = 0;
            damageInfo.blocked_amount = 0;

            damageInfo.TargetState = 0;
            damageInfo.HitInfo = 0;
            damageInfo.procAttacker = ProcFlags.None;
            damageInfo.procVictim = ProcFlags.None;
            damageInfo.hitOutCome = MeleeHitOutcome.Evade;

            if (victim == null)
                return;

            if (!IsAlive() || !victim.IsAlive())
                return;

            // Select HitInfo/procAttacker/procVictim flag based on attack type
            switch (attackType)
            {
                case WeaponAttackType.BaseAttack:
                    damageInfo.procAttacker = ProcFlags.DoneMeleeAutoAttack | ProcFlags.DoneMainHandAttack;
                    damageInfo.procVictim = ProcFlags.TakenMeleeAutoAttack;
                    break;
                case WeaponAttackType.OffAttack:
                    damageInfo.procAttacker = ProcFlags.DoneMeleeAutoAttack | ProcFlags.DoneOffHandAttack;
                    damageInfo.procVictim = ProcFlags.TakenMeleeAutoAttack;
                    damageInfo.HitInfo = HitInfo.OffHand;
                    break;
                default:
                    return;
            }

            // Physical Immune check
            if (damageInfo.target.IsImmunedToDamage((SpellSchoolMask)damageInfo.damageSchoolMask))
            {
                damageInfo.HitInfo |= HitInfo.NormalSwing;
                damageInfo.TargetState = VictimState.Immune;

                damageInfo.damage = 0;
                damageInfo.cleanDamage = 0;
                return;
            }

            damage += CalculateDamage(damageInfo.attackType, false, true);
            // Add melee damage bonus
            damage = MeleeDamageBonusDone(damageInfo.target, damage, damageInfo.attackType);
            damage = damageInfo.target.MeleeDamageBonusTaken(this, damage, damageInfo.attackType, DamageEffectType.Direct);

            // Script Hook For CalculateMeleeDamage -- Allow scripts to change the Damage pre class mitigation calculations
            Global.ScriptMgr.ModifyMeleeDamage(damageInfo.target, damageInfo.attacker, ref damage);

            // Calculate armor reduction
            if (IsDamageReducedByArmor((SpellSchoolMask)damageInfo.damageSchoolMask))
            {
                damageInfo.damage = CalcArmorReducedDamage(damageInfo.attacker, damageInfo.target, damage, null, damageInfo.attackType);
                damageInfo.cleanDamage += damage - damageInfo.damage;
            }
            else
                damageInfo.damage = damage;

            damageInfo.hitOutCome = RollMeleeOutcomeAgainst(damageInfo.target, damageInfo.attackType);

            switch (damageInfo.hitOutCome)
            {
                case MeleeHitOutcome.Evade:
                    damageInfo.HitInfo |= HitInfo.Miss | HitInfo.SwingNoHitSound;
                    damageInfo.TargetState = VictimState.Evades;
                    damageInfo.originalDamage = damageInfo.damage;
                    damageInfo.damage = 0;
                    damageInfo.cleanDamage = 0;
                    return;
                case MeleeHitOutcome.Miss:
                    damageInfo.HitInfo |= HitInfo.Miss;
                    damageInfo.TargetState = VictimState.Intact;
                    damageInfo.originalDamage = damageInfo.damage;
                    damageInfo.damage = 0;
                    damageInfo.cleanDamage = 0;
                    break;
                case MeleeHitOutcome.Normal:
                    damageInfo.TargetState = VictimState.Hit;
                    damageInfo.originalDamage = damageInfo.damage;
                    break;
                case MeleeHitOutcome.Crit:
                    damageInfo.HitInfo |= HitInfo.CriticalHit;
                    damageInfo.TargetState = VictimState.Hit;
                    // Crit bonus calc
                    damageInfo.damage += damageInfo.damage;

                    // Increase crit damage from SPELL_AURA_MOD_CRIT_DAMAGE_BONUS
                    var mod = (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, damageInfo.damageSchoolMask) - 1.0f) * 100;

                    if (mod != 0)
                        MathFunctions.AddPct(ref damageInfo.damage, mod);

                    damageInfo.originalDamage = damageInfo.damage;
                    break;
                case MeleeHitOutcome.Parry:
                    damageInfo.TargetState = VictimState.Parry;
                    damageInfo.originalDamage = damageInfo.damage;
                    damageInfo.cleanDamage += damageInfo.damage;
                    damageInfo.damage = 0;
                    break;
                case MeleeHitOutcome.Dodge:
                    damageInfo.TargetState = VictimState.Dodge;
                    damageInfo.originalDamage = damageInfo.damage;
                    damageInfo.cleanDamage += damageInfo.damage;
                    damageInfo.damage = 0;
                    break;
                case MeleeHitOutcome.Block:
                    damageInfo.TargetState = VictimState.Hit;
                    damageInfo.HitInfo |= HitInfo.Block;
                    damageInfo.originalDamage = damageInfo.damage;
                    // 30% damage blocked, double blocked amount if block is critical
                    damageInfo.blocked_amount = MathFunctions.CalculatePct(damageInfo.damage, damageInfo.target.IsBlockCritical() ? damageInfo.target.GetBlockPercent(GetLevel()) * 2 : damageInfo.target.GetBlockPercent(GetLevel()));
                    damageInfo.damage -= damageInfo.blocked_amount;
                    damageInfo.cleanDamage += damageInfo.blocked_amount;
                    break;
                case MeleeHitOutcome.Glancing:
                    damageInfo.HitInfo |= HitInfo.Glancing;
                    damageInfo.TargetState = VictimState.Hit;
                    damageInfo.originalDamage = damageInfo.damage;
                    var leveldif = (int)victim.GetLevel() - (int)GetLevel();
                    if (leveldif > 3)
                        leveldif = 3;

                    var reducePercent = 1.0f - leveldif * 0.1f;
                    damageInfo.cleanDamage += damageInfo.damage - (uint)(reducePercent * damageInfo.damage);
                    damageInfo.damage = (uint)(reducePercent * damageInfo.damage);
                    break;
                case MeleeHitOutcome.Crushing:
                    damageInfo.HitInfo |= HitInfo.Crushing;
                    damageInfo.TargetState = VictimState.Hit;
                    // 150% normal damage
                    damageInfo.damage += (damageInfo.damage / 2);
                    damageInfo.originalDamage = damageInfo.damage;
                    break;

                default:
                    break;
            }

            // Always apply HITINFO_AFFECTS_VICTIM in case its not a miss
            if (!damageInfo.HitInfo.HasAnyFlag(HitInfo.Miss))
                damageInfo.HitInfo |= HitInfo.AffectsVictim;

            var resilienceReduction = damageInfo.damage;
            ApplyResilience(victim, ref resilienceReduction);
            resilienceReduction = damageInfo.damage - resilienceReduction;
            damageInfo.damage -= resilienceReduction;
            damageInfo.cleanDamage += resilienceReduction;

            // Calculate absorb resist
            if (damageInfo.damage > 0)
            {
                damageInfo.procVictim |= ProcFlags.TakenDamage;
                // Calculate absorb & resists
                var dmgInfo = new DamageInfo(damageInfo);
                CalcAbsorbResist(dmgInfo);
                damageInfo.absorb = dmgInfo.GetAbsorb();
                damageInfo.resist = dmgInfo.GetResist();

                if (damageInfo.absorb != 0)
                    damageInfo.HitInfo |= (damageInfo.damage - damageInfo.absorb == 0 ? HitInfo.FullAbsorb : HitInfo.PartialAbsorb);

                if (damageInfo.resist != 0)
                    damageInfo.HitInfo |= (damageInfo.damage - damageInfo.resist == 0 ? HitInfo.FullResist : HitInfo.PartialResist);

                damageInfo.damage = dmgInfo.GetDamage();
            }
            else // Impossible get negative result but....
                damageInfo.damage = 0;
        }
        MeleeHitOutcome RollMeleeOutcomeAgainst(Unit victim, WeaponAttackType attType)
        {
            if (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks())
                return MeleeHitOutcome.Evade;

            // Miss chance based on melee
            var miss_chance = (int)(MeleeSpellMissChance(victim, attType, null) * 100.0f);

            // Critical hit chance
            var crit_chance = (int)(GetUnitCriticalChance(attType, victim) + GetTotalAuraModifier(AuraType.ModAutoAttackCritChance) * 100.0f);

            var dodge_chance = (int)(GetUnitDodgeChance(attType, victim) * 100.0f);
            var block_chance = (int)(GetUnitBlockChance(attType, victim) * 100.0f);
            var parry_chance = (int)(GetUnitParryChance(attType, victim) * 100.0f);

            // melee attack table implementation
            // outcome priority:
            //   1. >    2. >    3. >       4. >    5. >   6. >       7. >  8.
            // MISS > DODGE > PARRY > GLANCING > BLOCK > CRIT > CRUSHING > HIT

            int sum = 0, tmp = 0;
            var roll = RandomHelper.IRand(0, 9999);

            var attackerLevel = GetLevelForTarget(victim);
            var victimLevel = GetLevelForTarget(this);

            // check if attack comes from behind, nobody can parry or block if attacker is behind
            var canParryOrBlock = victim.HasInArc((float)Math.PI, this) || victim.HasAuraType(AuraType.IgnoreHitDirection);

            // only creatures can dodge if attacker is behind
            var canDodge = !victim.IsTypeId(TypeId.Player) || canParryOrBlock;

            // if victim is casting or cc'd it can't avoid attacks
            if (victim.IsNonMeleeSpellCast(false) || victim.HasUnitState(UnitState.Controlled))
            {
                canDodge = false;
                canParryOrBlock = false;
            }

            // 1. MISS
            tmp = miss_chance;
            if (tmp > 0 && roll < (sum += tmp))
                return MeleeHitOutcome.Miss;

            // always crit against a sitting target (except 0 crit chance)
            if (victim.IsTypeId(TypeId.Player) && crit_chance > 0 && !victim.IsStandState())
                return MeleeHitOutcome.Crit;

            // 2. DODGE
            if (canDodge)
            {
                tmp = dodge_chance;
                if (tmp > 0                                         // check if unit _can_ dodge
                    && roll < (sum += tmp))
                    return MeleeHitOutcome.Dodge;
            }

            // 3. PARRY
            if (canParryOrBlock)
            {
                tmp = parry_chance;
                if (tmp > 0                                         // check if unit _can_ parry
                    && roll < (sum += tmp))
                    return MeleeHitOutcome.Parry;
            }

            // 4. GLANCING
            // Max 40% chance to score a glancing blow against mobs that are higher level (can do only players and pets and not with ranged weapon)
            if ((IsTypeId(TypeId.Player) || IsPet()) &&
                !victim.IsTypeId(TypeId.Player) && !victim.IsPet() &&
                attackerLevel + 3 < victimLevel)
            {
                // cap possible value (with bonuses > max skill)
                tmp = (int)(10 + 10 * (victimLevel - attackerLevel)) * 100;
                if (tmp > 0 && roll < (sum += tmp))
                    return MeleeHitOutcome.Glancing;
            }

            // 5. BLOCK
            if (canParryOrBlock)
            {
                tmp = block_chance;
                if (tmp > 0                                          // check if unit _can_ block
                    && roll < (sum += tmp))
                    return MeleeHitOutcome.Block;
            }

            // 6.CRIT
            tmp = crit_chance;
            if (tmp > 0 && roll < (sum += tmp))
                return MeleeHitOutcome.Crit;

            // 7. CRUSHING
            // mobs can score crushing blows if they're 4 or more levels above victim
            if (attackerLevel >= victimLevel + 4 &&
                // can be from by creature (if can) or from controlled player that considered as creature
                !IsControlledByPlayer() &&
                !(GetTypeId() == TypeId.Unit && ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoCrush)))
            {
                // add 2% chance per level, min. is 15%
                tmp = (int)(attackerLevel - victimLevel * 1000 - 1500);
                if (roll < (sum += tmp))
                {
                    Log.outDebug(LogFilter.Unit, "RollMeleeOutcomeAgainst: CRUSHING <{0}, {1})", sum - tmp, sum);
                    return MeleeHitOutcome.Crushing;
                }
            }

            // 8. HIT
            return MeleeHitOutcome.Normal;
        }
        public uint CalculateDamage(WeaponAttackType attType, bool normalized, bool addTotalPct)
        { 
            float minDamage;
            float maxDamage;

            if (normalized || !addTotalPct)
            {
                CalculateMinMaxDamage(attType, normalized, addTotalPct, out minDamage, out maxDamage);
                if (IsInFeralForm() && attType == WeaponAttackType.BaseAttack)
                {
                    CalculateMinMaxDamage(WeaponAttackType.OffAttack, normalized, addTotalPct, out var minOffhandDamage, out var maxOffhandDamage);
                    minDamage += minOffhandDamage;
                    maxDamage += maxOffhandDamage;
                }
            }
            else
            {
                switch (attType)
                {
                    case WeaponAttackType.RangedAttack:
                        minDamage = m_unitData.MinRangedDamage;
                        maxDamage = m_unitData.MaxRangedDamage;
                        break;
                    case WeaponAttackType.BaseAttack:
                        minDamage = m_unitData.MinDamage;
                        maxDamage = m_unitData.MaxDamage;
                        if (IsInFeralForm())
                        {
                            minDamage += m_unitData.MinOffHandDamage;
                            maxDamage += m_unitData.MaxOffHandDamage;
                        }
                        break;
                    case WeaponAttackType.OffAttack:
                        minDamage = m_unitData.MinOffHandDamage;
                        maxDamage = m_unitData.MaxOffHandDamage;
                        break;
                    // Just for good manner
                    default:
                        minDamage = 0.0f;
                        maxDamage = 0.0f;
                        break;
                }
            }
            minDamage = Math.Max(0.0f, minDamage);
            maxDamage = Math.Max(0.0f, maxDamage);

            if (minDamage > maxDamage)
            {
                minDamage = minDamage + maxDamage;
                maxDamage = minDamage - maxDamage;
                minDamage = minDamage - maxDamage;
            }

            if (maxDamage == 0.0f)
                maxDamage = 5.0f;

            return RandomHelper.URand(minDamage, maxDamage);
        }
        public float GetWeaponDamageRange(WeaponAttackType attType, WeaponDamageRange type)
        {
            if (attType == WeaponAttackType.OffAttack && !HaveOffhandWeapon())
                return 0.0f;

            return m_weaponDamage[(int)attType][(int)type];
        }
        public float GetAPMultiplier(WeaponAttackType attType, bool normalized)
        {
            if (!IsTypeId(TypeId.Player) || (IsInFeralForm() && !normalized))
                return GetBaseAttackTime(attType) / 1000.0f;

            var weapon = ToPlayer().GetWeaponForAttack(attType, true);
            if (!weapon)
                return 2.0f;

            if (!normalized)
                return weapon.GetTemplate().GetDelay() / 1000.0f;

            switch ((ItemSubClassWeapon)weapon.GetTemplate().GetSubClass())
            {
                case ItemSubClassWeapon.Axe2:
                case ItemSubClassWeapon.Mace2:
                case ItemSubClassWeapon.Polearm:
                case ItemSubClassWeapon.Sword2:
                case ItemSubClassWeapon.Staff:
                case ItemSubClassWeapon.FishingPole:
                    return 3.3f;
                case ItemSubClassWeapon.Axe:
                case ItemSubClassWeapon.Mace:
                case ItemSubClassWeapon.Sword:
                case ItemSubClassWeapon.Warglaives:
                case ItemSubClassWeapon.Exotic:
                case ItemSubClassWeapon.Exotic2:
                case ItemSubClassWeapon.Fist:
                    return 2.4f;
                case ItemSubClassWeapon.Dagger:
                    return 1.7f;
                case ItemSubClassWeapon.Thrown:
                    return 2.0f;
                default:
                    return weapon.GetTemplate().GetDelay() / 1000.0f;
            }
        }
        public float GetTotalAttackPowerValue(WeaponAttackType attType, bool includeWeapon = true)
        {
            if (attType == WeaponAttackType.RangedAttack)
            {
                float ap = m_unitData.RangedAttackPower + m_unitData.RangedAttackPowerModPos + m_unitData.RangedAttackPowerModNeg;
                if (includeWeapon)
                    ap += Math.Max(m_unitData.MainHandWeaponAttackPower, m_unitData.RangedWeaponAttackPower);
                if (ap < 0)
                    return 0.0f;
                return ap * (1.0f + m_unitData.RangedAttackPowerMultiplier);
            }
            else
            {
                float ap = m_unitData.AttackPower + m_unitData.AttackPowerModPos + m_unitData.AttackPowerModNeg;
                if (includeWeapon)
                {
                    if (attType == WeaponAttackType.BaseAttack)
                        ap += Math.Max(m_unitData.MainHandWeaponAttackPower, m_unitData.RangedWeaponAttackPower);
                    else
                    {
                        ap += m_unitData.OffHandWeaponAttackPower;
                        ap /= 2;
                    }
                }
                if (ap < 0)
                    return 0.0f;
                return ap * (1.0f + m_unitData.AttackPowerMultiplier);
            }
        }
        public bool IsWithinMeleeRange(Unit obj)
        {
            if (!obj || !IsInMap(obj) || !IsInPhase(obj))
                return false;

            var dx = GetPositionX() - obj.GetPositionX();
            var dy = GetPositionY() - obj.GetPositionY();
            var dz = GetPositionZMinusOffset() - obj.GetPositionZMinusOffset();
            var distsq = (dx * dx) + (dy * dy) + (dz * dz);

            var maxdist = GetMeleeRange(obj) + GetTotalAuraModifier(AuraType.ModAutoAttackRange);

            return distsq <= maxdist * maxdist;
        }

        public float GetMeleeRange(Unit target)
        {
            var range = GetCombatReach() + target.GetCombatReach() + 4.0f / 3.0f;
            return Math.Max(range, SharedConst.NominalMeleeRange);
        }

        public void SetBaseAttackTime(WeaponAttackType att, uint val)
        {
            m_baseAttackSpeed[(int)att] = val;
            UpdateAttackTimeField(att);
        }
        void UpdateAttackTimeField(WeaponAttackType att)
        {
            switch (att)
            {
                case WeaponAttackType.BaseAttack:
                case WeaponAttackType.OffAttack:
                    SetUpdateFieldValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.AttackRoundBaseTime, (int)att), (uint)(m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att]));
                    break;
                case WeaponAttackType.RangedAttack:
                    SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.RangedAttackRoundBaseTime), (uint)(m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att]));
                    break;
                default:
                    break; ;
            }
        }
        public virtual void SetPvP(bool state)
        {
            if (state)
                AddPvpFlag(UnitPVPStateFlags.PvP);
            else
                RemovePvpFlag(UnitPVPStateFlags.PvP);
        }

        public bool CanHaveThreatList(bool skipAliveCheck = false)
        {
            // only creatures can have threat list
            if (!IsTypeId(TypeId.Unit))
                return false;

            // only alive units can have threat list
            if (!skipAliveCheck && !IsAlive())
                return false;

            // totems can not have threat list
            if (IsTotem())
                return false;

            // summons can not have a threat list, unless they are controlled by a creature
            if (HasUnitTypeMask(UnitTypeMask.Minion | UnitTypeMask.Guardian | UnitTypeMask.ControlableGuardian) && GetOwnerGUID().IsPlayer())
                return false;

            return true;
        }

        uint CalcSpellResistedDamage(Unit attacker, Unit victim, uint damage, SpellSchoolMask schoolMask, SpellInfo spellInfo)
        {
            // Magic damage, check for resists
            if (!Convert.ToBoolean(schoolMask & SpellSchoolMask.Magic))
                return 0;

            // Npcs can have holy resistance
            if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy) && victim.GetTypeId() != TypeId.Unit)
                return 0;

            // Ignore spells that can't be resisted
            if (spellInfo != null)
            {
                if (spellInfo.HasAttribute(SpellAttr4.IgnoreResistances))
                    return 0;

                // Binary spells can't have damage part resisted
                if (spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell))
                    return 0;
            }

            var averageResist = CalculateAverageResistReduction(schoolMask, victim, spellInfo);

            var discreteResistProbability = new float[11];
            if (averageResist <= 0.1f)
            {
                discreteResistProbability[0] = 1.0f - 7.5f * averageResist;
                discreteResistProbability[1] = 5.0f * averageResist;
                discreteResistProbability[2] = 2.5f * averageResist;
            }
            else
            {
                for (uint i = 0; i < 11; ++i)
                    discreteResistProbability[i] = Math.Max(0.5f - 2.5f * Math.Abs(0.1f * i - averageResist), 0.0f);
            }

            var roll = (float)RandomHelper.NextDouble();
            var probabilitySum = 0.0f;

            uint resistance = 0;
            for (; resistance < 11; ++resistance)
                if (roll < (probabilitySum += discreteResistProbability[resistance]))
                    break;

            var damageResisted = damage * resistance / 10f;
            if (damageResisted > 0.0f) // if any damage was resisted
            {
                var ignoredResistance = 0;

                ignoredResistance += GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)schoolMask);

                ignoredResistance = Math.Min(ignoredResistance, 100);
                MathFunctions.ApplyPct(ref damageResisted, 100 - ignoredResistance);

                // Spells with melee and magic school mask, decide whether resistance or armor absorb is higher
                if (spellInfo != null && spellInfo.HasAttribute(SpellCustomAttributes.SchoolmaskNormalWithMagic))
                {
                    var damageAfterArmor = CalcArmorReducedDamage(attacker, victim, damage, spellInfo, WeaponAttackType.BaseAttack);
                    float armorReduction = damage - damageAfterArmor;

                    // pick the lower one, the weakest resistance counts
                    damageResisted = Math.Min(damageResisted, armorReduction);
                }
            }

            damageResisted = Math.Max(damageResisted, 0.0f);
            return (uint)damageResisted;
        }

        float CalculateAverageResistReduction(SpellSchoolMask schoolMask, Unit victim, SpellInfo spellInfo)
        {
            var victimResistance = (float)victim.GetResistance(schoolMask);

            // pets inherit 100% of masters penetration
            // excluding traps
            var player = GetSpellModOwner();
            if (player != null && GetEntry() != SharedConst.WorldTrigger)
            {
                victimResistance += (float)player.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
                victimResistance -= (float)player.GetSpellPenetrationItemMod();
            }
            else
                victimResistance += (float)GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);

            // holy resistance exists in pve and comes from level difference, ignore template values
            if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy))
                victimResistance = 0.0f;

            // Chaos Bolt exception, ignore all target resistances (unknown attribute?)
            if (spellInfo != null && spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && spellInfo.Id == 116858)
                victimResistance = 0.0f;

            victimResistance = Math.Max(victimResistance, 0.0f);

            // level-based resistance does not apply to binary spells, and cannot be overcome by spell penetration
            if (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell))
                victimResistance += Math.Max(((float)victim.GetLevelForTarget(this) - (float)GetLevelForTarget(victim)) * 5.0f, 0.0f);

            uint bossLevel = 83;
            var bossResistanceConstant = 510.0f;
            var level = victim.GetLevelForTarget(this);
            float resistanceConstant;

            if (level == bossLevel)
                resistanceConstant = bossResistanceConstant;
            else
                resistanceConstant = level * 5.0f;

            return victimResistance / (victimResistance + resistanceConstant);
        }

        public void CalcAbsorbResist(DamageInfo damageInfo)
        {
            if (!damageInfo.GetVictim() || !damageInfo.GetVictim().IsAlive() || damageInfo.GetDamage() == 0)
                return;

            var resistedDamage = CalcSpellResistedDamage(damageInfo.GetAttacker(), damageInfo.GetVictim(), damageInfo.GetDamage(), damageInfo.GetSchoolMask(), damageInfo.GetSpellInfo());
            damageInfo.ResistDamage(resistedDamage);

            // Ignore Absorption Auras
            float auraAbsorbMod = GetMaxPositiveAuraModifierByMiscMask(AuraType.ModTargetAbsorbSchool, (uint)damageInfo.GetSchoolMask());

            MathFunctions.RoundToInterval(ref auraAbsorbMod, 0.0f, 100.0f);

            var absorbIgnoringDamage = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), auraAbsorbMod);
            damageInfo.ModifyDamage(-absorbIgnoringDamage);

            // We're going to call functions which can modify content of the list during iteration over it's elements
            // Let's copy the list so we can prevent iterator invalidation
            var vSchoolAbsorbCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.SchoolAbsorb);
            vSchoolAbsorbCopy.Sort(new AbsorbAuraOrderPred());

            // absorb without mana cost
            for (var i = 0; i < vSchoolAbsorbCopy.Count; ++i)
            {
                var absorbAurEff = vSchoolAbsorbCopy[i];
                if (damageInfo.GetDamage() == 0)
                    break;

                // Check if aura was removed during iteration - we don't need to work on such auras
                var aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                if (aurApp == null)
                    continue;
                if (!Convert.ToBoolean(absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    continue;

                // get amount which can be still absorbed by the aura
                var currentAbsorb = absorbAurEff.GetAmount();
                // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                var tempAbsorb = (uint)currentAbsorb;

                var defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (defaultPrevented)
                    continue;

                // absorb must be smaller than the damage itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.GetDamage());

                damageInfo.AbsorbDamage((uint)currentAbsorb);

                tempAbsorb = (uint)currentAbsorb;
                absorbAurEff.GetBase().CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                // Check if our aura is using amount to count damage
                if (absorbAurEff.GetAmount() >= 0)
                {
                    // Reduce shield amount
                    absorbAurEff.ChangeAmount(absorbAurEff.GetAmount() - currentAbsorb);
                    // Aura cannot absorb anything more - remove it
                    if (absorbAurEff.GetAmount() <= 0)
                        absorbAurEff.GetBase().Remove(AuraRemoveMode.EnemySpell);
                }
            }

            // absorb by mana cost
            var vManaShieldCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.ManaShield);
            foreach (var absorbAurEff in vManaShieldCopy)
            {
                if (damageInfo.GetDamage() == 0)
                    break;

                // Check if aura was removed during iteration - we don't need to work on such auras
                var aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                if (aurApp == null)
                    continue;
                // check damage school mask
                if (!Convert.ToBoolean(absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    continue;

                // get amount which can be still absorbed by the aura
                var currentAbsorb = absorbAurEff.GetAmount();
                // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                var tempAbsorb = (uint)currentAbsorb;

                var defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (defaultPrevented)
                    continue;

                // absorb must be smaller than the damage itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.GetDamage());

                var manaReduction = currentAbsorb;

                // lower absorb amount by talents
                var manaMultiplier = absorbAurEff.GetSpellEffectInfo().CalcValueMultiplier(absorbAurEff.GetCaster());
                if (manaMultiplier != 0)
                    manaReduction = (int)(manaReduction * manaMultiplier);

                var manaTaken = -damageInfo.GetVictim().ModifyPower(PowerType.Mana, -manaReduction);

                // take case when mana has ended up into account
                currentAbsorb = currentAbsorb != 0 ? (currentAbsorb * (manaTaken / manaReduction)) : 0;

                damageInfo.AbsorbDamage((uint)currentAbsorb);

                tempAbsorb = (uint)currentAbsorb;
                absorbAurEff.GetBase().CallScriptEffectAfterManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                // Check if our aura is using amount to count damage
                if (absorbAurEff.GetAmount() >= 0)
                {
                    absorbAurEff.ChangeAmount(absorbAurEff.GetAmount() - currentAbsorb);
                    if ((absorbAurEff.GetAmount() <= 0))
                        absorbAurEff.GetBase().Remove(AuraRemoveMode.EnemySpell);
                }
            }

            damageInfo.ModifyDamage(absorbIgnoringDamage);

            // split damage auras - only when not damaging self
            if (damageInfo.GetVictim() != this)
            {
                // We're going to call functions which can modify content of the list during iteration over it's elements
                // Let's copy the list so we can prevent iterator invalidation
                var vSplitDamagePctCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.SplitDamagePct);
                foreach (var itr in vSplitDamagePctCopy)
                {
                    if (damageInfo.GetDamage() == 0)
                        break;

                    // Check if aura was removed during iteration - we don't need to work on such auras
                    var aurApp = itr.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                    if (aurApp == null)
                        continue;

                    // check damage school mask
                    if (!Convert.ToBoolean(itr.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                        continue;

                    // Damage can be splitted only if aura has an alive caster
                    var caster = itr.GetCaster();
                    if (!caster || (caster == damageInfo.GetVictim()) || !caster.IsInWorld || !caster.IsAlive())
                        continue;

                    var splitDamage = MathFunctions.CalculatePct(damageInfo.GetDamage(), itr.GetAmount());

                    itr.GetBase().CallScriptEffectSplitHandlers(itr, aurApp, damageInfo, splitDamage);

                    // absorb must be smaller than the damage itself
                    splitDamage = MathFunctions.RoundToInterval(ref splitDamage, 0, damageInfo.GetDamage());

                    damageInfo.AbsorbDamage(splitDamage);

                    // check if caster is immune to damage
                    if (caster.IsImmunedToDamage(damageInfo.GetSchoolMask()))
                    {
                        damageInfo.GetVictim().SendSpellMiss(caster, itr.GetSpellInfo().Id, SpellMissInfo.Immune);
                        continue;
                    }

                    uint split_absorb = 0;
                    DealDamageMods(caster, ref splitDamage, ref split_absorb);

                    var log = new SpellNonMeleeDamage(this, caster, itr.GetSpellInfo(), itr.GetBase().GetSpellVisual(), damageInfo.GetSchoolMask(), itr.GetBase().GetCastGUID());
                    var cleanDamage = new CleanDamage(splitDamage, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
                    DealDamage(caster, splitDamage, cleanDamage, DamageEffectType.Direct, damageInfo.GetSchoolMask(), itr.GetSpellInfo(), false);
                    log.damage = splitDamage;
                    log.originalDamage = splitDamage;
                    log.absorb = split_absorb;
                    SendSpellNonMeleeDamageLog(log);

                    // break 'Fear' and similar auras
                    ProcSkillsAndAuras(caster, ProcFlags.None, ProcFlags.TakenSpellMagicDmgClassNeg, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, damageInfo, null);
                }
            }
        }

        public void CalcHealAbsorb(HealInfo healInfo)
        {
            if (healInfo.GetHeal() == 0)
                return;

            // Need remove expired auras after
            var existExpired = false;

            // absorb without mana cost
            var vHealAbsorb = healInfo.GetTarget().GetAuraEffectsByType(AuraType.SchoolHealAbsorb);
            for (var i = 0; i < vHealAbsorb.Count; ++i)
            {
                var eff = vHealAbsorb[i];
                if (healInfo.GetHeal() <= 0)
                    break;

                if (!Convert.ToBoolean(eff.GetMiscValue() & (int)healInfo.GetSpellInfo().SchoolMask))
                    continue;

                // Max Amount can be absorbed by this aura
                var currentAbsorb = eff.GetAmount();

                // Found empty aura (impossible but..)
                if (currentAbsorb <= 0)
                {
                    existExpired = true;
                    continue;
                }

                // currentAbsorb - damage can be absorbed by shield
                // If need absorb less damage
                currentAbsorb = (int)Math.Min(healInfo.GetHeal(), currentAbsorb);

                healInfo.AbsorbHeal((uint)currentAbsorb);

                // Reduce shield amount
                eff.ChangeAmount(eff.GetAmount() - currentAbsorb);
                // Need remove it later
                if (eff.GetAmount() <= 0)
                    existExpired = true;
            }

            // Remove all expired absorb auras
            if (existExpired)
            {
                for (var i = 0; i < vHealAbsorb.Count;)
                {
                    var auraEff = vHealAbsorb[i];
                    ++i;
                    if (auraEff.GetAmount() <= 0)
                    {
                        var removedAuras = healInfo.GetTarget().m_removedAurasCount;
                        auraEff.GetBase().Remove(AuraRemoveMode.EnemySpell);
                        if (removedAuras + 1 < healInfo.GetTarget().m_removedAurasCount)
                            i = 0;
                    }
                }
            }
        }

        public uint CalcArmorReducedDamage(Unit attacker, Unit victim, uint damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.Max)
        {
            float armor = victim.GetArmor();

            armor *= victim.GetArmorMultiplierForTarget(attacker);

            // bypass enemy armor by SPELL_AURA_BYPASS_ARMOR_FOR_CASTER
            var armorBypassPct = 0;
            var reductionAuras = victim.GetAuraEffectsByType(AuraType.BypassArmorForCaster);
            foreach (var eff in reductionAuras)
                if (eff.GetCasterGUID() == GetGUID())
                    armorBypassPct += eff.GetAmount();
            armor = MathFunctions.CalculatePct(armor, 100 - Math.Min(armorBypassPct, 100));

            // Ignore enemy armor by SPELL_AURA_MOD_TARGET_RESISTANCE aura
            armor += GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)SpellSchoolMask.Normal);

            if (spellInfo != null)
            {
                var modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellInfo, SpellModOp.IgnoreArmor, ref armor);
            }

            var resIgnoreAuras = GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);
            foreach (var eff in resIgnoreAuras)
            {
                if (eff.GetMiscValue().HasAnyFlag((int)SpellSchoolMask.Normal) && eff.IsAffectingSpell(spellInfo))
                    armor = (float)Math.Floor(MathFunctions.AddPct(ref armor, -eff.GetAmount()));
            }

            // Apply Player CR_ARMOR_PENETRATION rating
            if (IsTypeId(TypeId.Player))
            {
                var arpPct = ToPlayer().GetRatingBonusValue(CombatRating.ArmorPenetration);

                // no more than 100%
                MathFunctions.RoundToInterval(ref arpPct, 0.0f, 100.0f);

                float maxArmorPen;
                if (victim.GetLevelForTarget(attacker) < 60)
                    maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker);
                else
                    maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker) + 4.5f * 85 * (victim.GetLevelForTarget(attacker) - 59);

                // Cap armor penetration to this number
                maxArmorPen = Math.Min((armor + maxArmorPen) / 3.0f, armor);
                // Figure out how much armor do we ignore
                armor -= MathFunctions.CalculatePct(maxArmorPen, arpPct);
            }

            if (MathFunctions.fuzzyLe(armor, 0.0f))
                return damage;

            var attackerLevel = attacker.GetLevelForTarget(victim);
            // Expansion and ContentTuningID necessary? Does Player get a ContentTuningID too ?
            var armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, attacker.GetClass());
            if ((armor + armorConstant) == 0)
                return damage;

            var mitigation = Math.Min(armor / (armor + armorConstant), 0.85f);
            return Math.Max((uint)(damage * (1.0f - mitigation)), 1);
        }

        public uint MeleeDamageBonusDone(Unit victim, uint pdamage, WeaponAttackType attType, SpellInfo spellProto = null)
        {
            if (victim == null || pdamage == 0)
                return 0;

            var creatureTypeMask = victim.GetCreatureTypeMask();

            // Done fixed damage bonus auras
            var DoneFlatBenefit = 0;

            // ..done
            DoneFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageDoneCreature, (int)creatureTypeMask);

            // ..done
            // SPELL_AURA_MOD_DAMAGE_DONE included in weapon damage

            // ..done (base at attack power for marked target and base at attack power for creature type)
            var APbonus = 0;

            if (attType == WeaponAttackType.RangedAttack)
            {
                APbonus += victim.GetTotalAuraModifier(AuraType.RangedAttackPowerAttackerBonus);

                // ..done (base at attack power and creature type)
                APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModRangedAttackPowerVersus, (int)creatureTypeMask);
            }
            else
            {
                APbonus += victim.GetTotalAuraModifier(AuraType.MeleeAttackPowerAttackerBonus);

                // ..done (base at attack power and creature type)
                APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModMeleeAttackPowerVersus, (int)creatureTypeMask);
            }

            if (APbonus != 0)                                       // Can be negative
            {
                var normalized = spellProto != null && spellProto.HasEffect(SpellEffectName.NormalizedWeaponDmg);
                DoneFlatBenefit += (int)(APbonus / 3.5f * GetAPMultiplier(attType, normalized));
            }

            // Done total percent damage auras
            var DoneTotalMod = 1.0f;


            if (spellProto != null)
            {
                // Some spells don't benefit from pct done mods
                // mods for SPELL_SCHOOL_MASK_NORMAL are already factored in base melee damage calculation
                if (!spellProto.HasAttribute(SpellAttr6.NoDonePctDamageMods) && !spellProto.GetSchoolMask().HasAnyFlag(SpellSchoolMask.Normal))
                {
                    var maxModDamagePercentSchool = 0.0f;
                    var thisPlayer = ToPlayer();
                    if (thisPlayer != null)
                    {
                        for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
                        {
                            if (Convert.ToBoolean((int)spellProto.GetSchoolMask() & (1 << (int)i)))
                                maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, thisPlayer.m_activePlayerData.ModDamageDonePercent[(int)i]);
                        }
                    }
                    else
                        maxModDamagePercentSchool = GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, (uint)spellProto.GetSchoolMask());

                    DoneTotalMod *= maxModDamagePercentSchool;
                }
                else
                {
                    // melee attack
                    foreach (var autoAttackDamage in GetAuraEffectsByType(AuraType.ModAutoAttackDamage))
                        MathFunctions.AddPct(ref DoneTotalMod, autoAttackDamage.GetAmount());
                }
            }

            DoneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, creatureTypeMask);

            // bonus against aurastate
            DoneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate, aurEff =>
            {
                if (victim.HasAuraState((AuraStateType)aurEff.GetMiscValue()))
                    return true;
                return false;
            });

            // Add SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC percent bonus
            if (spellProto != null)
                MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellProto.Mechanic));

            var tmpDamage = (pdamage + DoneFlatBenefit) * DoneTotalMod;

            // apply spellmod to Done damage
            if (spellProto != null)
            {
                var modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellProto, SpellModOp.Damage, ref tmpDamage);
            }

            // bonus result can be negative
            return (uint)Math.Max(tmpDamage, 0.0f);
        }
        public uint MeleeDamageBonusTaken(Unit attacker, uint pdamage, WeaponAttackType attType, DamageEffectType damagetype, SpellInfo spellProto = null)
        {
            if (pdamage == 0)
                return 0;

            var TakenFlatBenefit = 0;
            var TakenTotalCasterMod = 0.0f;

            // get all auras from caster that allow the spell to ignore resistance (sanctified wrath)
            var attackSchoolMask = (int)(spellProto != null ? spellProto.GetSchoolMask() : SpellSchoolMask.Normal);
            TakenTotalCasterMod += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, attackSchoolMask);

            // ..taken
            TakenFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)attacker.GetMeleeDamageSchoolMask());

            if (attType != WeaponAttackType.RangedAttack)
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModMeleeDamageTaken);
            else
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModRangedDamageTaken);

            // Taken total percent damage auras
            var TakenTotalMod = 1.0f;

            // ..taken
            TakenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)attacker.GetMeleeDamageSchoolMask());

            // .. taken pct (special attacks)
            if (spellProto != null)
            {
                // From caster spells
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSchoolMaskDamageFromCaster, aurEff =>
                {
                    return aurEff.GetCasterGUID() == attacker.GetGUID() && (aurEff.GetMiscValue() & (int)spellProto.GetSchoolMask()) != 0;
                });

                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff =>
                {
                    return aurEff.GetCasterGUID() == attacker.GetGUID() && aurEff.IsAffectingSpell(spellProto);
                });

                // Mod damage from spell mechanic
                var mechanicMask = spellProto.GetAllEffectsMechanicMask();

                // Shred, Maul - "Effects which increase Bleed damage also increase Shred damage"
                if (spellProto.SpellFamilyName == SpellFamilyNames.Druid && spellProto.SpellFamilyFlags[0].HasAnyFlag(0x00008800u))
                    mechanicMask |= (1 << (int)Mechanics.Bleed);

                if (mechanicMask != 0)
                {
                    TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMechanicDamageTakenPercent, aurEff =>
                    {
                        if ((mechanicMask & (1 << (aurEff.GetMiscValue()))) != 0)
                            return true;
                        return false;
                    });
                }

                if (damagetype == DamageEffectType.DOT)
                    TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModPeriodicDamageTaken, aurEff => (aurEff.GetMiscValue() & (uint)spellProto.GetSchoolMask()) != 0);
            }

            var cheatDeath = GetAuraEffect(45182, 0);
            if (cheatDeath != null)
                MathFunctions.AddPct(ref TakenTotalMod, cheatDeath.GetAmount());

            if (attType != WeaponAttackType.RangedAttack)
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageTakenPct);
            else
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModRangedDamageTakenPct);

            // Versatility
            var modOwner = GetSpellModOwner();
            if (modOwner)
            {
                // only 50% of SPELL_AURA_MOD_VERSATILITY for damage reduction
                var versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
                MathFunctions.AddPct(ref TakenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
            }

            var tmpDamage = 0.0f;

            if (TakenTotalCasterMod != 0)
            {
                if (TakenFlatBenefit < 0)
                {
                    if (TakenTotalMod < 1)
                        tmpDamage = (((MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod) + TakenFlatBenefit) * TakenTotalMod) + MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod));
                    else
                        tmpDamage = (((MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod) + TakenFlatBenefit) + MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod)) * TakenTotalMod);
                }
                else if (TakenTotalMod < 1)
                    tmpDamage = ((MathFunctions.CalculatePct(pdamage + TakenFlatBenefit, TakenTotalCasterMod) * TakenTotalMod) + MathFunctions.CalculatePct(pdamage + TakenFlatBenefit, TakenTotalCasterMod));
            }
            if (tmpDamage == 0)
                tmpDamage = (pdamage + TakenFlatBenefit) * TakenTotalMod;

            // bonus result can be negative
            return (uint)Math.Max(tmpDamage, 0.0f);
        }

        bool IsBlockCritical()
        {
            if (RandomHelper.randChance(GetTotalAuraModifier(AuraType.ModBlockCritChance)))
                return true;
            return false;
        }
        public virtual SpellSchoolMask GetMeleeDamageSchoolMask() { return SpellSchoolMask.None; }

        // Redirect Threat
        public void SetRedirectThreat(ObjectGuid guid, uint pct) { _redirectThreatInfo.Set(guid, pct); }
        public void ResetRedirectThreat() { SetRedirectThreat(ObjectGuid.Empty, 0); }
        void ModifyRedirectThreat(int amount) { _redirectThreatInfo.ModifyThreatPct(amount); }
        public uint GetRedirectThreatPercent() { return _redirectThreatInfo.GetThreatPct(); }
        public Unit GetRedirectThreatTarget()
        {
            return Global.ObjAccessor.GetUnit(this, _redirectThreatInfo.GetTargetGUID());
        }

        float CalculateDefaultCoefficient(SpellInfo spellInfo, DamageEffectType damagetype)
        {
            // Damage over Time spells bonus calculation
            var DotFactor = 1.0f;
            if (damagetype == DamageEffectType.DOT)
            {

                var DotDuration = spellInfo.GetDuration();
                if (!spellInfo.IsChanneled() && DotDuration > 0)
                    DotFactor = DotDuration / 15000.0f;

                var DotTicks = spellInfo.GetMaxTicks();
                if (DotTicks != 0)
                    DotFactor /= DotTicks;
            }

            var CastingTime = (uint)(spellInfo.IsChanneled() ? spellInfo.GetDuration() : spellInfo.CalcCastTime());
            // Distribute Damage over multiple effects, reduce by AoE
            CastingTime = GetCastingTimeForBonus(spellInfo, damagetype, CastingTime);

            // As wowwiki says: C = (Cast Time / 3.5)
            return (CastingTime / 3500.0f) * DotFactor;
        }

        void ApplyPercentModFloatVar(ref float var, float val, bool apply)
        {
            var *= (apply ? (100.0f + val) / 100.0f : 100.0f / (100.0f + val));
        }

        public void ApplyAttackTimePercentMod(WeaponAttackType att, float val, bool apply)
        {
            var remainingTimePct = m_attackTimer[(int)att] / (m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att]);
            if (val > 0.0f)
            {
                MathFunctions.ApplyPercentModFloatVar(ref m_modAttackSpeedPct[(int)att], val, !apply);

                if (att == WeaponAttackType.BaseAttack)
                    ApplyPercentModUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ModHaste), val, !apply);
                else if (att == WeaponAttackType.RangedAttack)
                    ApplyPercentModUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ModRangedHaste), val, !apply);
            }
            else
            {
                MathFunctions.ApplyPercentModFloatVar(ref m_modAttackSpeedPct[(int)att], -val, apply);

                if (att == WeaponAttackType.BaseAttack)
                    ApplyPercentModUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ModHaste), -val, apply);
                else if (att == WeaponAttackType.RangedAttack)
                    ApplyPercentModUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.ModRangedHaste), -val, apply);
            }

            UpdateAttackTimeField(att);
            m_attackTimer[(int)att] = (uint)(m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att] * remainingTimePct);
        }

        // function based on function Unit.CanAttack from 13850 client
        public bool _IsValidAttackTarget(Unit target, SpellInfo bySpell, WorldObject obj = null)
        {
            Cypher.Assert(target != null);

            // can't attack self
            if (this == target)
                return false;

            // can't attack unattackable units or GMs
            if (target.HasUnitState(UnitState.Unattackable)
                || (target.IsTypeId(TypeId.Player) && target.ToPlayer().IsGameMaster()))
                return false;

            // visibility checks
            // skip visibility check for GO casts, needs removal when go cast is implemented. Also ignore for gameobject and dynauras
            if (GetEntry() != SharedConst.WorldTrigger && (!obj || !obj.IsTypeMask(TypeMask.GameObject | TypeMask.DynamicObject)))
            {
                // can't attack invisible
                if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible))
                {
                    if (obj && !obj.CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea()))
                        return false;
                    else if (!obj)
                    {
                        // ignore stealth for aoe spells. Ignore stealth if target is player and unit in combat with same player
                        var ignoreStealthCheck = (bySpell != null && bySpell.IsAffectingArea()) ||
                                                 (target.GetTypeId() == TypeId.Player && target.HasStealthAura() && target.IsInCombat() && IsInCombatWith(target));

                        if (!CanSeeOrDetect(target, ignoreStealthCheck))
                            return false;
                    }
                }
            }

            // can't attack dead
            if ((bySpell == null || !bySpell.IsAllowingDeadTarget()) && !target.IsAlive())
                return false;

            // can't attack untargetable
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable))
                && target.HasUnitFlag(UnitFlags.NotSelectable))
                return false;

            var playerAttacker = ToPlayer();
            if (playerAttacker != null)
            {
                if (playerAttacker.HasPlayerFlag(PlayerFlags.Commentator2))
                    return false;
            }

            // check flags
            if (target.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.TaxiFlight | UnitFlags.NotAttackable1 | UnitFlags.Unk16)
                || (!HasUnitFlag(UnitFlags.PvpAttackable) && target.HasUnitFlag(UnitFlags.ImmuneToNpc))
                || (!target.HasUnitFlag(UnitFlags.PvpAttackable) && HasUnitFlag(UnitFlags.ImmuneToNpc)))
                return false;

            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
                && (HasUnitFlag(UnitFlags.PvpAttackable) && target.HasUnitFlag(UnitFlags.ImmuneToPc))
                // check if this is a world trigger cast - GOs are using world triggers to cast their spells, so we need to ignore their immunity flag here, this is a temp workaround, needs removal when go cast is implemented properly
                && GetEntry() != SharedConst.WorldTrigger)
                return false;

            // CvC case - can attack each other only when one of them is hostile
            if (!HasUnitFlag(UnitFlags.PvpAttackable) && !target.HasUnitFlag(UnitFlags.PvpAttackable))
                return GetReactionTo(target) <= ReputationRank.Hostile || target.GetReactionTo(this) <= ReputationRank.Hostile;

            // PvP, PvC, CvP case
            // can't attack friendly targets
            if (GetReactionTo(target) > ReputationRank.Neutral || target.GetReactionTo(this) > ReputationRank.Neutral)
                return false;

            var playerAffectingAttacker = HasUnitFlag(UnitFlags.PvpAttackable) ? GetAffectingPlayer() : null;
            var playerAffectingTarget = target.HasUnitFlag(UnitFlags.PvpAttackable) ? target.GetAffectingPlayer() : null;

            // Not all neutral creatures can be attacked (even some unfriendly faction does not react aggresive to you, like Sporaggar)
            if ((playerAffectingAttacker && !playerAffectingTarget) || (!playerAffectingAttacker && playerAffectingTarget))
            {
                if (!(target.IsTypeId(TypeId.Player) && IsTypeId(TypeId.Player)) &&
                    !(target.IsTypeId(TypeId.Unit) && IsTypeId(TypeId.Unit)))
                {
                    var player = playerAffectingAttacker ? playerAffectingAttacker : playerAffectingTarget;
                    var creature = playerAffectingAttacker ? target : this;

                    var factionTemplate = creature.GetFactionTemplateEntry();
                    if (factionTemplate != null)
                    {
                        if (player.GetReputationMgr().GetForcedRankIfAny(factionTemplate) == ReputationRank.None)
                        {
                            var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplate.Faction);
                            if (factionEntry != null)
                            {
                                var repState = player.GetReputationMgr().GetState(factionEntry);
                                if (repState != null)
                                    if (!Convert.ToBoolean(repState.Flags & FactionFlags.AtWar))
                                        return false;
                            }
                        }

                    }
                }
            }

            var creatureAttacker = ToCreature();
            if (creatureAttacker != null && Convert.ToBoolean(creatureAttacker.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.TreatAsRaidUnit))
                return false;

            // check duel - before sanctuary checks
            if (playerAffectingAttacker != null && playerAffectingTarget != null)
                if (playerAffectingAttacker.duel != null && playerAffectingAttacker.duel.opponent == playerAffectingTarget && playerAffectingAttacker.duel.startTime != 0)
                    return true;

            // PvP case - can't attack when attacker or target are in sanctuary
            // however, 13850 client doesn't allow to attack when one of the unit's has sanctuary flag and is pvp
            if (target.HasUnitFlag(UnitFlags.PvpAttackable) && HasUnitFlag(UnitFlags.PvpAttackable) && (target.IsInSanctuary() || IsInSanctuary()))
                return false;

            // additional checks - only PvP case
            if (playerAffectingAttacker != null && playerAffectingTarget != null)
            {
                if (target.IsPvP())
                    return true;

                if (IsFFAPvP() && target.IsFFAPvP())
                    return true;

                return HasPvpFlag(UnitPVPStateFlags.Unk1) || target.HasPvpFlag(UnitPVPStateFlags.Unk1);
            }
            return true;
        }

        public bool IsValidAssistTarget(Unit target)
        {
            return _IsValidAssistTarget(target, null);
        }
        // function based on function Unit.CanAssist from 13850 client
        public bool _IsValidAssistTarget(Unit target, SpellInfo bySpell)
        {
            Cypher.Assert(target != null);

            // can assist to self
            if (this == target)
                return true;

            // can't assist unattackable units or GMs
            if (target.HasUnitState(UnitState.Unattackable)
                || (target.IsTypeId(TypeId.Player) && target.ToPlayer().IsGameMaster()))
                return false;

            // can't assist own vehicle or passenger
            if (m_vehicle != null)
                if (IsOnVehicle(target) || m_vehicle.GetBase().IsOnVehicle(target))
                    return false;

            // can't assist invisible
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible)) && !CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea()))
                return false;

            // can't assist dead
            if ((bySpell == null || !bySpell.IsAllowingDeadTarget()) && !target.IsAlive())
                return false;

            // can't assist untargetable
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable))
                && target.HasUnitFlag(UnitFlags.NotSelectable))
                return false;

            if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
            {
                if (HasUnitFlag(UnitFlags.PvpAttackable))
                {
                    if (target.HasUnitFlag(UnitFlags.ImmuneToPc))
                        return false;
                }
                else
                {
                    if (target.HasUnitFlag(UnitFlags.ImmuneToNpc))
                        return false;
                }
            }

            // can't assist non-friendly targets
            if (GetReactionTo(target) <= ReputationRank.Neutral
                && target.GetReactionTo(this) <= ReputationRank.Neutral
                && (!IsTypeId(TypeId.Unit) || !Convert.ToBoolean(ToCreature().GetCreatureTemplate().TypeFlags & CreatureTypeFlags.TreatAsRaidUnit)))
                return false;

            // Controlled player case, we can assist creatures (reaction already checked above, our faction == charmer faction)
            if (IsTypeId(TypeId.Player) && IsCharmed() && GetCharmerGUID().IsCreature())
                return true;

            // PvP case
            else if (target.HasUnitFlag(UnitFlags.PvpAttackable))
            {
                var targetPlayerOwner = target.GetAffectingPlayer();
                if (HasUnitFlag(UnitFlags.PvpAttackable))
                {
                    var selfPlayerOwner = GetAffectingPlayer();
                    if (selfPlayerOwner != null && targetPlayerOwner != null)
                    {
                        // can't assist player which is dueling someone
                        if (selfPlayerOwner != targetPlayerOwner && targetPlayerOwner.duel != null)
                            return false;
                    }
                    // can't assist player in ffa_pvp zone from outside
                    if (target.HasPvpFlag(UnitPVPStateFlags.FFAPvp) && !HasPvpFlag(UnitPVPStateFlags.FFAPvp))
                        return false;
                    // can't assist player out of sanctuary from sanctuary if has pvp enabled
                    if (target.HasPvpFlag(UnitPVPStateFlags.PvP))
                        if (IsInSanctuary() && !target.IsInSanctuary())
                            return false;
                }
            }
            // PvC case - player can assist creature only if has specific type flags
            else if (HasUnitFlag(UnitFlags.PvpAttackable)
                && (bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
                && !target.HasPvpFlag(UnitPVPStateFlags.PvP))
            {
                var creatureTarget = target.ToCreature();
                if (creatureTarget != null)
                    return Convert.ToBoolean(creatureTarget.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.TreatAsRaidUnit)
                        || Convert.ToBoolean(creatureTarget.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.CanAssist);
            }
            return true;
        }

        public virtual bool CheckAttackFitToAuraRequirement(WeaponAttackType attackType, AuraEffect aurEff) { return true; }

        public virtual void UpdateDamageDoneMods(WeaponAttackType attackType)
        {
            var unitMod = attackType switch
            {
                WeaponAttackType.BaseAttack => UnitMods.DamageMainHand,
                WeaponAttackType.OffAttack => UnitMods.DamageOffHand,
                WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
                _ => throw new NotImplementedException(),
            };

            float amount = GetTotalAuraModifier(AuraType.ModDamageDone, aurEff =>
            {
                if ((aurEff.GetMiscValue() & (int)SpellSchoolMask.Normal) == 0)
                    return false;

                return CheckAttackFitToAuraRequirement(attackType, aurEff);
            });

            SetStatFlatModifier(unitMod, UnitModifierFlatType.Total, amount);
        }

        public void UpdateAllDamageDoneMods()
        {
            for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
                UpdateDamageDoneMods(attackType);
        }

        public void UpdateDamagePctDoneMods(WeaponAttackType attackType)
        {
            (UnitMods unitMod, float factor) = attackType switch
            {
                WeaponAttackType.BaseAttack => (UnitMods.DamageMainHand, 1.0f),
                WeaponAttackType.OffAttack => (UnitMods.DamageOffHand, 0.5f),
                WeaponAttackType.RangedAttack => (UnitMods.DamageRanged, 1.0f),
                _ => throw new NotImplementedException(),
            };

            factor *= GetTotalAuraMultiplier(AuraType.ModDamagePercentDone, aurEff =>
            {
                if (!aurEff.GetMiscValue().HasAnyFlag((int)SpellSchoolMask.Normal))
                    return false;

                return CheckAttackFitToAuraRequirement(attackType, aurEff);
            });

            if (attackType == WeaponAttackType.OffAttack)
                factor *= GetTotalAuraMultiplier(AuraType.ModOffhandDamagePct, auraEffect => CheckAttackFitToAuraRequirement(attackType, auraEffect));

            SetStatPctModifier(unitMod, UnitModifierPctType.Total, factor);
        }

        public void UpdateAllDamagePctDoneMods()
        {
            for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
                UpdateDamagePctDoneMods(attackType);
        }

    }
}
