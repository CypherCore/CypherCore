// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    public class WorldBossAI : ScriptedAI
    {
        private readonly SummonList _summons;

        public WorldBossAI(Creature creature) : base(creature)
        {
            _summons = new SummonList(creature);
        }

        private void _Reset()
        {
            if (!me.IsAlive())
                return;

            Events.Reset();
            _summons.DespawnAll();
        }

        private void _JustDied()
        {
            Events.Reset();
            _summons.DespawnAll();
        }

        private void _JustEngagedWith()
        {
            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

            if (target)
                AttackStart(target);
        }

        public override void JustSummoned(Creature summon)
        {
            _summons.Summon(summon);
            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

            if (target)
                summon.GetAI().AttackStart(target);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            _summons.Despawn(summon);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            Events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            Events.ExecuteEvents(eventId =>
                                  {
                                      ExecuteEvent(eventId);

                                      if (me.HasUnitState(UnitState.Casting))
                                          return;
                                  });

            DoMeleeAttackIfReady();
        }

        // Hook used to execute events scheduled into EventMap without the need
        // to override UpdateAI
        // note: You must re-schedule the event within this method if the event
        // is supposed to run more than once
        public virtual void ExecuteEvent(uint eventId)
        {
        }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            _JustEngagedWith();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
        }
    }
}