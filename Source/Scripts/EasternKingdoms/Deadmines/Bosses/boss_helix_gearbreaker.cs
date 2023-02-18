// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;


using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Bosses
{
    [CreatureScript(47296)]
    public class boss_helix_gearbreaker : BossAI
    {
        public const string CHEST_BOMB = "Helix attaches a bomb to $N's chest.";

        public static readonly Position[] OafPos =
        {
            new Position(-289.809f, -527.215f, 49.8021f, 0),
            new Position(-289.587f, -489.575f, 49.9126f, 0)
        };

        public static readonly Position[] CrewSpawn =
        {
            new Position(-281.68f, -504.10f, 60.51f, 4.75f),
            new Position(-284.71f, -504.13f, 60.42f, 4.72f),
            new Position(-288.65f, -503.74f, 60.38f, 4.64f),
            new Position(-293.88f, -503.90f, 60.07f, 4.77f)
        };

        public boss_helix_gearbreaker(Creature pCreature) : base(pCreature, DMData.DATA_HELIX)
        {
            
        }

        public struct eSpels
        {
            // Helix
            public const uint OAFQUARD = 90546;
            public const uint HELIX_RIDE = 88337;
            public const uint THROW_BOMB = 88264;

            // Oaf spell
            public const uint OAF_GRAB_TARGETING = 88289;
            public const uint RIDE_OAF = 88278; // 88277;
            public const uint RIDE_VEHICLE_HARDCODED = 46598;
            public const uint OAF_CHARGE = 88288;
            public const uint OAF_SMASH = 88300;
            public const uint OAF_SMASH_H = 91568;

            // BOMB
            public const uint STICKY_BOMB_EXPLODE = 95500; //88329; // 95500 -> 88321; 88974
            public const uint STICKY_BOMB_EXPLODE_H = 91566;
            public const uint ARMING_VISUAL_YELLOW = 88315;
            public const uint ARMING_VISUAL_ORANGE = 88316;
            public const uint ARMING_VISUAL_RED = 88317;
            public const uint BOMB_ARMED_STATE = 88319;
            public const uint CHEST_BOMB = 88352; // Unused
        }

        public struct HelOaf_Events
        {
            // Helix Events
            public const uint EVENT_CHEST_BOMB = 1;
            public const uint EVENT_THROW_BOMB = 2;
            public const uint EVENT_NO_OAF = 3;
            public const uint EVENT_ACHIEVEVEMENT_BUFF = 4;

            // Oaf Events
            public const uint EVENT_OAFQUARD = 5;
            public const uint EVENT_MOVE_TO_POINT = 6;
            public const uint EVENT_MOUNT_PLAYER = 7;
            public const uint EVEMT_CHARGE = 8;
            public const uint EVENT_FINISH = 9;
        }

        private uint _phase;
        private uint _uiTimer;
        private uint _numberKillMineRat;
        private Creature _oaf;

        public override void Reset()
        {
            _Reset();
            _phase = 1;
            _uiTimer = 2000;
            _numberKillMineRat = 0;

            if (!me)
            {
                return;
            }

            base.instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            me.SetReactState(ReactStates.Aggressive);
            me.SetUnitFlag(UnitFlags.Uninteractible);
            base.summons.DespawnAll();
            OafSupport();
        }

        public override void JustEnteredCombat(Unit who)
        {
            if (!me)
            {
                return;
            }

            base.JustEnteredCombat(who);
            Talk(5);
            me.SetInCombatWithZone();
            base.instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            _events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));

            if (IsHeroic())
            {
                SummonCrew();
                _events.ScheduleEvent(HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF, TimeSpan.FromMilliseconds(0));
            }
        }

        public void OafSupport()
        {
            _oaf = me.GetVehicleCreatureBase();
            if (_oaf == null)
            {
                _oaf = me.FindNearestCreature(DMCreatures.NPC_OAF, 30.0f);
                if (_oaf != null && _oaf.IsAlive())
                {
                    me.CastSpell(_oaf, eSpels.RIDE_VEHICLE_HARDCODED);
                }
                else
                {
                    _oaf = me.SummonCreature(DMCreatures.NPC_OAF, me.GetHomePosition());

                    if (_oaf != null && _oaf.IsAlive())
                    {
                        me.CastSpell(_oaf, eSpels.RIDE_VEHICLE_HARDCODED);
                    }
                }
            }
        }

        public override void JustSummoned(Creature summoned)
        {
            base.summons.Summon(summoned);
        }

        public void SummonCrew()
        {
            for (byte i = 0; i < 4; ++i)
            {
                me.SummonCreature(DMCreatures.NPC_HELIX_CREW, CrewSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
            }
        }

        public override void JustDied(Unit killer)
        {
            if (!me)
            {
                return;
            }

            base.JustDied(killer);
            base.instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            Talk(0);
            base.summons.DespawnAll();
        }

        public override void JustReachedHome()
        {
            if (!me)
            {
                return;
            }

            base.JustReachedHome();
            Talk(1);
            instance.SetBossState(DMData.DATA_HELIX, EncounterState.Fail);
        }

        public void OafDead()
        {
            _events.ScheduleEvent(HelOaf_Events.EVENT_NO_OAF, TimeSpan.FromMilliseconds(100));
            _events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));
            if (IsHeroic())
            {
                _events.ScheduleEvent(HelOaf_Events.EVENT_CHEST_BOMB, TimeSpan.FromMilliseconds(5000));
            }
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!UpdateVictim())
            {
                return;
            }

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
                    case HelOaf_Events.EVENT_THROW_BOMB:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);
                        if (target != null)
                        {
                            me.CastSpell(target, eSpels.THROW_BOMB, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle | TriggerCastFlags.IgnoreCasterAurastate));
                        }
                        _events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));
                        break;
                    case HelOaf_Events.EVENT_CHEST_BOMB:
                        Unit target1 = SelectTarget(SelectTargetMethod.Random, 0, 150, true);
                        if (target1 != null)
                        {
                            me.TextEmote(CHEST_BOMB, target1, true);
                            me.AddAura(eSpels.CHEST_BOMB, target1);
                        }
                        _events.ScheduleEvent(HelOaf_Events.EVENT_CHEST_BOMB, TimeSpan.FromMilliseconds(11000));
                        break;
                    case HelOaf_Events.EVENT_NO_OAF:
                        me.RemoveUnitFlag(UnitFlags.Uninteractible);
                        me.RemoveAura(eSpels.OAFQUARD);
                        Talk(2);
                        _events.RescheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));
                        break;
                    case HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF:
                        List<Unit> players = new List<Unit>();
                        AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                        PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                        Cell.VisitGrid(me, searcher, 150f);

                        foreach (var item in players)
                        {
                            me.CastSpell(item, eSpels.HELIX_RIDE, true);
                        }

                        _events.ScheduleEvent(HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF, TimeSpan.FromMilliseconds(60000));
                        break;
                }
            }
        }
    }
}
