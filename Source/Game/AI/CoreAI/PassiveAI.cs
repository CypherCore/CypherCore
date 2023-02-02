// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public class PassiveAI : CreatureAI
    {
        public PassiveAI(Creature creature) : base(creature)
        {
            creature.SetReactState(ReactStates.Passive);
        }

        public override void UpdateAI(uint diff)
        {
            if (me.IsEngaged() && !me.IsInCombat())
                EnterEvadeMode(EvadeReason.NoHostiles);
        }

        public override void AttackStart(Unit victim) { }

        public override void MoveInLineOfSight(Unit who) { }
    }

    public class PossessedAI : CreatureAI
    {
        public PossessedAI(Creature creature) : base(creature)
        {
            creature.SetReactState(ReactStates.Passive);
        }

        public override void AttackStart(Unit target)
        {
            me.Attack(target, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (me.GetVictim() != null)
            {
                if (!me.IsValidAttackTarget(me.GetVictim()))
                    me.AttackStop();
                else
                    DoMeleeAttackIfReady();
            }
        }

        public override void JustDied(Unit unit)
        {
            // We died while possessed, disable our loot
            me.RemoveDynamicFlag(UnitDynFlags.Lootable);
        }

        public override void MoveInLineOfSight(Unit who) { }

        public override void JustEnteredCombat(Unit who)
        {
            EngagementStart(who);
        }

        public override void JustExitedCombat()
        {
            EngagementOver();
        }

        public override void JustStartedThreateningMe(Unit who)
        {

        }

        public override void EnterEvadeMode(EvadeReason why) { }
    }

    public class NullCreatureAI : CreatureAI
    {
        public NullCreatureAI(Creature creature) : base(creature)
        {
            creature.SetReactState(ReactStates.Passive);
        }

        public override void MoveInLineOfSight(Unit unit) { }
        public override void AttackStart(Unit unit) { }
        public override  void JustStartedThreateningMe(Unit unit)  { }
        public override void JustEnteredCombat(Unit who) { }
        public override void UpdateAI(uint diff) { }
        public override void JustAppeared() { }
        public override void EnterEvadeMode(EvadeReason why) { }
        public override void OnCharmed(bool isNew) { }
    }

    public class CritterAI : PassiveAI
    {
        public CritterAI(Creature c) : base(c)
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void JustEngagedWith(Unit who)
        {
            if (!me.HasUnitState(UnitState.Fleeing))
                me.SetControlled(true, UnitState.Fleeing);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.TimedFleeing)
                EnterEvadeMode(EvadeReason.Other);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (me.HasUnitState(UnitState.Fleeing))
                me.SetControlled(false, UnitState.Fleeing);
            base.EnterEvadeMode(why);
        }
    }

    public class TriggerAI : NullCreatureAI
    {
        public TriggerAI(Creature c) : base(c) { }

        public override void IsSummonedBy(WorldObject summoner)
        {
            if (me.m_spells[0] != 0)
            {
                CastSpellExtraArgs extra = new();
                extra.OriginalCaster = summoner.GetGUID();
                me.CastSpell(me, me.m_spells[0], extra);
            }
        }
    }
}
