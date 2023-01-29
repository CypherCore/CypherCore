using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [Script]
        // Doomguard - 11859, Terrorguard - 59000
        public class npc_warlock_doomguard : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warlock_doomguard() : base("npc_warlock_doomguard")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_doomguardAI(creature);
            }

            public class npc_warlock_doomguardAI : ScriptedAI
            {
                public EventMap events = new();
                public float maxDistance;

                public npc_warlock_doomguardAI(Creature creature) : base(creature)
                {
                }

                public override void Reset()
                {
                    me.SetClass(Class.Rogue);
                    me.SetPowerType(PowerType.Energy);
                    me.SetMaxPower(PowerType.Energy, 200);
                    me.SetPower(PowerType.Energy, 200);

                    events.Reset();
                    events.ScheduleEvent(1, TimeSpan.FromSeconds(3));

                    me.SetControlled(true, UnitState.Root);
                    maxDistance = SpellManager.Instance.GetSpellInfo(SpellIds.PET_DOOMBOLT, Difficulty.None).RangeEntry.RangeMax[0];
                }

                public override void UpdateAI(uint diff)
                {
                    UpdateVictim();
                    Unit owner = me.GetOwner();

                    if (me.GetOwner())
                    {
                        Unit victim = owner.GetVictim();

                        if (owner.GetVictim())
                            me.Attack(victim, false);
                    }

                    events.Update(diff);

                    uint eventId = events.ExecuteEvent();

                    while (eventId != 0)
                    {
                        switch (eventId)
                        {
                            case 1:
                                if (!me.GetVictim())
                                {
                                    me.SetControlled(false, UnitState.Root);
                                    events.ScheduleEvent(eventId, TimeSpan.FromSeconds(1));

                                    return;
                                }

                                me.SetControlled(true, UnitState.Root);
                                me.CastSpell(me.GetVictim(), SpellIds.PET_DOOMBOLT, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(me.GetOwnerGUID()));
                                events.ScheduleEvent(eventId, TimeSpan.FromSeconds(3));

                                break;
                        }

                        eventId = events.ExecuteEvent();
                    }
                }
            }
        }

        [Script]
        public class npc_warl_demonic_gateway : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warl_demonic_gateway() : base("npc_warl_demonic_gateway")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warl_demonic_gatewayAI(creature);
            }

            public class npc_warl_demonic_gatewayAI : CreatureAI
            {
                public EventMap events = new();
                public bool firstTick = true;

                public npc_warl_demonic_gatewayAI(Creature creature) : base(creature)
                {
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (firstTick)
                    {
                        me.CastSpell(me, SpellIds.DEMONIC_GATEWAY_VISUAL, true);

                        //todo me->SetInteractSpellId(SPELL_WARLOCK_DEMONIC_GATEWAY_ACTIVATE);
                        me.SetUnitFlag(UnitFlags.NonAttackable);
                        me.SetNpcFlag(NPCFlags.SpellClick);
                        me.SetReactState(ReactStates.Passive);
                        me.SetControlled(true, UnitState.Root);

                        firstTick = false;
                    }
                }

                public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
                {
                    if (clicker.IsPlayer())
                    {
                        // don't allow using the gateway while having specific Auras
                        uint[] aurasToCheck =
                        {
                            121164, 121175, 121176, 121177
                        }; // Orbs of Power @ Temple of Kotmogu

                        foreach (var auraToCheck in aurasToCheck)
                            if (clicker.HasAura(auraToCheck))
                                return;

                        TeleportTarget(clicker, true);
                    }

                    return;
                }

                public void TeleportTarget(Unit target, bool allowAnywhere)
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    // only if Target stepped through the portal
                    if (!allowAnywhere &&
                        me.GetDistance2d(target) > 1.0f)
                        return;

                    // check if Target wasn't recently teleported
                    if (target.HasAura(SpellIds.DEMONIC_GATEWAY_DEBUFF))
                        return;

                    // only if in same party
                    if (!target.IsInRaidWith(owner))
                        return;

                    // not allowed while CC'ed
                    if (!target.CanFreeMove())
                        return;

                    uint otherGateway = me.GetEntry() == SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE : SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN;
                    uint teleportSpell = me.GetEntry() == SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? SpellIds.DEMONIC_GATEWAY_JUMP_GREEN : SpellIds.DEMONIC_GATEWAY_JUMP_PURPLE;

                    var gateways = me.GetCreatureListWithEntryInGrid(otherGateway, 100.0f);

                    foreach (var gateway in gateways)
                    {
                        if (gateway.GetOwnerGUID() != me.GetOwnerGUID())
                            continue;

                        target.CastSpell(gateway, teleportSpell, true);

                        if (target.HasAura(SpellIds.PLANESWALKER))
                            target.CastSpell(target, SpellIds.PLANESWALKER_BUFF, true);

                        // Item - Warlock PvP Set 4P Bonus: "Your allies can use your Demonic Gateway again 15 sec sooner"
                        int amount = owner.GetAuraEffect(SpellIds.PVP_4P_BONUS, 0).GetAmount();

                        if (amount > 0)
                        {
                            Aura aura = target.GetAura(SpellIds.DEMONIC_GATEWAY_DEBUFF);

                            aura?.SetDuration(aura.GetDuration() - amount * Time.InMilliseconds);
                        }

                        break;
                    }
                }
            }
        }

        // Dreadstalker - 98035
        [Script]
        public class npc_warlock_dreadstalker : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warlock_dreadstalker() : base("npc_warlock_dreadstalker")
            {
            }

            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_dreadstalkerAI(creature);
            }

            public class npc_warlock_dreadstalkerAI : ScriptedAI
            {
                public bool firstTick = true;

                public npc_warlock_dreadstalkerAI(Creature creature) : base(creature)
                {
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (firstTick)
                    {
                        Unit owner = me.GetOwner();

                        if (!me.GetOwner() ||
                            !me.GetOwner().ToPlayer())
                            return;

                        me.SetMaxHealth(owner.CountPctFromMaxHealth(40));
                        me.SetHealth(me.GetMaxHealth());

                        Unit target = owner.ToPlayer().GetSelectedUnit();

                        if (owner.ToPlayer().GetSelectedUnit())
                            me.CastSpell(target, SpellIds.DREADSTALKER_CHARGE, true);

                        firstTick = false;

                        //me->CastSpell(SPELL_WARLOCK_SHARPENED_DREADFANGS_BUFF, SPELLVALUE_BASE_POINT0, owner->GetAuraEffectAmount(SPELL_WARLOCK_SHARPENED_DREADFANGS, EFFECT_0), me, true);
                    }

                    UpdateVictim();
                    DoMeleeAttackIfReady();
                }
            }
        }


        [Script]
        // Darkglare - 103673
        public class npc_pet_warlock_darkglare : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_pet_warlock_darkglare() : base("npc_pet_warlock_darkglare")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const override
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_pet_warlock_darkglare_PetAI(creature);
            }

            public class npc_pet_warlock_darkglare_PetAI : PetAI
            {
                public npc_pet_warlock_darkglare_PetAI(Creature creature) : base(creature)
                {
                }

                public void UpdateAI(uint UnnamedParameter)
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    var target = me.GetAttackerForHelper();

                    if (target != null)
                    {
                        target.RemoveAura(SpellIds.DOOM, owner.GetGUID());
                        me.CastSpell(target, SpellIds.EYE_LASER, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(owner.GetGUID()));
                    }
                }
            }
        }

        [Script]
        public class npc_warlock_infernal : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warlock_infernal() : base("npc_warlock_infernal")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_infernalAI(creature);
            }

            public class npc_warlock_infernalAI : ScriptedAI
            {
                public Position spawnPos = new();

                public npc_warlock_infernalAI(Creature c) : base(c)
                {
                }

                public override void Reset()
                {
                    spawnPos = me.GetPosition();

                    // if we leave default State (ASSIST) it will passively be controlled by warlock
                    me.SetReactState(ReactStates.Passive);

                    // melee Damage
                    Unit owner = me.GetOwner();

                    if (me.GetOwner())
                    {
                        Player player = owner.ToPlayer();

                        if (owner.ToPlayer())
                        {
                            bool isLordSummon = me.GetEntry() == 108452;

                            int spellPower = player.SpellBaseDamageBonusDone(SpellSchoolMask.Fire);
                            int dmg = MathFunctions.CalculatePct(spellPower, isLordSummon ? 30 : 50);
                            int diff = MathFunctions.CalculatePct(dmg, 10);

                            me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, dmg - diff);
                            me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, dmg + diff);


                            if (isLordSummon)
                                return;

                            if (player.HasAura(SpellIds.LORD_OF_THE_FLAMES) &&
                                !player.HasAura(SpellIds.LORD_OF_THE_FLAMES_CD))
                            {
                                List<float> angleOffsets = new()
                                                           {
                                                               (float)Math.PI / 2.0f,
                                                               (float)Math.PI,
                                                               3.0f * (float)Math.PI / 2.0f
                                                           };

                                for (uint i = 0; i < 3; ++i)
                                    player.CastSpell(me, SpellIds.LORD_OF_THE_FLAMES_SUMMON, true);

                                player.CastSpell(player, SpellIds.LORD_OF_THE_FLAMES_CD, true);
                            }
                        }
                    }
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (!me.HasAura(SpellIds.IMMOLATION))
                        DoCast(SpellIds.IMMOLATION);

                    // "The Infernal deals strong area of effect Damage, and will be drawn to attack targets near the impact point"
                    if (!me.GetVictim())
                    {
                        Unit preferredTarget = me.GetAttackerForHelper();

                        if (preferredTarget != null)
                            me.GetAI().AttackStart(preferredTarget);
                    }

                    DoMeleeAttackIfReady();
                }
            }
        }

        // 107024 - Fel Lord
        [Script]
        public class npc_warl_fel_lord : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warl_fel_lord() : base("npc_warl_fel_lord")
            {
            }

            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warl_fel_lordAI(creature);
            }

            public class npc_warl_fel_lordAI : CreatureAI
            {
                public npc_warl_fel_lordAI(Creature creature) : base(creature)
                {
                }

                public override void Reset()
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    me.SetMaxHealth(owner.GetMaxHealth());
                    me.SetHealth(me.GetMaxHealth());
                    me.SetControlled(true, UnitState.Root);
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (me.HasUnitState(UnitState.Casting))
                        return;

                    me.CastSpell(me, SpellIds.FEL_LORD_CLEAVE, false);
                }
            }
        }


        // Wild Imp - 99739
        [Script]
        public class npc_pet_warlock_wild_imp : PetAI
        {
            private ObjectGuid _targetGUID = new();

            public npc_pet_warlock_wild_imp(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();

                if (me.GetOwner())
                {
                    me.SetLevel(owner.GetLevel());
                    me.SetMaxHealth(owner.GetMaxHealth() / 3);
                    me.SetHealth(owner.GetHealth() / 3);
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                Unit target = GetTarget();
                ObjectGuid newtargetGUID = owner.GetTarget();

                if (newtargetGUID.IsEmpty() ||
                    newtargetGUID == _targetGUID)
                {
                    CastSpellOnTarget(owner, target);

                    return;
                }

                Unit newTarget = ObjectAccessor.Instance.GetUnit(me, newtargetGUID);

                if (ObjectAccessor.Instance.GetUnit(me, newtargetGUID))
                    if (target != newTarget &&
                        me.IsValidAttackTarget(newTarget))
                        target = newTarget;

                CastSpellOnTarget(owner, target);
            }

            private Unit GetTarget()
            {
                return ObjectAccessor.Instance.GetUnit(me, _targetGUID);
            }

            private void CastSpellOnTarget(Unit owner, Unit target)
            {
                if (target != null &&
                    me.IsValidAttackTarget(target) &&
                    !me.HasUnitState(UnitState.Casting) &&
                    !me.VariableStorage.GetValue("controlled", false))
                {
                    _targetGUID = target.GetGUID();
                    var result = me.CastSpell(target, SpellIds.FEL_FIREBOLT, new CastSpellExtraArgs(TriggerCastFlags.IgnorePowerAndReagentCost).SetOriginalCaster(owner.GetGUID()));
                }
            }
        }
    }
}