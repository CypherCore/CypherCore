// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Bosses
{
    [CreatureScript(43778)]
    public class boss_foe_reaper_5000 : BossAI
    {
        public const string MONSTER_START = "A stray jolt from the Foe Reaper has distrupted the foundry controls!";
        public const string MONSTER_SLAG = "The monster slag begins to bubble furiously!";
        public static readonly Position PrototypeSpawn = new Position(-200.499f, -553.946f, 51.2295f, 4.32651f);

        public static readonly Position[] HarvestSpawn =
        {
            new Position(-229.72f, -590.37f, 19.38f, 0.71f),
            new Position(-229.67f, -565.75f, 19.38f, 5.98f),
            new Position(-205.53f, -552.74f, 19.38f, 4.53f),
            new Position(-182.74f, -565.96f, 19.38f, 3.35f)
        };

        public struct eSpell
        {
            public const uint ENERGIZE = 89132;
            public const uint ENERGIZED = 91733; // -> 89200;
            public const uint ON_FIRE = 91737;
            public const uint COSMETIC_STAND = 88906;

            // BOSS spells
            public const uint OVERDRIVE = 88481; // 88484
            public const uint HARVEST = 88495;
            public const uint HARVEST_AURA = 88497;

            public const uint HARVEST_SWEEP = 88521;
            public const uint HARVEST_SWEEP_H = 91718;

            public const uint REAPER_STRIKE = 88490;
            public const uint REAPER_STRIKE_H = 91717;

            public const uint SAFETY_REST_OFFLINE = 88522;
            public const uint SAFETY_REST_OFFLINE_H = 91720;

            public const uint SUMMON_MOLTEN_SLAG = 91839;
        }

        public struct eAchievementMisc
        {
            public const uint ACHIEVEMENT_PROTOTYPE_PRODIGY = 5368;
            public const uint DATA_ACHIV_PROTOTYPE_PRODIGY = 1;
        }

        public struct BossEvents
        {
            public const uint EVENT_NULL = 0;
            public const uint EVENT_START = 1;
            public const uint EVENT_START_2 = 2;
            public const uint EVENT_SRO = 3;
            public const uint EVENT_OVERDRIVE = 4;
            public const uint EVENT_HARVEST = 5;
            public const uint EVENT_HARVEST_SWEAP = 6;
            public const uint EVENT_REAPER_STRIKE = 7;
            public const uint EVENT_SAFETY_OFFLINE = 8;
            public const uint EVENT_SWITCH_TARGET = 9;
            public const uint EVENT_MOLTEN_SLAG = 10;
            public const uint EVENT_START_ATTACK = 11;
        }

        public struct eSays
        {
            public const uint SAY_CAST_OVERDRIVE = 0;
            public const uint SAY_JUSTDIED = 1;
            public const uint SAY_KILLED_UNIT = 2;
            public const uint SAY_EVENT_START = 3;

            public const uint SAY_HARVEST_SWEAP = 4;
            public const uint SAY_CAST_OVERDRIVE_E = 5;
            public const uint SAY_EVENT_SRO = 6;
        }

        public boss_foe_reaper_5000(Creature creature) : base(creature, DMData.DATA_FOEREAPER)
        {
            me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.ImmuneToPc | UnitFlags.Stunned);
        }

        private uint _eventId;
        private uint _step;
        private ObjectGuid _prototypeGUID;

        private bool _below;

        public override void Reset()
        {
            if (!me)
            {
                return;
            }

            _Reset();
            me.SetReactState(ReactStates.Passive);
            me.SetPower(PowerType.Energy, 100);
            me.SetMaxPower(PowerType.Energy, 100);
            me.SetPowerType(PowerType.Energy);
            me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc);
            _step = 0;
            _below = false;

            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);

            me.SetFullHealth();
            me.SetOrientation(4.273f);

            DespawnOldWatchers();
            RespawnWatchers();

            if (IsHeroic())
            {
                Creature Reaper = ObjectAccessor.GetCreature(me, _prototypeGUID);
                if (Reaper != null)
                {
                    Reaper.DespawnOrUnsummon();
                }

                Creature prototype = me.SummonCreature(DMCreatures.NPC_PROTOTYPE_REAPER, PrototypeSpawn, TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
                if (prototype != null)
                {
                    prototype.SetFullHealth();
                    _prototypeGUID = prototype.GetGUID();
                }
            }
        }

        public override void JustEnteredCombat(Unit who)
        {
            base.JustEnteredCombat(who);
            _events.ScheduleEvent(BossEvents.EVENT_REAPER_STRIKE, TimeSpan.FromMilliseconds(10000));
            _events.ScheduleEvent(BossEvents.EVENT_OVERDRIVE, TimeSpan.FromMilliseconds(11000));
            _events.ScheduleEvent(BossEvents.EVENT_HARVEST, TimeSpan.FromMilliseconds(25000));

            if (IsHeroic())
            {
                _events.ScheduleEvent(BossEvents.EVENT_MOLTEN_SLAG, TimeSpan.FromMilliseconds(15000));
            }

            if (!me)
            {
                return;
            }

            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
        }

        public override void JustDied(Unit killer)
        {
            if (!me)
            {
                return;
            }

            base.JustDied(killer);
            DespawnOldWatchers();
            Talk(eSays.SAY_JUSTDIED);
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);

            if (IsHeroic())
            {
                Creature Reaper = ObjectAccessor.GetCreature(me, _prototypeGUID);
                if (Reaper != null)
                {
                    Reaper.DespawnOrUnsummon();
                }
            }
        }

        //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
        //ORIGINAL LINE: uint GetData(uint type) const
        public uint GetData(uint type)
        {
            if (type == (uint)eAchievementMisc.DATA_ACHIV_PROTOTYPE_PRODIGY)
            {
                if (!IsHeroic())
                {
                    return false ? 1 : 0;
                }

                Creature prototype_reaper = ObjectAccessor.GetCreature(me, _prototypeGUID);
                if (prototype_reaper != null)
                {
                    if (prototype_reaper.GetHealth() >= 0.9 * prototype_reaper.GetMaxHealth())
                    {
                        return true ? 1 : 0;
                    }
                }
            }

            return false ? 1 : 0;
        }

        public override void JustReachedHome()
        {
            if (!me)
            {
                return;
            }

            base.JustReachedHome();
            Talk(eSays.SAY_KILLED_UNIT);
            me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.Stunned);
            instance.SetBossState(DMData.DATA_FOEREAPER, EncounterState.Fail);
        }

        public void DespawnOldWatchers()
        {
            var reapers = me.GetCreatureListWithEntryInGrid(47403, 250.0f);

            reapers.Sort(new ObjectDistanceOrderPred(me));
            foreach (var reaper in reapers)
            {
                if (reaper && reaper.GetTypeId() == TypeId.Unit)
                {
                    reaper.DespawnOrUnsummon();
                }
            }
        }

        public void RespawnWatchers()
        {
            for (byte i = 0; i < 4; ++i)
            {
                me.SummonCreature(47403, HarvestSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
            }
        }

        public void SpellHit(Unit UnnamedParameter, SpellInfo spell)
        {
            if (spell == null || !me)
            {
                return;
            }

            if (spell.Id == eSpell.ENERGIZE)
            {
                if (_step == 3)
                {
                    _events.ScheduleEvent(BossEvents.EVENT_START, TimeSpan.FromMilliseconds(100));
                }
                _step++;
            }
        }

        public void MovementInform(uint UnnamedParameter, uint id)
        {
            if (id == 0)
            {
                Creature HarvestTarget = me.FindNearestCreature(DMCreatures.NPC_HARVEST_TARGET, 200.0f, true);
                if (HarvestTarget != null)
                {
                    //DoCast(HarvestTarget, IsHeroic() ? HARVEST_SWEEP_H : HARVEST_SWEEP);
                    me.RemoveAurasDueToSpell(eSpell.HARVEST_AURA);
                    _events.ScheduleEvent(BossEvents.EVENT_START_ATTACK, TimeSpan.FromMilliseconds(1000));
                }
            }
        }

        public void HarvestChase()
        {
            Creature HarvestTarget = me.FindNearestCreature(DMCreatures.NPC_HARVEST_TARGET, 200.0f, true);
            if (HarvestTarget != null)
            {
                me.SetSpeed(UnitMoveType.Run, 3.0f);
                me.GetMotionMaster().MoveCharge(HarvestTarget.GetPositionX(), HarvestTarget.GetPositionY(), HarvestTarget.GetPositionZ(), 5.0f, 0);
                HarvestTarget.DespawnOrUnsummon(TimeSpan.FromMilliseconds(8500));
            }
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!me)
            {
                return;
            }

            DoMeleeAttackIfReady();

            _events.Update(uiDiff);

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0) 
            {
                switch (eventId)
                {
                    case BossEvents.EVENT_START:
                        Talk(eSays.SAY_EVENT_START);
                        me.AddAura(eSpell.ENERGIZED, me);
                        me.TextEmote(MONSTER_START, null, true);
                        _events.ScheduleEvent(BossEvents.EVENT_START_2, TimeSpan.FromMilliseconds(5000));
                        break;

                    case BossEvents.EVENT_START_2:
                        me.TextEmote(MONSTER_SLAG, null, true);
                        me.SetHealth(me.GetMaxHealth());
                        DoZoneInCombat();
                        me.SetReactState(ReactStates.Aggressive);
                        me.RemoveUnitFlag(UnitFlags.NonAttackable);
                        me.RemoveUnitFlag(UnitFlags.ImmuneToPc);
                        me.RemoveUnitFlag(UnitFlags.Stunned);
                        me.RemoveAurasDueToSpell(eSpell.ENERGIZED);
                        _events.ScheduleEvent(BossEvents.EVENT_SRO, TimeSpan.FromMilliseconds(1000));
                        break;

                    case BossEvents.EVENT_SRO:
                        me.RemoveAurasDueToSpell(DMSharedSpells.OFFLINE);

                        Player victim = me.FindNearestPlayer(40.0f);
                        if (victim != null)
                        {
                            me.Attack(victim, false);
                        }
                        break;

                    case BossEvents.EVENT_START_ATTACK:
                        me.RemoveAurasDueToSpell(eSpell.HARVEST_AURA);
                        me.SetSpeed(UnitMoveType.Run, 2.0f);
                        Player victim2 = me.FindNearestPlayer(40.0f);
                        if (victim2 != null)
                        {
                            me.Attack(victim2, true);
                        }
                        break;

                    case BossEvents.EVENT_OVERDRIVE:
                        if (!UpdateVictim())
                        {
                            return;
                        }

                        me.TextEmote("|TInterface\\Icons\\ability_whirlwind.blp:20|tFoe Reaper 5000 begins to activate |cFFFF0000|Hspell:91716|h[Overdrive]|h|r!", null, true);
                        me.AddAura(eSpell.OVERDRIVE, me);
                        me.SetSpeed(UnitMoveType.Run, 4.0f);
                        _events.ScheduleEvent(BossEvents.EVENT_SWITCH_TARGET, TimeSpan.FromMilliseconds(1500));
                        _events.ScheduleEvent(BossEvents.EVENT_OVERDRIVE, TimeSpan.FromMilliseconds(45000));
                        break;

                    case BossEvents.EVENT_SWITCH_TARGET:
                        Unit victim3 = SelectTarget(SelectTargetMethod.Random, 0, 150, true);
                        if (victim3 != null)
                        {
                            me.Attack(victim3, true);
                        }

                        if (me.HasAura(eSpell.OVERDRIVE))
                        {
                            _events.ScheduleEvent(BossEvents.EVENT_SWITCH_TARGET, TimeSpan.FromMilliseconds(1500));
                        }
                        break;

                    case BossEvents.EVENT_HARVEST:
                        if (!UpdateVictim())
                        {
                            return;
                        }

                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);
                        if (target != null)
                        {
                            me.CastSpell(target, eSpell.HARVEST);
                        }

                        _events.RescheduleEvent(BossEvents.EVENT_HARVEST_SWEAP, TimeSpan.FromMilliseconds(5500));
                        break;

                    case BossEvents.EVENT_HARVEST_SWEAP:
                        if (!UpdateVictim())
                        {
                            return;
                        }

                        HarvestChase();
                        Talk(eSays.SAY_HARVEST_SWEAP);
                        _events.ScheduleEvent(BossEvents.EVENT_START_ATTACK, TimeSpan.FromMilliseconds(8000));
                        _events.RescheduleEvent(BossEvents.EVENT_HARVEST, TimeSpan.FromMilliseconds(45000));
                        break;

                    case BossEvents.EVENT_REAPER_STRIKE:
                        if (!UpdateVictim())
                        {
                            return;
                        }

                        Unit victim4 = me.GetVictim();
                        if (victim4 != null)
                        {
                            if (me.IsWithinDist(victim4, 25.0f))
                            {
                                DoCast(victim4, IsHeroic() ? eSpell.REAPER_STRIKE_H : eSpell.REAPER_STRIKE);
                            }
                        }
                        _events.ScheduleEvent(BossEvents.EVENT_REAPER_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(9000, 12000)));
                        break;

                    case BossEvents.EVENT_MOLTEN_SLAG:
                        me.TextEmote(MONSTER_SLAG, null, true);
                        me.CastSpell(-213.21f, -576.85f, 20.97f, eSpell.SUMMON_MOLTEN_SLAG, false);
                        _events.ScheduleEvent(BossEvents.EVENT_MOLTEN_SLAG, TimeSpan.FromMilliseconds(20000));
                        break;

                    case BossEvents.EVENT_SAFETY_OFFLINE:
                        Talk(eSays.SAY_EVENT_SRO);
                        DoCast(me, IsHeroic() ? eSpell.SAFETY_REST_OFFLINE_H : eSpell.SAFETY_REST_OFFLINE);
                        break;
                }

                if (HealthBelowPct(30) && !_below)
                {
                    _events.ScheduleEvent(BossEvents.EVENT_SAFETY_OFFLINE, TimeSpan.FromMilliseconds(0));
                    _below = true;
                }
            }
        }
    }
}
