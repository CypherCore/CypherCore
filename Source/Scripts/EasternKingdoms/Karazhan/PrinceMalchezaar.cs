// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.PrinceMalchezaar
{
    internal struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayAxeToss1 = 1;

        public const uint SayAxeToss2 = 2;

        //public const uint SaySpecial1                = 3; Not used, needs to be implemented, but I don't know where it should be used.
        //public const uint SaySpecial2                = 4; Not used, needs to be implemented, but I don't know where it should be used.
        //public const uint SaySpecial3                = 5; Not used, needs to be implemented, but I don't know where it should be used.
        public const uint SaySlay = 6;
        public const uint SaySummon = 7;
        public const uint SayDeath = 8;
    }

    internal struct SpellIds
    {
        public const uint Enfeeble = 30843; //Enfeeble during phase 1 and 2
        public const uint EnfeebleEffect = 41624;

        public const uint Shadownova = 30852;    //Shadownova used during all phases
        public const uint SwPain = 30854;        //Shadow word pain during phase 1 and 3 (different targeting rules though)
        public const uint ThrashPassive = 12787; //Extra attack chance during phase 2
        public const uint SunderArmor = 30901;   //Sunder armor during phase 2
        public const uint ThrashAura = 12787;    //Passive proc chance for thrash
        public const uint EquipAxes = 30857;     //Visual for axe equiping
        public const uint AmplifyDamage = 39095; //Amplifiy during phase 3
        public const uint Cleave = 30131;        //Same as Nightbane.
        public const uint Hellfire = 30859;      //Infenals' hellfire aura

        public const uint InfernalRelay = 30834;
    }

    internal struct MiscConst
    {
        public const uint TotalInfernalPoints = 18;
        public const uint NetherspiteInfernal = 17646; //The netherspite infernal creature
        public const uint MalchezarsAxe = 17650;       //Malchezar's axes (creatures), summoned during phase 3

        public const uint InfernalModelInvisible = 11686; //Infernal Effects
        public const int EquipIdAxe = 33542;              //Axes info
    }

    [Script]
    internal class netherspite_infernal : ScriptedAI
    {
        public ObjectGuid Malchezaar;
        public Vector2 Point;

        public netherspite_infernal(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
        }

        public override void JustEngagedWith(Unit who)
        {
        }

        public override void MoveInLineOfSight(Unit who)
        {
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void KilledUnit(Unit who)
        {
            Unit unit = Global.ObjAccessor.GetUnit(me, Malchezaar);

            if (unit)
            {
                Creature creature = unit.ToCreature();

                if (creature)
                    creature.GetAI().KilledUnit(who);
            }
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.InfernalRelay)
            {
                me.SetDisplayId(me.GetNativeDisplayId());
                me.SetUnitFlag(UnitFlags.Uninteractible);

                _scheduler.Schedule(TimeSpan.FromSeconds(4), task => DoCast(me, SpellIds.Hellfire));

                _scheduler.Schedule(TimeSpan.FromSeconds(170),
                                    task =>
                                    {
                                        Creature pMalchezaar = ObjectAccessor.GetCreature(me, Malchezaar);

                                        if (pMalchezaar && pMalchezaar.IsAlive())
                                            pMalchezaar.GetAI<boss_malchezaar>().Cleanup(me, Point);
                                    });
            }
        }

        public override void DamageTaken(Unit done_by, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!done_by ||
                done_by.GetGUID() != Malchezaar)
                damage = 0;
        }
    }

    [Script]
    internal class boss_malchezaar : ScriptedAI
    {
        private static readonly Vector2[] InfernalPoints =
        {
            new(-10922.8f, -1985.2f), new(-10916.2f, -1996.2f), new(-10932.2f, -2008.1f), new(-10948.8f, -2022.1f), new(-10958.7f, -1997.7f), new(-10971.5f, -1997.5f), new(-10990.8f, -1995.1f), new(-10989.8f, -1976.5f), new(-10971.6f, -1973.0f), new(-10955.5f, -1974.0f), new(-10939.6f, -1969.8f), new(-10958.0f, -1952.2f), new(-10941.7f, -1954.8f), new(-10943.1f, -1988.5f), new(-10948.8f, -2005.1f), new(-10984.0f, -2019.3f), new(-10932.8f, -1979.6f), new(-10935.7f, -1996.0f)
        };

        private readonly ObjectGuid[] axes = new ObjectGuid[2];
        private readonly long[] enfeeble_health = new long[5];
        private readonly ObjectGuid[] enfeeble_targets = new ObjectGuid[5];

        private readonly List<ObjectGuid> infernals = new();

        private readonly InstanceScript instance;
        private readonly List<Vector2> positions = new();

        private uint AmplifyDamageTimer;
        private uint AxesTargetSwitchTimer;
        private uint Cleave_Timer;
        private uint EnfeebleResetTimer;
        private uint EnfeebleTimer;
        private uint InfernalCleanupTimer;
        private uint InfernalTimer;

        private uint phase;
        private uint ShadowNovaTimer;
        private uint SunderArmorTimer;
        private uint SWPainTimer;

        public boss_malchezaar(Creature creature) : base(creature)
        {
            Initialize();

            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            AxesCleanup();
            ClearWeapons();
            InfernalCleanup();
            positions.Clear();

            Initialize();

            for (byte i = 0; i < MiscConst.TotalInfernalPoints; ++i)
                positions.Add(InfernalPoints[i]);

            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoNetherDoor), true);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            AxesCleanup();
            ClearWeapons();
            InfernalCleanup();
            positions.Clear();

            for (byte i = 0; i < MiscConst.TotalInfernalPoints; ++i)
                positions.Add(InfernalPoints[i]);

            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoNetherDoor), true);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);

            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoNetherDoor), false); // Open the door leading further in
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (EnfeebleResetTimer != 0 &&
                EnfeebleResetTimer <= diff) // Let's not forget to reset that
            {
                EnfeebleResetHealth();
                EnfeebleResetTimer = 0;
            }
            else
            {
                EnfeebleResetTimer -= diff;
            }

            if (me.HasUnitState(UnitState.Stunned)) // While shifting to phase 2 malchezaar stuns himself
                return;

            if (me.GetVictim() &&
                me.GetTarget() != me.GetVictim().GetGUID())
                me.SetTarget(me.GetVictim().GetGUID());

            if (phase == 1)
            {
                if (HealthBelowPct(60))
                {
                    me.InterruptNonMeleeSpells(false);

                    phase = 2;

                    //animation
                    DoCast(me, SpellIds.EquipAxes);

                    //text
                    Talk(TextIds.SayAxeToss1);

                    //passive thrash aura
                    DoCast(me, SpellIds.ThrashAura, new CastSpellExtraArgs(true));

                    //models
                    SetEquipmentSlots(false, MiscConst.EquipIdAxe, MiscConst.EquipIdAxe);

                    me.SetBaseAttackTime(WeaponAttackType.OffAttack, (me.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 150) / 100);
                    me.SetCanDualWield(true);
                }
            }
            else if (phase == 2)
            {
                if (HealthBelowPct(30))
                {
                    InfernalTimer = 15000;

                    phase = 3;

                    ClearWeapons();

                    //remove thrash
                    me.RemoveAura(SpellIds.ThrashAura);

                    Talk(TextIds.SayAxeToss2);

                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                    for (byte i = 0; i < 2; ++i)
                    {
                        Creature axe = me.SummonCreature(MiscConst.MalchezarsAxe, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), 0, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));

                        if (axe)
                        {
                            axe.SetUnitFlag(UnitFlags.Uninteractible);
                            axe.SetFaction(me.GetFaction());
                            axes[i] = axe.GetGUID();

                            if (target)
                            {
                                axe.GetAI().AttackStart(target);
                                AddThreat(target, 10000000.0f, axe);
                            }
                        }
                    }

                    if (ShadowNovaTimer > 35000)
                        ShadowNovaTimer = EnfeebleTimer + 5000;

                    return;
                }

                if (SunderArmorTimer <= diff)
                {
                    DoCastVictim(SpellIds.SunderArmor);
                    SunderArmorTimer = RandomHelper.URand(10000, 18000);
                }
                else
                {
                    SunderArmorTimer -= diff;
                }

                if (Cleave_Timer <= diff)
                {
                    DoCastVictim(SpellIds.Cleave);
                    Cleave_Timer = RandomHelper.URand(6000, 12000);
                }
                else
                {
                    Cleave_Timer -= diff;
                }
            }
            else
            {
                if (AxesTargetSwitchTimer <= diff)
                {
                    AxesTargetSwitchTimer = RandomHelper.URand(7500, 20000);

                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                    if (target)
                        for (byte i = 0; i < 2; ++i)
                        {
                            Unit axe = Global.ObjAccessor.GetUnit(me, axes[i]);

                            if (axe)
                            {
                                if (axe.GetVictim())
                                    ResetThreat(axe.GetVictim(), axe);

                                AddThreat(target, 1000000.0f, axe);
                            }
                        }
                }
                else
                {
                    AxesTargetSwitchTimer -= diff;
                }

                if (AmplifyDamageTimer <= diff)
                {
                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                    if (target)
                        DoCast(target, SpellIds.AmplifyDamage);

                    AmplifyDamageTimer = RandomHelper.URand(20000, 30000);
                }
                else
                {
                    AmplifyDamageTimer -= diff;
                }
            }

            //Time for global and double timers
            if (InfernalTimer <= diff)
            {
                SummonInfernal(diff);
                InfernalTimer = phase == 3 ? 14500 : 44500u; // 15 secs in phase 3, 45 otherwise
            }
            else
            {
                InfernalTimer -= diff;
            }

            if (ShadowNovaTimer <= diff)
            {
                DoCastVictim(SpellIds.Shadownova);
                ShadowNovaTimer = phase == 3 ? 31000 : uint.MaxValue;
            }
            else
            {
                ShadowNovaTimer -= diff;
            }

            if (phase != 2)
            {
                if (SWPainTimer <= diff)
                {
                    Unit target;

                    if (phase == 1)
                        target = me.GetVictim(); // the tank
                    else                         // anyone but the tank
                        target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);

                    if (target)
                        DoCast(target, SpellIds.SwPain);

                    SWPainTimer = 20000;
                }
                else
                {
                    SWPainTimer -= diff;
                }
            }

            if (phase != 3)
            {
                if (EnfeebleTimer <= diff)
                {
                    EnfeebleHealthEffect();
                    EnfeebleTimer = 30000;
                    ShadowNovaTimer = 5000;
                    EnfeebleResetTimer = 9000;
                }
                else
                {
                    EnfeebleTimer -= diff;
                }
            }

            if (phase == 2)
                DoMeleeAttacksIfReady();
            else
                DoMeleeAttackIfReady();
        }

        public void Cleanup(Creature infernal, Vector2 point)
        {
            foreach (var guid in infernals)
                if (guid == infernal.GetGUID())
                {
                    infernals.Remove(guid);

                    break;
                }

            positions.Add(point);
        }

        private void Initialize()
        {
            EnfeebleTimer = 30000;
            EnfeebleResetTimer = 38000;
            ShadowNovaTimer = 35500;
            SWPainTimer = 20000;
            AmplifyDamageTimer = 5000;
            Cleave_Timer = 8000;
            InfernalTimer = 40000;
            InfernalCleanupTimer = 47000;
            AxesTargetSwitchTimer = RandomHelper.URand(7500, 20000);
            SunderArmorTimer = RandomHelper.URand(5000, 10000);
            phase = 1;

            for (byte i = 0; i < 5; ++i)
            {
                enfeeble_targets[i].Clear();
                enfeeble_health[i] = 0;
            }
        }

        private void InfernalCleanup()
        {
            //Infernal Cleanup
            foreach (var guid in infernals)
            {
                Unit pInfernal = Global.ObjAccessor.GetUnit(me, guid);

                if (pInfernal && pInfernal.IsAlive())
                {
                    pInfernal.SetVisible(false);
                    pInfernal.SetDeathState(DeathState.JustDied);
                }
            }

            infernals.Clear();
        }

        private void AxesCleanup()
        {
            for (byte i = 0; i < 2; ++i)
            {
                Unit axe = Global.ObjAccessor.GetUnit(me, axes[i]);

                if (axe && axe.IsAlive())
                    axe.KillSelf();

                axes[i].Clear();
            }
        }

        private void ClearWeapons()
        {
            SetEquipmentSlots(false, 0, 0);
            me.SetCanDualWield(false);
        }

        private void EnfeebleHealthEffect()
        {
            SpellInfo info = Global.SpellMgr.GetSpellInfo(SpellIds.EnfeebleEffect, GetDifficulty());

            if (info == null)
                return;

            Unit tank = me.GetThreatManager().GetCurrentVictim();
            List<Unit> targets = new();

            foreach (var refe in me.GetThreatManager().GetSortedThreatList())
            {
                Unit target = refe.GetVictim();

                if (target != tank &&
                    target.IsAlive() &&
                    target.IsPlayer())
                    targets.Add(target);
            }

            if (targets.Empty())
                return;

            //cut down to size if we have more than 5 targets
            targets.RandomResize(5);

            uint i = 0;

            foreach (var target in targets)
            {
                if (target)
                {
                    enfeeble_targets[i] = target.GetGUID();
                    enfeeble_health[i] = target.GetHealth();

                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.OriginalCaster = me.GetGUID();
                    target.CastSpell(target, SpellIds.Enfeeble, args);
                    target.SetHealth(1);
                }

                i++;
            }
        }

        private void EnfeebleResetHealth()
        {
            for (byte i = 0; i < 5; ++i)
            {
                Unit target = Global.ObjAccessor.GetUnit(me, enfeeble_targets[i]);

                if (target && target.IsAlive())
                    target.SetHealth(enfeeble_health[i]);

                enfeeble_targets[i].Clear();
                enfeeble_health[i] = 0;
            }
        }

        private void SummonInfernal(uint diff)
        {
            Vector2 point = Vector2.Zero;
            Position pos = null;

            if ((me.GetMapId() != 532) ||
                positions.Empty())
            {
                pos = me.GetRandomNearPosition(60);
            }
            else
            {
                point = positions.SelectRandom();
                pos.Relocate(point.X, point.Y, 275.5f, RandomHelper.FRand(0.0f, (MathF.PI * 2)));
            }

            Creature infernal = me.SummonCreature(MiscConst.NetherspiteInfernal, pos, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(3));

            if (infernal)
            {
                infernal.SetDisplayId(MiscConst.InfernalModelInvisible);
                infernal.SetFaction(me.GetFaction());

                if (point != Vector2.Zero)
                    infernal.GetAI<netherspite_infernal>().Point = point;

                infernal.GetAI<netherspite_infernal>().Malchezaar = me.GetGUID();

                infernals.Add(infernal.GetGUID());
                DoCast(infernal, SpellIds.InfernalRelay);
            }

            Talk(TextIds.SaySummon);
        }

        private void DoMeleeAttacksIfReady()
        {
            if (me.IsWithinMeleeRange(me.GetVictim()) &&
                !me.IsNonMeleeSpellCast(false))
            {
                //Check for base attack
                if (me.IsAttackReady() &&
                    me.GetVictim())
                {
                    me.AttackerStateUpdate(me.GetVictim());
                    me.ResetAttackTimer();
                }

                //Check for offhand attack
                if (me.IsAttackReady(WeaponAttackType.OffAttack) &&
                    me.GetVictim())
                {
                    me.AttackerStateUpdate(me.GetVictim(), WeaponAttackType.OffAttack);
                    me.ResetAttackTimer(WeaponAttackType.OffAttack);
                }
            }
        }
    }
}