using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_helix_gearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(47297)]
    public class npc_lumbering_oafAI : ScriptedAI
    {
        public npc_lumbering_oafAI(Creature pCreature) : base(pCreature)
        {
            _vehicle = me.GetVehicleKit();
            _instance = pCreature.GetInstanceScript();
            _summons = new SummonList(pCreature);
        }

        private InstanceScript _instance;
        private SummonList _summons;
        private EventMap _events = new EventMap();
        private Vehicle _vehicle;
        private Creature _bunny;

        public override void Reset()
        {
            if (!me || _vehicle == null)
            {
                return;
            }

            _events.Reset();
        }

        public override void JustEnteredCombat(Unit who)
        {
            if (!me)
            {
                return;
            }

            _events.ScheduleEvent(HelOaf_Events.EVENT_OAFQUARD, TimeSpan.FromMilliseconds(5000));
        }

        public override void JustDied(Unit killer)
        {
            Creature Helix = me.FindNearestCreature(DMCreatures.NPC_HELIX_GEARBREAKER, 200, true);
            if (Helix != null)
            {
                boss_helix_gearbreaker pAI = (boss_helix_gearbreaker)Helix.GetAI();
                if (pAI != null)
                {
                    pAI.OafDead();
                }
            }
        }

        public void SummonBunny()
        {
            Talk(0);
            Talk(1);
            _bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, OafPos[1].GetPositionX(), OafPos[1].GetPositionY(), OafPos[1].GetPositionZ());
            me.SetInCombatWithZone();
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
            {
                return;
            }

            if (id == 1)
            {
                if (_bunny != null)
                {
                    me.SetInCombatWithZone();
                    Unit passenger = me.GetVehicleKit().GetPassenger(1);
                    if (passenger != null)
                    {
                        passenger.ExitVehicle();
                        me.Attack(passenger, true);
                    }

                    if (_bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 100.0f))
                    {
                        me.CastSpell(me, IsHeroic() ? eSpels.SPELL_OAF_SMASH_H : eSpels.SPELL_OAF_SMASH);

                        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -303.193481f, -486.287140f, 49.185917f, 2.152038f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(360000));
                        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -300.496674f, -490.433746f, 49.073387f, 5.243889f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
                        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -298.689301f, -486.994995f, 48.893055f, 0.950859f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
                        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -301.923676f, -486.674591f, 49.081684f, 2.677864f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
                        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -296.066345f, -488.150177f, 48.917435f, 2.657793f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));

                        Unit.Kill(_bunny, _bunny);
                        me.SetSpeed(UnitMoveType.Run, 1.0f);
                    }
                }

                Unit target = SelectTarget(SelectTargetMethod.MaxThreat);
                if (target != null)
                {
                    DoStartMovement(target);
                }
            }
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!UpdateVictim())
            {
                return;
            }

            if (!me || _vehicle == null)
            {
                return;
            }

            _events.Update(uiDiff);

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case HelOaf_Events.EVENT_OAFQUARD:
                        SummonBunny();
                        _events.ScheduleEvent(HelOaf_Events.EVENT_MOUNT_PLAYER, TimeSpan.FromMilliseconds(500));
                        break;

                    case HelOaf_Events.EVENT_MOUNT_PLAYER:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);
                        if (target != null)
                        {
                            target.CastSpell(me, eSpels.SPELL_RIDE_VEHICLE_HARDCODED);
                        }
                        _events.ScheduleEvent(HelOaf_Events.EVENT_MOVE_TO_POINT, TimeSpan.FromMilliseconds(500));
                        break;

                    case HelOaf_Events.EVENT_MOVE_TO_POINT:
                        me.SetSpeed(UnitMoveType.Run, 5.0f);
                        me.GetMotionMaster().MovePoint(0, -289.809f, -527.215f, 49.8021f);
                        _events.ScheduleEvent(HelOaf_Events.EVEMT_CHARGE, TimeSpan.FromMilliseconds(2000));
                        break;

                    case HelOaf_Events.EVEMT_CHARGE:
                        if (me.GetDistance(OafPos[0]) <= 2.0f)
                        {
                            me.GetMotionMaster().Clear();
                            if (_bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 150.0f))
                            {
                                me.GetMotionMaster().MovePoint(1, _bunny.GetPositionX(), _bunny.GetPositionY(), _bunny.GetPositionZ());
                                _bunny.SetUnitFlag(UnitFlags.Uninteractible);
                            }
                        }
                        _events.ScheduleEvent(HelOaf_Events.EVENT_FINISH, TimeSpan.FromMilliseconds(1500));
                        break;

                    case HelOaf_Events.EVENT_FINISH:
                        _events.ScheduleEvent(HelOaf_Events.EVENT_OAFQUARD, TimeSpan.FromMilliseconds(17000));
                        break;
                }
            }
            DoMeleeAttackIfReady();
        }
    }
}
