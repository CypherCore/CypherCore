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
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Combat;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Network.Packets;
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
            ObjectGuid guid = who.GetGUID();
            foreach (var refe in GetThreatManager().getThreatList())
            {
                // Return true if the unit matches
                if (refe != null && refe.getUnitGuid() == guid)
                    return true;
            }

            // Nothing found, false.
            return false;
        }

        public ThreatManager GetThreatManager() { return threatManager; }

        public bool CanDualWield() { return m_canDualWield; }

        public void SendChangeCurrentVictim(HostileReference pHostileReference)
        {
            if (!GetThreatManager().isThreatListEmpty())
            {
                HighestThreatUpdate packet = new HighestThreatUpdate();
                packet.UnitGUID = GetGUID();
                packet.HighestThreatGUID = pHostileReference.getUnitGuid();

                var refeList = GetThreatManager().getThreatList();
                foreach (var refe in refeList)
                {
                    ThreatInfo info = new ThreatInfo();
                    info.UnitGUID = refe.getUnitGuid();
                    info.Threat = (long)refe.getThreat() * 100;
                    packet.ThreatList.Add(info);
                }
                SendMessageToSet(packet, false);
            }
        }

        public void StopAttackFaction(uint factionId)
        {
            Unit victim = GetVictim();
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

            var attackers = getAttackers();
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

            getHostileRefManager().deleteReferencesForFaction(factionId);

            foreach (var control in m_Controlled)
                control.StopAttackFaction(factionId);
        }

        public void HandleProcExtraAttackFor(Unit victim)
        {
            while (m_extraAttacks != 0)
            {
                AttackerStateUpdate(victim, WeaponAttackType.BaseAttack, true);
                --m_extraAttacks;
            }
        }

        public virtual void SetCanDualWield(bool value) { m_canDualWield = value; }

        void SendClearThreatList()
        {
            ThreatClear packet = new ThreatClear();
            packet.UnitGUID = GetGUID();
            SendMessageToSet(packet, false);
        }

        public void SendRemoveFromThreatList(HostileReference pHostileReference)
        {
            ThreatRemove packet = new ThreatRemove();
            packet.UnitGUID = GetGUID();
            packet.AboutGUID = pHostileReference.getUnitGuid();
            SendMessageToSet(packet, false);
        }

        void SendThreatListUpdate()
        {
            if (!GetThreatManager().isThreatListEmpty())
            {
                ThreatUpdate packet = new ThreatUpdate();
                packet.UnitGUID = GetGUID();
                var tlist = GetThreatManager().getThreatList();
                foreach (var refe in tlist)
                {
                    ThreatInfo info = new ThreatInfo();
                    info.UnitGUID = refe.getUnitGuid();
                    info.Threat = (long)refe.getThreat() * 100;
                    packet.ThreatList.Add(info);
                }
                SendMessageToSet(packet, false);
            }
        }

        public void DeleteThreatList()
        {
            if (CanHaveThreatList(true) && !threatManager.isThreatListEmpty())
                SendClearThreatList();
            threatManager.clearReferences();
        }

        public void TauntApply(Unit taunter)
        {
            Cypher.Assert(IsTypeId(TypeId.Unit));

            if (!taunter || (taunter.IsTypeId(TypeId.Player) && taunter.ToPlayer().IsGameMaster()))
                return;

            if (!CanHaveThreatList())
                return;

            Creature creature = ToCreature();

            if (creature.HasReactState(ReactStates.Passive))
                return;

            Unit target = GetVictim();
            if (target && target == taunter)
                return;

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

            Creature creature = ToCreature();

            if (creature.HasReactState(ReactStates.Passive))
                return;

            Unit target = GetVictim();
            if (!target || target != taunter)
                return;

            if (threatManager.isThreatListEmpty())
            {
                if (creature.IsAIEnabled)
                    creature.GetAI().EnterEvadeMode(EvadeReason.NoHostiles);
                return;
            }

            target = creature.SelectVictim();  // might have more taunt auras remaining

            if (target && target != taunter)
            {
                SetInFront(target);
                if (creature.IsAIEnabled)
                    creature.GetAI().AttackStart(target);
            }
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
        }
        public void CombatStopWithPets(bool includingCast = false)
        {
            CombatStop(includingCast);

            foreach (var control in m_Controlled)
                control.CombatStop(includingCast);
        }
        public void ClearInCombat()
        {
            m_CombatTimer = 0;
            RemoveFlag(UnitFields.Flags, UnitFlags.InCombat);

            // Player's state will be cleared in Player.UpdateContestedPvP
            Creature creature = ToCreature();
            if (creature != null)
            {
                ClearUnitState(UnitState.AttackPlayer);
                if (HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.Tapped))
                    SetUInt32Value(ObjectFields.DynamicFlags, creature.GetCreatureTemplate().DynamicFlags);

                if (creature.IsPet() || creature.IsGuardian())
                {
                    Unit owner = GetOwner();
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

            RemoveFlag(UnitFields.Flags, UnitFlags.PetInCombat);
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeaveCombat);
        }

        void RemoveAllAttackers()
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

        public void addHatedBy(HostileReference pHostileReference)
        {
            m_HostileRefManager.InsertFirst(pHostileReference);
        }
        public void removeHatedBy(HostileReference pHostileReference) { } //nothing to do yet

        public void AddThreat(Unit victim, float fThreat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal, SpellInfo threatSpell = null)
        {
            // Only mobs can manage threat lists
            if (CanHaveThreatList() && !HasUnitState(UnitState.Evade))
                threatManager.addThreat(victim, fThreat, schoolMask, threatSpell);
        }
        public float ApplyTotalThreatModifier(float fThreat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal)
        {
            if (!HasAuraType(AuraType.ModThreat) || fThreat < 0)
                return fThreat;

            SpellSchools school = Global.SpellMgr.GetFirstSchoolInMask(schoolMask);

            return fThreat * m_threatModifier[(int)school];
        }

        public bool isTargetableForAttack(bool checkFakeDeath = true)
        {
            if (!IsAlive())
                return false;

            if (HasFlag(UnitFields.Flags, (UnitFlags.NonAttackable | UnitFlags.NotSelectable)))
                return false;

            if (IsTypeId(TypeId.Player) && ToPlayer().IsGameMaster())
                return false;

            return !HasUnitState(UnitState.Unattackable) && (!checkFakeDeath || !HasUnitState(UnitState.Died));
        }

        public DeathState getDeathState()
        {
            return m_deathState;
        }
        public bool IsInCombat()
        {
            return HasFlag(UnitFields.Flags, UnitFlags.InCombat);
        }
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

            if (HasFlag(UnitFields.Flags, UnitFlags.Pacified))
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

            if (m_attacking != null)
            {
                if (m_attacking == victim)
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

            if (m_attacking != null)
                m_attacking._removeAttacker(this);

            m_attacking = victim;
            m_attacking._addAttacker(this);

            // Set our target
            SetTarget(victim.GetGUID());

            if (meleeAttack)
                AddUnitState(UnitState.MeleeAttacking);

            if (IsTypeId(TypeId.Unit) && !IsPet())
            {
                // should not let player enter combat by right clicking target - doesn't helps
                AddThreat(victim, 0.0f);
                SetInCombatWith(victim);

                if (victim.IsTypeId(TypeId.Player))
                    victim.SetInCombatWith(this);

                ToCreature().SendAIReaction(AiReaction.Hostile);
                ToCreature().CallAssistance();

                // Remove emote state - will be restored on creature reset
                SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);
            }

            // delay offhand weapon attack to next attack time
            if (haveOffhandWeapon() && GetTypeId() != TypeId.Player)
                resetAttackTimer(WeaponAttackType.OffAttack);

            if (meleeAttack)
                SendMeleeAttackStart(victim);

            // Let the pet know we've started attacking someting. Handles melee attacks only
            // Spells such as auto-shot and others handled in WorldSession.HandleCastSpellOpcode
            if (IsTypeId(TypeId.Player))
            {
                Pet playerPet = ToPlayer().GetPet();

                if (playerPet != null && playerPet.IsAlive())
                    playerPet.GetAI().OwnerAttacked(victim);
            }
            return true;
        }
        public void SendMeleeAttackStart(Unit victim)
        {
            AttackStart packet = new AttackStart();
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
        public ObjectGuid GetTarget() { return GetGuidValue(UnitFields.Target); }
        public virtual void SetTarget(ObjectGuid guid) { }
        public bool AttackStop()
        {
            if (m_attacking == null)
                return false;

            Unit victim = m_attacking;

            m_attacking._removeAttacker(this);
            m_attacking = null;

            // Clear our target
            SetTarget(ObjectGuid.Empty);

            ClearUnitState(UnitState.MeleeAttacking);

            InterruptSpell(CurrentSpellTypes.Melee);

            // reset only at real combat stop
            Creature creature = ToCreature();
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
            return m_attacking;
        }
        public Unit getAttackerForHelper()
        {
            if (GetVictim() != null)
                return GetVictim();

            if (attackerList.Count != 0)
                return attackerList[0];

            return null;
        }
        public List<Unit> getAttackers()
        {
            return attackerList;
        }

        public float GetCombatReach() { return GetFloatValue(UnitFields.CombatReach); }
        float GetBoundaryRadius() { return GetFloatValue(UnitFields.BoundingRadius); }

        public bool haveOffhandWeapon()
        {
            if (IsTypeId(TypeId.Player))
                return ToPlayer().GetWeaponForAttack(WeaponAttackType.OffAttack, true) != null;
            else
                return m_canDualWield;
        }
        public void resetAttackTimer(WeaponAttackType type = WeaponAttackType.BaseAttack)
        {
            m_attackTimer[(int)type] = (uint)(GetBaseAttackTime(type) * m_modAttackSpeedPct[(int)type]);
        }
        public void setAttackTimer(WeaponAttackType type, uint time)
        {
            m_attackTimer[(int)type] = time;
        }
        public uint getAttackTimer(WeaponAttackType type)
        {
            return m_attackTimer[(int)type];
        }
        public bool isAttackReady(WeaponAttackType type = WeaponAttackType.BaseAttack)
        {
            return m_attackTimer[(int)type] == 0;
        }
        public uint GetBaseAttackTime(WeaponAttackType att)
        {
            return m_baseAttackSpeed[(int)att];
        }
        public void AttackerStateUpdate(Unit victim, WeaponAttackType attType = WeaponAttackType.BaseAttack, bool extra = false)
        {
            if (HasUnitState(UnitState.CannotAutoattack) || HasFlag(UnitFields.Flags, UnitFlags.Pacified))
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

            if (IsTypeId(TypeId.Unit) && !HasFlag(UnitFields.Flags, UnitFlags.PlayerControlled))
                SetFacingToObject(victim); // update client side facing to face the target (prevents visual glitches when casting untargeted spells)

            // melee attack spell casted at main hand attack only - no normal melee dmg dealt
            if (attType == WeaponAttackType.BaseAttack && GetCurrentSpell(CurrentSpellTypes.Melee) != null && !extra)
                m_currentSpells[CurrentSpellTypes.Melee].cast();
            else
            {
                // attack can be redirected to another target
                victim = GetMeleeHitRedirectTarget(victim);

                CalcDamageInfo damageInfo;
                CalculateMeleeDamage(victim, 0, out damageInfo, attType);
                // Send log damage message to client
                DealDamageMods(victim, ref damageInfo.damage, ref damageInfo.absorb);
                SendAttackStateUpdate(damageInfo);

                DealMeleeDamage(damageInfo, true);

                DamageInfo dmgInfo = new DamageInfo(damageInfo);
                ProcSkillsAndAuras(damageInfo.target, damageInfo.procAttacker, damageInfo.procVictim, ProcFlagsSpellType.None, ProcFlagsSpellPhase.None, dmgInfo.GetHitMask(), null, dmgInfo, null);

                if (IsTypeId(TypeId.Player))
                    Log.outDebug(LogFilter.Unit, "AttackerStateUpdate: (Player) {0} attacked {1} (TypeId: {2}) for {3} dmg, absorbed {4}, blocked {5}, resisted {6}.",
                        GetGUID().ToString(), victim.GetGUID().ToString(), victim.GetTypeId(), damageInfo.damage, damageInfo.absorb, damageInfo.blocked_amount, damageInfo.resist);
                else
                    Log.outDebug(LogFilter.Unit, "AttackerStateUpdate: (NPC) {0} attacked {1} (TypeId: {2}) for {3} dmg, absorbed {4}, blocked {5}, resisted {6}.",
                        GetGUID().ToString(), victim.GetGUID().ToString(), victim.GetTypeId(), damageInfo.damage, damageInfo.absorb, damageInfo.blocked_amount, damageInfo.resist);
            }
        }

        public void FakeAttackerStateUpdate(Unit victim, WeaponAttackType attType = WeaponAttackType.BaseAttack)
        {
            if (HasUnitState(UnitState.CannotAutoattack) || HasFlag(UnitFields.Flags, UnitFlags.Pacified))
                return;

            if (!victim.IsAlive())
                return;

            if ((attType == WeaponAttackType.BaseAttack || attType == WeaponAttackType.OffAttack) && !IsWithinLOSInMap(victim))
                return;

            CombatStart(victim);
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.MeleeAttack);

            if (attType != WeaponAttackType.BaseAttack && attType != WeaponAttackType.OffAttack)
                return;                                             // ignore ranged case

            if (IsTypeId(TypeId.Unit) && !HasFlag(UnitFields.Flags, UnitFlags.PlayerControlled))
                SetFacingToObject(victim); // update client side facing to face the target (prevents visual glitches when casting untargeted spells)

            CalcDamageInfo damageInfo = new CalcDamageInfo();
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
                Unit magnet = eff.GetBase().GetCaster();
                if (magnet != null)
                {
                    if (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && _IsValidAttackTarget(magnet, spellInfo))
                    {
                        // @todo handle this charge drop by proc in cast phase on explicit target
                        if (spellInfo.Speed > 0.0f)
                        {
                            // Set up missile speed based delay
                            uint delay = (uint)Math.Floor(Math.Max(victim.GetDistance(this), 5.0f) / spellInfo.Speed * 1000.0f);
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
                Unit magnet = i.GetCaster();
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

            damage *= (uint)GetDamageMultiplierForTarget(victim);
        }
        void DealMeleeDamage(CalcDamageInfo damageInfo, bool durabilityLoss)
        {
            Unit victim = damageInfo.target;

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
                float offtime = victim.getAttackTimer(WeaponAttackType.OffAttack);
                float basetime = victim.getAttackTimer(WeaponAttackType.BaseAttack);
                // Reduce attack time
                if (victim.haveOffhandWeapon() && offtime < basetime)
                {
                    float percent20 = victim.GetBaseAttackTime(WeaponAttackType.OffAttack) * 0.20f;
                    float percent60 = 3.0f * percent20;
                    if (offtime > percent20 && offtime <= percent60)
                        victim.setAttackTimer(WeaponAttackType.OffAttack, (uint)percent20);
                    else if (offtime > percent60)
                    {
                        offtime -= 2.0f * percent20;
                        victim.setAttackTimer(WeaponAttackType.OffAttack, (uint)offtime);
                    }
                }
                else
                {
                    float percent20 = victim.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 0.20f;
                    float percent60 = 3.0f * percent20;
                    if (basetime > percent20 && basetime <= percent60)
                        victim.setAttackTimer(WeaponAttackType.BaseAttack, (uint)percent20);
                    else if (basetime > percent60)
                    {
                        basetime -= 2.0f * percent20;
                        victim.setAttackTimer(WeaponAttackType.BaseAttack, (uint)basetime);
                    }
                }
            }

            // Call default DealDamage
            CleanDamage cleanDamage = new CleanDamage(damageInfo.cleanDamage, damageInfo.absorb, damageInfo.attackType, damageInfo.hitOutCome);
            DealDamage(victim, damageInfo.damage, cleanDamage, DamageEffectType.Direct, (SpellSchoolMask)damageInfo.damageSchoolMask, null, durabilityLoss);

            // If this is a creature and it attacks from behind it has a probability to daze it's victim
            if ((damageInfo.hitOutCome == MeleeHitOutcome.Crit || damageInfo.hitOutCome == MeleeHitOutcome.Crushing || damageInfo.hitOutCome == MeleeHitOutcome.Normal || damageInfo.hitOutCome == MeleeHitOutcome.Glancing) &&
                !IsTypeId(TypeId.Player) && !ToCreature().IsControlledByPlayer() && !victim.HasInArc(MathFunctions.PI, this)
                && (victim.IsTypeId(TypeId.Player) || !victim.ToCreature().isWorldBoss()) && !victim.IsVehicle())
            {
                // -probability is between 0% and 40%
                // 20% base chance
                float Probability = 20.0f;

                // there is a newbie protection, at level 10 just 7% base chance; assuming linear function
                if (victim.getLevel() < 30)
                    Probability = 0.65f * victim.GetLevelForTarget(this) + 0.5f;

                uint VictimDefense = victim.GetMaxSkillValueForLevel(this);
                uint AttackerMeleeSkill = GetMaxSkillValueForLevel();

                Probability *= (AttackerMeleeSkill / (float)VictimDefense * 0.16f);

                if (Probability < 0)
                    Probability = 0;

                if (Probability > 40.0f)
                    Probability = 40.0f;

                if (RandomHelper.randChance(Probability))
                    CastSpell(victim, 1604, true);
            }

            if (IsTypeId(TypeId.Player))
            {
                DamageInfo dmgInfo = new DamageInfo(damageInfo);
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
                    SpellInfo spellInfo = dmgShield.GetSpellInfo();

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

                    uint damage = (uint)dmgShield.GetAmount();
                    Unit caster = dmgShield.GetCaster();
                    if (caster)
                    {
                        damage = caster.SpellDamageBonusDone(this, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
                        damage = SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
                    }

                    DamageInfo damageInfo1 = new DamageInfo(this, victim, damage, spellInfo, spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                    victim.CalcAbsorbResist(damageInfo1);
                    damage = damageInfo1.GetDamage();

                    victim.DealDamageMods(this, ref damage);

                    SpellDamageShield damageShield = new SpellDamageShield();
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
                    Pet pet = victim.ToPlayer().GetPet();

                    if (pet != null && pet.IsAlive())
                        pet.GetAI().OwnerAttackedBy(this);
                }

                if (victim.ToPlayer().GetCommandStatus(PlayerCommandStates.God))
                    return 0;
            }

            // Signal the pet it was attacked so the AI can respond if needed
            if (victim.IsTypeId(TypeId.Unit) && this != victim && victim.IsPet() && victim.IsAlive())
                victim.ToPet().GetAI().AttackedBy(this);

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
                        Spell spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                        if (spell)
                        {
                            if (spell.getState() == SpellState.Preparing)
                            {
                                SpellInterruptFlags interruptFlags = spell.m_spellInfo.InterruptFlags;
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

                    Unit shareDamageTarget = aura.GetCaster();
                    if (shareDamageTarget == null)
                        continue;
                    SpellInfo spell = aura.GetSpellInfo();

                    uint share = MathFunctions.CalculatePct(damage, aura.GetAmount());

                    // @todo check packets if damage is done by victim, or by attacker of victim
                    DealDamageMods(shareDamageTarget, ref share);
                    DealDamage(shareDamageTarget, share, null, DamageEffectType.NoDamage, spell.GetSchoolMask(), spell, false);
                }
            }

            // Rage from Damage made (only from direct weapon damage)
            if (cleanDamage != null && (cleanDamage.attackType == WeaponAttackType.BaseAttack || cleanDamage.attackType == WeaponAttackType.OffAttack) && damagetype == DamageEffectType.Direct && this != victim && GetPowerType() == PowerType.Rage)
            {
                uint rage = (uint)(GetBaseAttackTime(cleanDamage.attackType) / 1000.0f * 1.75f);
                if (cleanDamage.attackType == WeaponAttackType.OffAttack)
                    rage /= 2;
                RewardRage(rage);
            }

            if (damage == 0)
                return 0;

            Log.outDebug(LogFilter.Unit, "DealDamageStart");

            uint health = (uint)victim.GetHealth();
            Log.outDebug(LogFilter.Unit, "Unit {0} dealt {1} damage to unit {2}", GetGUID(), damage, victim.GetGUID());

            // duel ends when player has 1 or less hp
            bool duel_hasEnded = false;
            bool duel_wasMounted = false;
            if (victim.IsTypeId(TypeId.Player) && victim.ToPlayer().duel != null && damage >= (health - 1))
            {
                // prevent kill only if killed in duel and killed by opponent or opponent controlled creature
                if (victim.ToPlayer().duel.opponent == this || victim.ToPlayer().duel.opponent.GetGUID() == GetOwnerGUID())
                    damage = health - 1;

                duel_hasEnded = true;
            }
            else if (victim.IsVehicle() && damage >= (health - 1) && victim.GetCharmer() != null && victim.GetCharmer().IsTypeId(TypeId.Player))
            {
                Player victimRider = victim.GetCharmer().ToPlayer();
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
                Player killer = ToPlayer();

                // in bg, count dmg if victim is also a player
                if (victim.IsTypeId(TypeId.Player))
                {
                    Battleground bg = killer.GetBattleground();
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
            else if (!victim.IsControlledByPlayer() || victim.IsVehicle())
            {
                if (!victim.ToCreature().hasLootRecipient())
                    victim.ToCreature().SetLootRecipient(this);

                if (IsControlledByPlayer())
                    victim.ToCreature().LowerPlayerDamageReq(health < damage ? health : damage);
            }

            damage = (uint)(damage / victim.GetHealthMultiplierForTarget(this));

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
                {
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.DirectDamage, spellProto != null ? spellProto.Id : 0);
                    victim.UpdateLastDamagedTime(spellProto);
                }

                if (!victim.IsTypeId(TypeId.Player))
                {
                    victim.AddThreat(this, damage, damageSchoolMask, spellProto);
                }
                else                                                // victim is a player
                {
                    // random durability for items (HIT TAKEN)
                    if (WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage) > RandomHelper.randChance())
                    {
                        byte slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                        victim.ToPlayer().DurabilityPointLossForEquipSlot(slot);
                    }
                }

                if (IsTypeId(TypeId.Player))
                {
                    // random durability for items (HIT DONE)
                    if (RandomHelper.randChance(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage)))
                    {
                        byte slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
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
                            Spell spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);
                            if (spell != null)
                                if (spell.getState() == SpellState.Preparing)
                                {
                                    var interruptFlags = spell.m_spellInfo.InterruptFlags;
                                    if (interruptFlags.HasAnyFlag(SpellInterruptFlags.AbortOnDmg))
                                        victim.InterruptNonMeleeSpells(false);
                                    else if (interruptFlags.HasAnyFlag(SpellInterruptFlags.PushBack))
                                        spell.Delayed();
                                }
                        }
                        Spell spell1 = victim.GetCurrentSpell(CurrentSpellTypes.Channeled);
                        if (spell1 != null)
                            if (spell1.getState() == SpellState.Casting && spell1.m_spellInfo.HasChannelInterruptFlag(SpellChannelInterruptFlags.Delay) && damagetype != DamageEffectType.DOT)
                                spell1.DelayedChannel();
                    }
                }
                // last damage from duel opponent
                if (duel_hasEnded)
                {
                    Player he = duel_wasMounted ? victim.GetCharmer().ToPlayer() : victim.ToPlayer();

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

            long curHealth = (long)GetHealth();

            long val = dVal + curHealth;
            if (val <= 0)
            {
                SetHealth(0);
                return -curHealth;
            }

            long maxHealth = (long)GetMaxHealth();
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
                HealthUpdate packet = new HealthUpdate();
                packet.Guid = GetGUID();
                packet.Health = (long)GetHealth();

                Player player = GetCharmerOrOwnerPlayerOrPlayerItself();
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

            long curHealth = (long)GetHealth();

            long val = dVal + curHealth;
            if (val <= 0)
            {
                return -curHealth;
            }

            long maxHealth = (long)GetMaxHealth();

            if (val < maxHealth)
                gain = dVal;
            else if (curHealth != maxHealth)
                gain = maxHealth - curHealth;

            return gain;
        }

        public void SendAttackStateUpdate(HitInfo HitInfo, Unit target, SpellSchoolMask damageSchoolMask, uint Damage, uint AbsorbDamage, uint Resist, VictimState TargetState, uint BlockedAmount)
        {
            CalcDamageInfo dmgInfo = new CalcDamageInfo();
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
            AttackerStateUpdate packet = new AttackerStateUpdate();
            packet.hitInfo = damageInfo.HitInfo;
            packet.AttackerGUID = damageInfo.attacker.GetGUID();
            packet.VictimGUID = damageInfo.target.GetGUID();
            packet.Damage = (int)damageInfo.damage;
            packet.OriginalDamage = (int)damageInfo.originalDamage;
            int overkill = (int)(damageInfo.damage - damageInfo.target.GetHealth());
            packet.OverDamage = (overkill < 0 ? -1 : overkill);

            SubDamage subDmg = new SubDamage();
            subDmg.SchoolMask = (int)damageInfo.damageSchoolMask;   // School of sub damage
            subDmg.FDamage = damageInfo.damage;                // sub damage
            subDmg.Damage = (int)damageInfo.damage;                 // Sub Damage
            subDmg.Absorbed = (int)damageInfo.absorb;
            subDmg.Resisted = (int)damageInfo.resist;
            packet.SubDmg.Set(subDmg);

            packet.VictimState = (byte)damageInfo.TargetState;
            packet.BlockAmount = (int)damageInfo.blocked_amount;
            packet.LogData.Initialize(damageInfo.attacker);

            ContentTuningParams contentTuningParams = new ContentTuningParams();
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
                {
                    if (target.IsPet())
                        target.ToCreature().GetAI().AttackedBy(this); // PetAI has special handler before AttackStart()
                    else
                        target.ToCreature().GetAI().AttackStart(this);
                }

                SetInCombatWith(target);
                target.SetInCombatWith(this);
            }
            Unit who = target.GetCharmerOrOwnerOrSelf();
            if (who.IsTypeId(TypeId.Player))
                SetContestedPvP(who.ToPlayer());

            Player me = GetCharmerOrOwnerPlayerOrPlayerItself();
            if (me != null && who.IsPvP() && (!who.IsTypeId(TypeId.Player) || me.duel == null || me.duel.opponent != who))
            {
                me.UpdatePvP(true);
                me.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);
            }
        }
        public void SetInCombatWith(Unit enemy)
        {
            Unit eOwner = enemy.GetCharmerOrOwnerOrSelf();
            if (eOwner.IsPvP())
            {
                SetInCombatState(true, enemy);
                return;
            }

            // check for duel
            if (eOwner.IsTypeId(TypeId.Player) && eOwner.ToPlayer().duel != null)
            {
                Unit myOwner = GetCharmerOrOwnerOrSelf();
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
                m_CombatTimer = 5000;
                Player me = ToPlayer();
                if (me)
                    me.EnablePvpRules(true);
            }

            if (IsInCombat() || HasUnitState(UnitState.Evade))
                return;

            SetFlag(UnitFields.Flags, UnitFlags.InCombat);

            Creature creature = ToCreature();
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
                        creature.GetFormation().MemberAttackStart(creature, enemy);
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
            {
                unit.SetInCombatState(PvP, enemy);
                unit.SetFlag(UnitFields.Flags, UnitFlags.PetInCombat);
            }

            ProcSkillsAndAuras(enemy, ProcFlags.EnterCombat, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        internal void SendCombatLogMessage(CombatLogServerPacket combatLog)
        {
            CombatLogSender notifier = new CombatLogSender(this, combatLog, GetVisibilityRange());
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public void Kill(Unit victim, bool durabilityLoss = true)
        {
            // Prevent killing unit twice (and giving reward from kill twice)
            if (victim.GetHealth() == 0)
                return;

            // find player: owner of controlled `this` or `this` itself maybe
            Player player = GetCharmerOrOwnerPlayerOrPlayerItself();
            Creature creature = victim.ToCreature();

            bool isRewardAllowed = true;
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
                PartyKillLog partyKillLog = new PartyKillLog();
                partyKillLog.Player = player.GetGUID();
                partyKillLog.Victim = victim.GetGUID();

                Player looter = player;
                var group = player.GetGroup();
                bool hasLooterGuid = false;
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
                        LootList lootList = new LootList();
                        lootList.Owner = creature.GetGUID();
                        lootList.LootObj = creature.loot.GetGUID();
                        player.SendMessageToSet(lootList, true);
                    }
                }

                if (creature)
                {
                    Loot loot = creature.loot;

                    loot.clear();
                    uint lootid = creature.GetCreatureTemplate().LootId;
                    if (lootid != 0)
                        loot.FillLoot(lootid, LootStorage.Creature, looter, false, false, creature.GetLootMode());

                    loot.generateMoneyLoot(creature.GetCreatureTemplate().MinGold, creature.GetCreatureTemplate().MaxGold);

                    if (group)
                    {
                        if (hasLooterGuid)
                            group.SendLooter(creature, looter);
                        else
                            group.SendLooter(creature, null);

                        // Update round robin looter only if the creature had loot
                        if (!loot.empty())
                            group.UpdateLooterGuid(creature);
                    }
                }

                player.RewardPlayerAndGroupAtKill(victim, false);
            }

            // Do KILL and KILLED procs. KILL proc is called only for the unit who landed the killing blow (and its owner - for pets and totems) regardless of who tapped the victim
            if (IsPet() || IsTotem())
            {
                // proc only once for victim
                Unit owner = GetOwner();
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
            victim.setDeathState(DeathState.JustDied);

            // Inform pets (if any) when player kills target)
            // MUST come after victim.setDeathState(JUST_DIED); or pet next target
            // selection will get stuck on same target and break pet react state
            if (player != null)
            {
                Pet pet = player.GetPet();
                if (pet != null && pet.IsAlive() && pet.isControlled())
                    pet.GetAI().KilledUnit(victim);
            }

            // 10% durability loss on death
            // clean InHateListOf
            Player plrVictim = victim.ToPlayer();
            if (plrVictim != null)
            {
                // remember victim PvP death for corpse type and corpse reclaim delay
                // at original death (not at SpiritOfRedemtionTalent timeout)
                plrVictim.SetPvPDeath(player != null);

                // only if not player and not controlled by player pet. And not at BG
                if ((durabilityLoss && player == null && !victim.ToPlayer().InBattleground()) || (player != null && WorldConfig.GetBoolValue(WorldCfg.DurabilityLossInPvp)))
                {
                    double baseLoss = WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath);
                    uint loss = (uint)(baseLoss - (baseLoss * plrVictim.GetTotalAuraMultiplier(AuraType.ModDurabilityLoss)));
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
                    creature.DeleteThreatList();

                    // must be after setDeathState which resets dynamic flags
                    if (!creature.loot.isLooted())
                        creature.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Lootable);
                    else
                        creature.AllLootRemovedFromCorpse();
                }

                // Call KilledUnit for creatures, this needs to be called after the lootable flag is set
                if (IsTypeId(TypeId.Unit) && IsAIEnabled)
                    ToCreature().GetAI().KilledUnit(victim);

                // Call creature just died function
                if (creature.IsAIEnabled)
                    creature.GetAI().JustDied(this);

                TempSummon summon = creature.ToTempSummon();
                if (summon != null)
                {
                    Unit summoner = summon.GetSummoner();
                    if (summoner != null)
                        if (summoner.IsTypeId(TypeId.Unit) && summoner.IsAIEnabled)
                            summoner.ToCreature().GetAI().SummonedCreatureDies(creature, this);
                }

                // Dungeon specific stuff, only applies to players killing creatures
                if (creature.GetInstanceId() != 0)
                {
                    Map instanceMap = creature.GetMap();
                    Player creditedPlayer = GetCharmerOrOwnerPlayerOrPlayerItself();
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
                            long resettime = creature.GetRespawnTimeEx() + 2 * Time.Hour;
                            InstanceSave save = Global.InstanceSaveMgr.GetInstanceSave(creature.GetInstanceId());
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
                OutdoorPvP pvp = player.GetOutdoorPvP();
                if (pvp != null)
                    pvp.HandleKill(player, victim);

                BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
                if (bf != null)
                    bf.HandleKill(player, victim);
            }

            // Battlegroundthings (do this at the end, so the death state flag will be properly set to handle in the bg.handlekill)
            if (player != null && player.InBattleground())
            {
                Battleground bg = player.GetBattleground();
                if (bg)
                {
                    Player playerVictim = victim.ToPlayer();
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
            Player killerPlr = ToPlayer();
            Creature killerCre = ToCreature();
            if (killerPlr != null)
            {
                Player killedPlr = victim.ToPlayer();
                Creature killedCre = victim.ToCreature();
                if (killedPlr != null)
                    Global.ScriptMgr.OnPVPKill(killerPlr, killedPlr);
                else if (killedCre != null)
                    Global.ScriptMgr.OnCreatureKill(killerPlr, killedCre);
            }
            else if (killerCre != null)
            {
                Player killed = victim.ToPlayer();
                if (killed != null)
                    Global.ScriptMgr.OnPlayerKilledByCreature(killerCre, killed);
            }
        }

        public void KillSelf(bool durabilityLoss = true) { Kill(this, durabilityLoss); }

        public virtual uint GetBlockPercent() { return 30; }

        public void SetContestedPvP(Player attackedPlayer = null)
        {
            Player player = GetCharmerOrOwnerPlayerOrPlayerItself();

            if (player == null || (attackedPlayer != null && (attackedPlayer == player || (player.duel != null && player.duel.opponent == attackedPlayer))))
                return;

            player.SetContestedPvPTimer(30000);
            if (!player.HasUnitState(UnitState.AttackPlayer))
            {
                player.AddUnitState(UnitState.AttackPlayer);
                player.SetFlag(PlayerFields.Flags, PlayerFlags.ContestedPVP);
                // call MoveInLineOfSight for nearby contested guards
                UpdateObjectVisibility();
            }
            if (!HasUnitState(UnitState.AttackPlayer))
            {
                AddUnitState(UnitState.AttackPlayer);
                // call MoveInLineOfSight for nearby contested guards
                UpdateObjectVisibility();
            }
        }

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
                Player modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.ProcPerMinute, ref PPM);
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
            Group group = player.GetGroup();
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

            List<Unit> nearMembers = new List<Unit>();
            // reserve place for players and pets because resizing vector every unit push is unefficient (vector is reallocated then)

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
            {
                Player Target = refe.GetSource();
                if (Target)
                {
                    // IsHostileTo check duel and controlled by enemy
                    if (Target != this && Target.IsAlive() && IsWithinDistInMap(Target, radius) && !IsHostileTo(Target))
                        nearMembers.Add(Target);

                    // Push player's pet to vector
                    Unit pet = Target.GetGuardianPet();
                    if (pet)
                        if (pet != this && pet.IsAlive() && IsWithinDistInMap(pet, radius) && !IsHostileTo(pet))
                            nearMembers.Add(pet);
                }
            }

            if (nearMembers.Empty())
                return null;

            int randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);
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
            damage = damageInfo.target.MeleeDamageBonusTaken(this, damage, damageInfo.attackType);

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
                    float mod = (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, damageInfo.damageSchoolMask) - 1.0f) * 100;

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
                    damageInfo.blocked_amount = MathFunctions.CalculatePct(damageInfo.damage, damageInfo.target.isBlockCritical() ? damageInfo.target.GetBlockPercent() * 2 : damageInfo.target.GetBlockPercent());
                    damageInfo.damage -= damageInfo.blocked_amount;
                    damageInfo.cleanDamage += damageInfo.blocked_amount;
                    break;
                case MeleeHitOutcome.Glancing:
                    damageInfo.HitInfo |= HitInfo.Glancing;
                    damageInfo.TargetState = VictimState.Hit;
                    damageInfo.originalDamage = damageInfo.damage;
                    int leveldif = (int)victim.getLevel() - (int)getLevel();
                    if (leveldif > 3)
                        leveldif = 3;

                    float reducePercent = 1.0f - leveldif * 0.1f;
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

            uint resilienceReduction = damageInfo.damage;
            ApplyResilience(victim, ref resilienceReduction);
            resilienceReduction = damageInfo.damage - resilienceReduction;
            damageInfo.damage -= resilienceReduction;
            damageInfo.cleanDamage += resilienceReduction;

            // Calculate absorb resist
            if (damageInfo.damage > 0)
            {
                damageInfo.procVictim |= ProcFlags.TakenDamage;
                // Calculate absorb & resists
                DamageInfo dmgInfo = new DamageInfo(damageInfo);
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
            int miss_chance = (int)(MeleeSpellMissChance(victim, attType, 0) * 100.0f);

            // Critical hit chance
            int crit_chance = (int)(GetUnitCriticalChance(attType, victim) * 100.0f);

            int dodge_chance = (int)(GetUnitDodgeChance(attType, victim) * 100.0f);
            int block_chance = (int)(GetUnitBlockChance(attType, victim) * 100.0f);
            int parry_chance = (int)(GetUnitParryChance(attType, victim) * 100.0f);

            // melee attack table implementation
            // outcome priority:
            //   1. >    2. >    3. >       4. >    5. >   6. >       7. >  8.
            // MISS > DODGE > PARRY > GLANCING > BLOCK > CRIT > CRUSHING > HIT

            int sum = 0, tmp = 0;
            int roll = RandomHelper.IRand(0, 9999);

            uint attackerLevel = GetLevelForTarget(victim);
            uint victimLevel = GetLevelForTarget(this);

            // check if attack comes from behind, nobody can parry or block if attacker is behind
            bool canParryOrBlock = victim.HasInArc((float)Math.PI, this) || victim.HasAuraType(AuraType.IgnoreHitDirection);

            // only creatures can dodge if attacker is behind
            bool canDodge = !victim.IsTypeId(TypeId.Player) || canParryOrBlock;

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
            float minDamage, maxDamage = 0.0f;

            if (normalized || !addTotalPct)
            {
                CalculateMinMaxDamage(attType, normalized, addTotalPct, out minDamage, out maxDamage);
                if (IsInFeralForm() && attType == WeaponAttackType.BaseAttack)
                {
                    float minOffhandDamage = 0.0f;
                    float maxOffhandDamage = 0.0f;
                    CalculateMinMaxDamage(WeaponAttackType.OffAttack, normalized, addTotalPct, out minOffhandDamage, out maxOffhandDamage);
                    minDamage += minOffhandDamage;
                    maxDamage += maxOffhandDamage;
                }
            }
            else
            {
                switch (attType)
                {
                    case WeaponAttackType.RangedAttack:
                        minDamage = GetFloatValue(UnitFields.MinRangedDamage);
                        maxDamage = GetFloatValue(UnitFields.MaxRangedDamage);
                        break;
                    case WeaponAttackType.BaseAttack:
                        minDamage = GetFloatValue(UnitFields.MinDamage);
                        maxDamage = GetFloatValue(UnitFields.MaxDamage);
                        if (IsInFeralForm())
                        {
                            minDamage += GetFloatValue(UnitFields.MinOffHandDamage);
                            maxDamage += GetFloatValue(UnitFields.MaxOffHandDamage);
                        }
                        break;
                    case WeaponAttackType.OffAttack:
                        minDamage = GetFloatValue(UnitFields.MinOffHandDamage);
                        maxDamage = GetFloatValue(UnitFields.MaxOffHandDamage);
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
            if (attType == WeaponAttackType.OffAttack && !haveOffhandWeapon())
                return 0.0f;

            return m_weaponDamage[(int)attType][(int)type];
        }
        public float GetAPMultiplier(WeaponAttackType attType, bool normalized)
        {
            if (!IsTypeId(TypeId.Player) || (IsInFeralForm() && !normalized))
                return GetBaseAttackTime(attType) / 1000.0f;

            Item weapon = ToPlayer().GetWeaponForAttack(attType, true);
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
        public float GetTotalAttackPowerValue(WeaponAttackType attType)
        {
            if (attType == WeaponAttackType.RangedAttack)
            {
                int ap = GetInt32Value(UnitFields.RangedAttackPower);
                if (ap < 0)
                    return 0.0f;
                return ap * (1.0f + GetFloatValue(UnitFields.RangedAttackPowerMultiplier));
            }
            else
            {
                int ap = GetInt32Value(UnitFields.AttackPower);
                if (ap < 0)
                    return 0.0f;
                return ap * (1.0f + GetFloatValue(UnitFields.AttackPowerMultiplier));
            }
        }
        public float GetModifierValue(UnitMods unitMod, UnitModifierType modifierType)
        {
            if (unitMod >= UnitMods.End || modifierType >= UnitModifierType.End)
            {
                Log.outError(LogFilter.Unit, "attempt to access non-existing modifier value from UnitMods!");
                return 0.0f;
            }

            if (modifierType == UnitModifierType.TotalPCT && m_auraModifiersGroup[(int)unitMod][(int)modifierType] <= 0.0f)
                return 0.0f;

            return m_auraModifiersGroup[(int)unitMod][(int)modifierType];
        }
        public bool IsWithinMeleeRange(Unit obj)
        {
            if (!obj || !IsInMap(obj) || !IsInPhase(obj))
                return false;

            float dx = GetPositionX() - obj.GetPositionX();
            float dy = GetPositionY() - obj.GetPositionY();
            float dz = GetPositionZMinusOffset() - obj.GetPositionZMinusOffset();
            float distsq = (dx * dx) + (dy * dy) + (dz * dz);

            float maxdist = GetMeleeRange(obj);

            return distsq <= maxdist * maxdist;
        }

        public float GetMeleeRange(Unit target)
        {
            float range = GetCombatReach() + target.GetCombatReach() + 4.0f / 3.0f;
            return Math.Max(range, SharedConst.NominalMeleeRange);
        }

        public void SetBaseAttackTime(WeaponAttackType att, uint val)
        {
            m_baseAttackSpeed[(int)att] = val;
            UpdateAttackTimeField(att);
        }
        void UpdateAttackTimeField(WeaponAttackType att)
        {
            SetUInt32Value(UnitFields.BaseAttackTime + (int)att, (uint)(m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att]));
        }
        public virtual void SetPvP(bool state)
        {
            if (state)
                SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.PvP);
            else
                RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.PvP);
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

        uint CalcSpellResistance(Unit victim, SpellSchoolMask schoolMask, SpellInfo spellInfo)
        {
            // Magic damage, check for resists
            if (!Convert.ToBoolean(schoolMask & SpellSchoolMask.Spell))
                return 0;

            // Ignore spells that can't be resisted
            if (spellInfo != null && spellInfo.HasAttribute(SpellAttr4.IgnoreResistances))
                return 0;

            byte bossLevel = 83;
            uint bossResistanceConstant = 510;
            uint resistanceConstant = 0;
            uint level = victim.GetLevelForTarget(this);

            if (level == bossLevel)
                resistanceConstant = bossResistanceConstant;
            else
                resistanceConstant = level * 5;

            int baseVictimResistance = (int)victim.GetResistance(schoolMask);
            baseVictimResistance += GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);

            Player player = ToPlayer();
            if (player)
                baseVictimResistance -= player.GetSpellPenetrationItemMod();

            // Resistance can't be lower then 0
            int victimResistance = Math.Max(baseVictimResistance, 0);

            if (victimResistance > 0)
            {
                int ignoredResistance = GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)schoolMask);
                ignoredResistance = Math.Min(ignoredResistance, 100);
                MathFunctions.ApplyPct(ref victimResistance, 100 - ignoredResistance);
            }

            if (victimResistance <= 0)
                return 0;

            float averageResist = (float)victimResistance / (float)(victimResistance + resistanceConstant);

            float[] discreteResistProbability = new float[11];
            for (uint i = 0; i < 11; ++i)
            {
                discreteResistProbability[i] = 0.5f - 2.5f * Math.Abs(0.1f * i - averageResist);
                if (discreteResistProbability[i] < 0.0f)
                    discreteResistProbability[i] = 0.0f;
            }

            if (averageResist <= 0.1f)
            {
                discreteResistProbability[0] = 1.0f - 7.5f * averageResist;
                discreteResistProbability[1] = 5.0f * averageResist;
                discreteResistProbability[2] = 2.5f * averageResist;
            }

            uint resistance = 0;
            float r = (float)RandomHelper.NextDouble();
            float probabilitySum = discreteResistProbability[0];

            while (r >= probabilitySum && resistance < 10)
                probabilitySum += discreteResistProbability[++resistance];

            return resistance * 10;
        }

        public void CalcAbsorbResist(DamageInfo damageInfo)
        {
            if (!damageInfo.GetVictim() || !damageInfo.GetVictim().IsAlive() || damageInfo.GetDamage() == 0)
                return;

            uint spellResistance = CalcSpellResistance(damageInfo.GetVictim(), damageInfo.GetSchoolMask(), damageInfo.GetSpellInfo());
            damageInfo.ResistDamage(MathFunctions.CalculatePct(damageInfo.GetDamage(), spellResistance));

            // Ignore Absorption Auras
            float auraAbsorbMod = GetMaxPositiveAuraModifierByMiscMask(AuraType.ModTargetAbsorbSchool, (uint)damageInfo.GetSchoolMask());

            auraAbsorbMod = Math.Max(auraAbsorbMod, (float)GetMaxPositiveAuraModifier(AuraType.ModTargetAbilityAbsorbSchool, aurEff =>
            {
                if (!Convert.ToBoolean(aurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    return false;

                if (!aurEff.IsAffectingSpell(damageInfo.GetSpellInfo()))
                    return false;

                return true;
            }));

            MathFunctions.RoundToInterval(ref auraAbsorbMod, 0.0f, 100.0f);

            int absorbIgnoringDamage = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), auraAbsorbMod);
            damageInfo.ModifyDamage(-absorbIgnoringDamage);

            // We're going to call functions which can modify content of the list during iteration over it's elements
            // Let's copy the list so we can prevent iterator invalidation
            var vSchoolAbsorbCopy = damageInfo.GetVictim().GetAuraEffectsByType(AuraType.SchoolAbsorb);
            vSchoolAbsorbCopy.Sort(new AbsorbAuraOrderPred());

            // absorb without mana cost
            for (var i = 0; i < vSchoolAbsorbCopy.Count; ++i )
            {
                var absorbAurEff = vSchoolAbsorbCopy[i];
                if (damageInfo.GetDamage() == 0)
                    break;

                // Check if aura was removed during iteration - we don't need to work on such auras
                AuraApplication aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                if (aurApp == null)
                    continue;
                if (!Convert.ToBoolean(absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    continue;

                // get amount which can be still absorbed by the aura
                int currentAbsorb = absorbAurEff.GetAmount();
                // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                uint tempAbsorb = (uint)currentAbsorb;

                bool defaultPrevented = false;

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
                AuraApplication aurApp = absorbAurEff.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                if (aurApp == null)
                    continue;
                // check damage school mask
                if (!Convert.ToBoolean(absorbAurEff.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                    continue;

                // get amount which can be still absorbed by the aura
                int currentAbsorb = absorbAurEff.GetAmount();
                // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                if (currentAbsorb < 0)
                    currentAbsorb = 0;

                uint tempAbsorb = (uint)currentAbsorb;

                bool defaultPrevented = false;

                absorbAurEff.GetBase().CallScriptEffectManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
                currentAbsorb = (int)tempAbsorb;

                if (defaultPrevented)
                    continue;

                // absorb must be smaller than the damage itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.GetDamage());

                int manaReduction = currentAbsorb;

                // lower absorb amount by talents
                float manaMultiplier = absorbAurEff.GetSpellEffectInfo().CalcValueMultiplier(absorbAurEff.GetCaster());
                if (manaMultiplier != 0)
                    manaReduction = (int)(manaReduction * manaMultiplier);

                int manaTaken = -damageInfo.GetVictim().ModifyPower(PowerType.Mana, -manaReduction);

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
                    AuraApplication aurApp = itr.GetBase().GetApplicationOfTarget(damageInfo.GetVictim().GetGUID());
                    if (aurApp == null)
                        continue;

                    // check damage school mask
                    if (!Convert.ToBoolean(itr.GetMiscValue() & (int)damageInfo.GetSchoolMask()))
                        continue;

                    // Damage can be splitted only if aura has an alive caster
                    Unit caster = itr.GetCaster();
                    if (!caster || (caster == damageInfo.GetVictim()) || !caster.IsInWorld || !caster.IsAlive())
                        continue;

                    uint splitDamage = MathFunctions.CalculatePct(damageInfo.GetDamage(), itr.GetAmount());

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

                    SpellNonMeleeDamage log = new SpellNonMeleeDamage(this, caster, itr.GetSpellInfo().Id, itr.GetBase().GetSpellXSpellVisualId(), damageInfo.GetSchoolMask(), itr.GetBase().GetCastGUID());
                    CleanDamage cleanDamage = new CleanDamage(splitDamage, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
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
            bool existExpired = false;

            // absorb without mana cost
            var vHealAbsorb = healInfo.GetTarget().GetAuraEffectsByType(AuraType.SchoolHealAbsorb);
            foreach (var eff in vHealAbsorb)
            {
                if (healInfo.GetHeal() <= 0)
                    break;

                if (!Convert.ToBoolean(eff.GetMiscValue() & (int)healInfo.GetSpellInfo().SchoolMask))
                    continue;

                // Max Amount can be absorbed by this aura
                int currentAbsorb = eff.GetAmount();

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
                    AuraEffect auraEff = vHealAbsorb[i];
                    ++i;
                    if (auraEff.GetAmount() <= 0)
                    {
                        uint removedAuras = healInfo.GetTarget().m_removedAurasCount;
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
            int armorBypassPct = 0;
            var reductionAuras = victim.GetAuraEffectsByType(AuraType.BypassArmorForCaster);
            foreach (var eff in reductionAuras)
                if (eff.GetCasterGUID() == GetGUID())
                    armorBypassPct += eff.GetAmount();
            armor = MathFunctions.CalculatePct(armor, 100 - Math.Min(armorBypassPct, 100));

            // Ignore enemy armor by SPELL_AURA_MOD_TARGET_RESISTANCE aura
            armor += GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)SpellSchoolMask.Normal);

            if (spellInfo != null)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.IgnoreArmor, ref armor);
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
                float arpPct = ToPlayer().GetRatingBonusValue(CombatRating.ArmorPenetration);

                // no more than 100%
                MathFunctions.RoundToInterval(ref arpPct, 0.0f, 100.0f);

                float maxArmorPen = 0.0f;
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

            uint attackerLevel = attacker.GetLevelForTarget(victim);
            if (attackerLevel > CliDB.ArmorMitigationByLvlGameTable.GetTableRowCount())
                attackerLevel = (uint)CliDB.ArmorMitigationByLvlGameTable.GetTableRowCount();

            GtArmorMitigationByLvlRecord ambl = CliDB.ArmorMitigationByLvlGameTable.GetRow(attackerLevel);
            if (ambl == null)
                return damage;

            float mitigation = Math.Min(armor / (armor + ambl.Mitigation), 0.85f);
            return Math.Max((uint)(damage * (1.0f - mitigation)), 1);
        }

        public uint MeleeDamageBonusDone(Unit victim, uint pdamage, WeaponAttackType attType, SpellInfo spellProto = null)
        {
            if (victim == null || pdamage == 0)
                return 0;

            uint creatureTypeMask = victim.GetCreatureTypeMask();

            // Done fixed damage bonus auras
            int DoneFlatBenefit = 0;

            // ..done
            DoneFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageDoneCreature, (int)creatureTypeMask);

            // ..done
            // SPELL_AURA_MOD_DAMAGE_DONE included in weapon damage

            // ..done (base at attack power for marked target and base at attack power for creature type)
            int APbonus = 0;

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
                bool normalized = spellProto != null && spellProto.HasEffect(GetMap().GetDifficultyID(), SpellEffectName.NormalizedWeaponDmg);
                DoneFlatBenefit += (int)(APbonus / 3.5f * GetAPMultiplier(attType, normalized));
            }

            // Done total percent damage auras
            float DoneTotalMod = 1.0f;

            // Some spells don't benefit from pct done mods
            if (spellProto != null && !spellProto.HasAttribute(SpellAttr6.NoDonePctDamageMods))
            {
                // mods for SPELL_SCHOOL_MASK_NORMAL are already factored in base melee damage calculation
                if (!spellProto.GetSchoolMask().HasAnyFlag(SpellSchoolMask.Normal))
                {
                    float maxModDamagePercentSchool = 0.0f;
                    for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
                    {
                        if (Convert.ToBoolean((int)spellProto.GetSchoolMask() & (1 << (int)i)))
                            maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, GetFloatValue(ActivePlayerFields.ModDamageDonePct + (int)i));
                    }

                    DoneTotalMod *= maxModDamagePercentSchool;
                }
            }

            DoneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, (uint)creatureTypeMask);

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

            float tmpDamage = (pdamage + DoneFlatBenefit) * DoneTotalMod;

            // apply spellmod to Done damage
            if (spellProto != null)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Damage, ref tmpDamage);
            }

            // bonus result can be negative
            return (uint)Math.Max(tmpDamage, 0.0f);
        }
        public uint MeleeDamageBonusTaken(Unit attacker, uint pdamage, WeaponAttackType attType, SpellInfo spellProto = null)
        {
            if (pdamage == 0)
                return 0;

            int TakenFlatBenefit = 0;
            float TakenTotalCasterMod = 0.0f;

            // get all auras from caster that allow the spell to ignore resistance (sanctified wrath)
            int attackSchoolMask = (int)(spellProto != null ? spellProto.GetSchoolMask() : SpellSchoolMask.Normal);
            TakenTotalCasterMod += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, attackSchoolMask);

            // ..taken
            TakenFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)attacker.GetMeleeDamageSchoolMask());

            if (attType != WeaponAttackType.RangedAttack)
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModMeleeDamageTaken);
            else
                TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModRangedDamageTaken);

            // Taken total percent damage auras
            float TakenTotalMod = 1.0f;

            // ..taken
            TakenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)attacker.GetMeleeDamageSchoolMask());

            // .. taken pct (special attacks)
            if (spellProto != null)
            {
                // From caster spells
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff =>
                {
                    if (aurEff.GetCasterGUID() == attacker.GetGUID() && aurEff.IsAffectingSpell(spellProto))
                        return true;
                    return false;
                });

                // Mod damage from spell mechanic
                uint mechanicMask = spellProto.GetAllEffectsMechanicMask();

                // Shred, Maul - "Effects which increase Bleed damage also increase Shred damage"
                if (spellProto.SpellFamilyName == SpellFamilyNames.Druid && spellProto.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00008800))
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
            }

            AuraEffect cheatDeath = GetAuraEffect(45182, 0);
            if (cheatDeath != null)
                MathFunctions.AddPct(ref TakenTotalMod, cheatDeath.GetAmount());

            if (attType != WeaponAttackType.RangedAttack)
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageTakenPct);
            else
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModRangedDamageTakenPct);

            // Versatility
            Player modOwner = GetSpellModOwner();
            if (modOwner)
            {
                // only 50% of SPELL_AURA_MOD_VERSATILITY for damage reduction
                float versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
                MathFunctions.AddPct(ref TakenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
            }

            float tmpDamage = 0.0f;

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

        bool isBlockCritical()
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
            float DotFactor = 1.0f;
            if (damagetype == DamageEffectType.DOT)
            {

                int DotDuration = spellInfo.GetDuration();
                if (!spellInfo.IsChanneled() && DotDuration > 0)
                    DotFactor = DotDuration / 15000.0f;

                uint DotTicks = spellInfo.GetMaxTicks(GetMap().GetDifficultyID());
                if (DotTicks != 0)
                    DotFactor /= DotTicks;
            }

            uint CastingTime = (uint)(spellInfo.IsChanneled() ? spellInfo.GetDuration() : spellInfo.CalcCastTime());
            // Distribute Damage over multiple effects, reduce by AoE
            CastingTime = GetCastingTimeForBonus(spellInfo, damagetype, CastingTime);

            // As wowwiki says: C = (Cast Time / 3.5)
            return (CastingTime / 3500.0f) * DotFactor;
        }

        public void ApplyAttackTimePercentMod(WeaponAttackType att, float val, bool apply)
        {
            float remainingTimePct = m_attackTimer[(int)att] / (m_baseAttackSpeed[(int)att] * m_modAttackSpeedPct[(int)att]);
            if (val > 0)
            {
                MathFunctions.ApplyPercentModFloatVar(ref m_modAttackSpeedPct[(int)att], val, !apply);

                if (att == WeaponAttackType.BaseAttack)
                    ApplyPercentModFloatValue(UnitFields.ModHaste, val, !apply);
                else if (att == WeaponAttackType.RangedAttack)
                    ApplyPercentModFloatValue(UnitFields.ModRangedHaste, val, !apply);
            }
            else
            {
                MathFunctions.ApplyPercentModFloatVar(ref m_modAttackSpeedPct[(int)att], -val, apply);

                if (att == WeaponAttackType.BaseAttack)
                    ApplyPercentModFloatValue(UnitFields.ModHaste, -val, apply);
                else if (att == WeaponAttackType.RangedAttack)
                    ApplyPercentModFloatValue(UnitFields.ModRangedHaste, -val, apply);
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
            if (GetEntry() != SharedConst.WorldTrigger && (!obj || !obj.isTypeMask(TypeMask.GameObject | TypeMask.DynamicObject)))
            {
                // can't attack invisible
                if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible))
                {
                    if (obj && !obj.CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea(GetMap().GetDifficultyID())))
                        return false;
                    else if (!obj)
                    {
                        // ignore stealth for aoe spells. Ignore stealth if target is player and unit in combat with same player
                        bool ignoreStealthCheck = (bySpell != null && bySpell.IsAffectingArea(GetMap().GetDifficultyID())) ||
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
                && target.HasFlag(UnitFields.Flags, UnitFlags.NotSelectable))
                return false;

            Player playerAttacker = ToPlayer();
            if (playerAttacker != null)
            {
                if (playerAttacker.HasFlag(PlayerFields.Flags, PlayerFlags.Commentator2))
                    return false;
            }

            // check flags
            if (target.HasFlag(UnitFields.Flags, (UnitFlags.NonAttackable | UnitFlags.TaxiFlight | UnitFlags.NotAttackable1 | UnitFlags.Unk16))
                || (!HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) && target.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc))
                || (!target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) && HasFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc)))
                return false;

            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
                && (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) && target.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToPc))
                // check if this is a world trigger cast - GOs are using world triggers to cast their spells, so we need to ignore their immunity flag here, this is a temp workaround, needs removal when go cast is implemented properly
                && GetEntry() != SharedConst.WorldTrigger)
                return false;

            // CvC case - can attack each other only when one of them is hostile
            if (!HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) && !target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
                return GetReactionTo(target) <= ReputationRank.Hostile || target.GetReactionTo(this) <= ReputationRank.Hostile;

            // PvP, PvC, CvP case
            // can't attack friendly targets
            if (GetReactionTo(target) > ReputationRank.Neutral || target.GetReactionTo(this) > ReputationRank.Neutral)
                return false;

            Player playerAffectingAttacker = HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) ? GetAffectingPlayer() : null;
            Player playerAffectingTarget = target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) ? target.GetAffectingPlayer() : null;

            // Not all neutral creatures can be attacked (even some unfriendly faction does not react aggresive to you, like Sporaggar)
            if ((playerAffectingAttacker && !playerAffectingTarget) || (!playerAffectingAttacker && playerAffectingTarget))
            {
                if (!(target.IsTypeId(TypeId.Player) && IsTypeId(TypeId.Player)) &&
                    !(target.IsTypeId(TypeId.Unit) && IsTypeId(TypeId.Unit)))
                {
                    Player player = playerAffectingAttacker ? playerAffectingAttacker : playerAffectingTarget;
                    Unit creature = playerAffectingAttacker ? target : this;

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

            Creature creatureAttacker = ToCreature();
            if (creatureAttacker != null && Convert.ToBoolean(creatureAttacker.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.TreatAsRaidUnit))
                return false;

            // check duel - before sanctuary checks
            if (playerAffectingAttacker != null && playerAffectingTarget != null)
                if (playerAffectingAttacker.duel != null && playerAffectingAttacker.duel.opponent == playerAffectingTarget && playerAffectingAttacker.duel.startTime != 0)
                    return true;

            // PvP case - can't attack when attacker or target are in sanctuary
            // however, 13850 client doesn't allow to attack when one of the unit's has sanctuary flag and is pvp
            if (target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable) && HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable)
                && (Convert.ToBoolean(target.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.Sanctuary)
                || Convert.ToBoolean(GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.Sanctuary)))
                return false;

            // additional checks - only PvP case
            if (playerAffectingAttacker != null && playerAffectingTarget != null)
            {
                if (target.IsPvP())
                    return true;

                if (IsFFAPvP() && target.IsFFAPvP())
                    return true;

                return HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.Unk1) || target.HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.Unk1);
            }
            return true;
        }

        bool IsValidAssistTarget(Unit target)
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
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible)) && !CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea(GetMap().GetDifficultyID())))
                return false;

            // can't assist dead
            if ((bySpell == null || !bySpell.IsAllowingDeadTarget()) && !target.IsAlive())
                return false;

            // can't assist untargetable
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable))
                && target.HasFlag(UnitFields.Flags, UnitFlags.NotSelectable))
                return false;

            if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
            {
                if (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
                {
                    if (target.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToPc))
                        return false;
                }
                else
                {
                    if (target.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc))
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
            else if (target.HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
            {
                Player targetPlayerOwner = target.GetAffectingPlayer();
                if (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable))
                {
                    Player selfPlayerOwner = GetAffectingPlayer();
                    if (selfPlayerOwner != null && targetPlayerOwner != null)
                    {
                        // can't assist player which is dueling someone
                        if (selfPlayerOwner != targetPlayerOwner && targetPlayerOwner.duel != null)
                            return false;
                    }
                    // can't assist player in ffa_pvp zone from outside
                    if (Convert.ToBoolean(target.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.FFAPvp)
                        && !Convert.ToBoolean(GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.FFAPvp))
                        return false;
                    // can't assist player out of sanctuary from sanctuary if has pvp enabled
                    if (Convert.ToBoolean(target.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.PvP))
                        if (Convert.ToBoolean(GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.Sanctuary) 
                            && !Convert.ToBoolean(target.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.Sanctuary))
                            return false;
                }
            }
            // PvC case - player can assist creature only if has specific type flags
            else if (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable)
                && (bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
                && !Convert.ToBoolean(target.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag) & (byte)UnitBytes2Flags.PvP))
            {
                Creature creatureTarget = target.ToCreature();
                if (creatureTarget != null)
                    return Convert.ToBoolean(creatureTarget.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.TreatAsRaidUnit)
                        || Convert.ToBoolean(creatureTarget.GetCreatureTemplate().TypeFlags & CreatureTypeFlags.CanAssist);
            }
            return true;
        }

        // Part of Evade mechanics
        public long GetLastDamagedTime() { return _lastDamagedTime; }
        public void SetLastDamagedTime(long val) { _lastDamagedTime = val; }
    }
}
