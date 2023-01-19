// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.CorenDirebrew
{
    struct SpellIds
    {
        public const uint MoleMachineEmerge = 50313;
        public const uint DirebrewDisarmPreCast = 47407;
        public const uint MoleMachineTargetPicker = 47691;
        public const uint MoleMachineMinionSummoner = 47690;
        public const uint DirebrewDisarmGrow = 47409;
        public const uint DirebrewDisarm = 47310;
        public const uint ChuckMug = 50276;
        public const uint PortToCoren = 52850;
        public const uint SendMugControlAura = 47369;
        public const uint SendMugTargetPicker = 47370;
        public const uint SendFirstMug = 47333;
        public const uint SendSecondMug = 47339;
        public const uint RequestSecondMug = 47344;
        public const uint HasDarkBrewmaidensBrew = 47331;
        public const uint BarreledControlAura = 50278;
        public const uint Barreled = 47442;
    }

    struct TextIds
    {
        public const uint SayIntro = 0;
        public const uint SayIntro1 = 1;
        public const uint SayIntro2 = 2;
        public const uint SayInsult = 3;
        public const uint SayAntagonist1 = 0;
        public const uint SayAntagonist2 = 1;
        public const uint SayAntagonistCombat = 2;
    }

    struct ActionIds
    {
        public const int StartFight = -1;
        public const int AntagonistSay1 = -2;
        public const int AntagonistSay2 = -3;
        public const int AntagonistHostile = -4;
    }

    struct CreatureIds
    {
        public const uint IlsaDirebrew = 26764;
        public const uint UrsulaDirebrew = 26822;
        public const uint Antagonist = 23795;
    }

    enum DirebrewPhases
    {
        All = 1,
        Intro,
        One,
        Two,
        Three
    }

    struct MiscConst
    {
        public const uint GossipId = 11388;
        public const uint GoMoleMachineTrap = 188509;
        public const uint GossipOptionFight = 0;
        public const uint GossipOptionApologize = 1;
        public const int DataTargetGuid = 1;
        public const uint MaxAntagonists = 3;

        public static Position[] AntagonistPos =
        {
            new Position(895.3782f, -132.1722f, -49.66423f, 2.6529f),
            new Position(893.9837f, -133.2879f, -49.66541f, 2.583087f),
            new Position(896.2667f, -130.483f,  -49.66249f, 2.600541f)
        };
    }

    [Script]
    class boss_coren_direbrew : BossAI
    {
        DirebrewPhases phase;

        public boss_coren_direbrew(Creature creature) : base(creature, DataTypes.DataCoren) { }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId != MiscConst.GossipId)
                return false;

            if (gossipListId == MiscConst.GossipOptionFight)
            {
                Talk(TextIds.SayInsult, player);
                DoAction(ActionIds.StartFight);
            }
            else if (gossipListId == MiscConst.GossipOptionApologize)
                player.CloseGossipMenu();

            return false;
        }

        public override void Reset()
        {
            _Reset();
            me.SetImmuneToPC(true);
            me.SetFaction((uint)FactionTemplates.Friendly);
            phase = DirebrewPhases.All;
            _scheduler.CancelAll();

            for (byte i = 0; i < MiscConst.MaxAntagonists; ++i)
                me.SummonCreature(CreatureIds.Antagonist, MiscConst.AntagonistPos[i], TempSummonType.DeadDespawn);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            _EnterEvadeMode();
            summons.DespawnAll();
            _DespawnAtEvade(TimeSpan.FromSeconds(10));
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (phase != DirebrewPhases.All || !who.IsPlayer())
                return;

            phase = DirebrewPhases.Intro;
            _scheduler.Schedule(TimeSpan.FromSeconds(6), introTask1 =>
            {
                Talk(TextIds.SayIntro1);
                introTask1.Schedule(TimeSpan.FromSeconds(4), introTask2 =>
                {
                    EntryCheckPredicate pred = new(CreatureIds.Antagonist);
                    summons.DoAction(ActionIds.AntagonistSay1, pred);
                    introTask2.Schedule(TimeSpan.FromSeconds(3), introlTask3 =>
                    {
                        Talk(TextIds.SayIntro2);
                        EntryCheckPredicate pred = new(CreatureIds.Antagonist);
                        summons.DoAction(ActionIds.AntagonistSay2, pred);
                    });
                });
            });
            Talk(TextIds.SayIntro);
        }

        public override void DoAction(int action)
        {
            if (action == ActionIds.StartFight)
            {
                phase = DirebrewPhases.One;
                //events.SetPhase(PhaseOne);
                me.SetImmuneToPC(false);
                me.SetFaction((uint)FactionTemplates.GoblinDarkIronBarPatron);
                DoZoneInCombat();

                EntryCheckPredicate pred = new(CreatureIds.Antagonist);
                summons.DoAction(ActionIds.AntagonistHostile, pred);

                _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
                {
                    CastSpellExtraArgs args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.MaxTargets, 1);
                    me.CastSpell((WorldObject)null, SpellIds.MoleMachineTargetPicker, args);
                    task.Repeat();
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
                {
                    DoCastSelf(SpellIds.DirebrewDisarmPreCast, new CastSpellExtraArgs(true));
                    task.Repeat();
                });
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(66, damage) && phase == DirebrewPhases.One)
            {
                phase = DirebrewPhases.Two;
                SummonSister(CreatureIds.IlsaDirebrew);
            }
            else if (me.HealthBelowPctDamaged(33, damage) && phase == DirebrewPhases.Two)
            {
                phase = DirebrewPhases.Three;
                SummonSister(CreatureIds.UrsulaDirebrew);
            }
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            if (summon.GetEntry() == CreatureIds.IlsaDirebrew)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    SummonSister(CreatureIds.IlsaDirebrew);
                });
            }
            else if (summon.GetEntry() == CreatureIds.UrsulaDirebrew)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    SummonSister(CreatureIds.UrsulaDirebrew);
                });
            }
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();

            var players = me.GetMap().GetPlayers();
            if (!players.Empty())
            {
                Group group = players[0].GetGroup();
                if (group)
                    if (group.IsLFGGroup())
                        Global.LFGMgr.FinishDungeon(group.GetGUID(), 287, me.GetMap());
            }
        }

        void SummonSister(uint entry)
        {
            Creature sister = me.SummonCreature(entry, me.GetPosition(), TempSummonType.DeadDespawn);
            if (sister)
                DoZoneInCombat(sister);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && phase != DirebrewPhases.Intro)
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    class npc_coren_direbrew_sisters : ScriptedAI
    {
        ObjectGuid _targetGUID;

        public npc_coren_direbrew_sisters(Creature creature) : base(creature) { }

        public override void SetGUID(ObjectGuid guid, int id)
        {
            if (id == MiscConst.DataTargetGuid)
                _targetGUID = guid;
        }

        public override ObjectGuid GetGUID(int data)
        {
            if (data == MiscConst.DataTargetGuid)
                return _targetGUID;

            return ObjectGuid.Empty;
        }

        public override void JustEngagedWith(Unit who)
        {
            DoCastSelf(SpellIds.PortToCoren);

            if (me.GetEntry() == CreatureIds.UrsulaDirebrew)
                DoCastSelf(SpellIds.BarreledControlAura);
            else
                DoCastSelf(SpellIds.SendMugControlAura);

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(2), mugChuck =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, false, true, -(int)SpellIds.HasDarkBrewmaidensBrew);
                if (target)
                    DoCast(target, SpellIds.ChuckMug);
                mugChuck.Repeat(TimeSpan.FromSeconds(4));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    class npc_direbrew_minion : ScriptedAI
    {
        InstanceScript _instance;

        public npc_direbrew_minion(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            me.SetFaction((uint)FactionTemplates.GoblinDarkIronBarPatron);
            DoZoneInCombat();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            Creature coren = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.DataCoren));
            if (coren)
                coren.GetAI().JustSummoned(me);
        }
    }

    class npc_direbrew_antagonist : ScriptedAI
    {
        public npc_direbrew_antagonist(Creature creature) : base(creature) { }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case ActionIds.AntagonistSay1:
                    Talk(TextIds.SayAntagonist1);
                    break;
                case ActionIds.AntagonistSay2:
                    Talk(TextIds.SayAntagonist2);
                    break;
                case ActionIds.AntagonistHostile:
                    me.SetImmuneToPC(false);
                    me.SetFaction((uint)FactionTemplates.GoblinDarkIronBarPatron);
                    DoZoneInCombat();
                    break;
                default:
                    break;
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAntagonistCombat, who);
            base.JustEngagedWith(who);
        }
    }

    class go_direbrew_mole_machine : GameObjectAI
    {
        public go_direbrew_mole_machine(GameObject go) : base(go) { }

        public override void Reset()
        {
            me.SetLootState(LootState.Ready);
            _scheduler.Schedule(TimeSpan.FromSeconds(1), context =>
            {
                me.UseDoorOrButton(10000);
                me.CastSpell(null, SpellIds.MoleMachineEmerge, true);
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), context =>
            {
                GameObject trap = me.GetLinkedTrap();
                if (trap)
                {
                    trap.SetLootState(LootState.Activated);
                    trap.UseDoorOrButton();
                }
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    // 47691 - Summon Mole Machine Target Picker
    class spell_direbrew_summon_mole_machine_target_picker : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MoleMachineMinionSummoner);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MoleMachineMinionSummoner, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 47370 - Send Mug Target Picker
    class spell_send_mug_target_picker : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();

            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HasDarkBrewmaidensBrew));

            if (targets.Count > 1)
            {
                targets.RemoveAll(obj =>
            {
                if (obj.GetGUID() == caster.GetAI().GetGUID(MiscConst.DataTargetGuid))
                    return true;
                return false;
            });
            }

            if (targets.Empty())
                return;

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.GetAI().SetGUID(GetHitUnit().GetGUID(), MiscConst.DataTargetGuid);
            caster.CastSpell(GetHitUnit(), SpellIds.SendFirstMug, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 47344 - Request Second Mug
    class spell_request_second_mug : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SendSecondMug);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), SpellIds.SendSecondMug, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 47369 - Send Mug Control Aura
    class spell_send_mug_control_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SendMugTargetPicker);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SendMugTargetPicker, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
        }
    }

    // 50278 - Barreled Control Aura
    class spell_barreled_control_aura : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(null, SpellIds.Barreled, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // 47407 - Direbrew's Disarm (precast)
    class spell_direbrew_disarm : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DirebrewDisarm, SpellIds.DirebrewDisarmGrow);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            Aura aura = GetTarget().GetAura(SpellIds.DirebrewDisarmGrow);
            if (aura != null)
            {
                aura.SetStackAmount((byte)(aura.GetStackAmount() + 1));
                aura.SetDuration(aura.GetDuration() - 1500);
            }
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.DirebrewDisarmGrow, true);
            GetTarget().CastSpell(GetTarget(), SpellIds.DirebrewDisarm);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicDummy));
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }
    }
}

