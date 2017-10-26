using System;
using Game.Entities;
using Game.AI;
using Game.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.TheStockade
{
    struct TextIds
    {
        public const uint SayPull = 0; // Forest Just Setback!
        public const uint SayEnrage = 1; // Areatriggermessage: Hogger Enrages!
        public const uint SayDeath = 2; // Yiipe!

        public const uint SayWarden1 = 0; // Yell - This Ends Here; Hogger!
        public const uint SayWarden2 = 1; // Say - He'S...He'S Dead?
        public const uint SayWarden3 = 2; // Say - It'S Simply Too Good To Be True. You Couldn'T Have Killed Him So Easily!
    }

    struct SpellIds
    {
        public const uint ViciousSlice = 86604;
        public const uint MaddeningCall = 86620;
        public const uint Enrage = 86736;
    }

    struct Events
    {
        public const uint SayWarden1 = 1;
        public const uint SayWarden2 = 2;
        public const uint SayWarden3 = 3;
    }

    [Script]
    class boss_hogger : BossAI
    {
        public boss_hogger(Creature creature) : base(creature, DataTypes.Hogger) { }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayPull);

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.ViciousSlice);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), task =>
            {
                DoCast(SpellIds.MaddeningCall);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            _JustDied();
            me.SummonCreature(CreatureIds.WardenThelwater, Misc.WardenThelwaterPos);
        }

        public override void JustSummoned(Creature summon)
        {
            base.JustSummoned(summon);
            if (summon.GetEntry() == CreatureIds.WardenThelwater)
                summon.GetMotionMaster().MovePoint(0, Misc.WardenThelwaterMovePos);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (me.HealthBelowPctDamaged(30, damage) && !_hasEnraged)
            {
                _hasEnraged = true;
                Talk(TextIds.SayEnrage);
                DoCastSelf(SpellIds.Enrage);
            }
        }

        bool _hasEnraged;
    }

    [Script]
    class npc_warden_thelwater : ScriptedAI
    {
        public npc_warden_thelwater(Creature creature) : base(creature) { }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.Point && id == 0)
                _events.ScheduleEvent(Events.SayWarden1, TimeSpan.FromSeconds(1));
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.SayWarden1:
                        Talk(TextIds.SayWarden1);
                        _events.ScheduleEvent(Events.SayWarden2, TimeSpan.FromSeconds(4));
                        break;
                    case Events.SayWarden2:
                        Talk(TextIds.SayWarden2);
                        _events.ScheduleEvent(Events.SayWarden3, TimeSpan.FromSeconds(3));
                        break;
                    case Events.SayWarden3:
                        Talk(TextIds.SayWarden3);
                        break;
                }
            });
        }
    }
}
