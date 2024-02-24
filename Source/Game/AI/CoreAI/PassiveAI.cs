// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;
using System;

namespace Game.AI
{
    public class PassiveAI : CreatureAI
    {
        public PassiveAI(Creature creature) : base(creature)
        {
            me.SetReactState(ReactStates.Passive);
            me.SetCanMelee(false);
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
        TimeTracker _evadeTimer;

        public CritterAI(Creature creature) : base(creature)
        {
            _evadeTimer = new();
            me.SetCanMelee(false, true);
        }

        public override void JustEngagedWith(Unit who)
        {
            me.StartDefaultCombatMovement(who);            
            _evadeTimer.Reset(TimeSpan.FromMilliseconds(WorldConfig.GetIntValue(WorldCfg.CreatureFamilyFleeDelay)));
        }

        public override void UpdateAI(uint diff)
        {
            if (me.IsEngaged())
            {
                if (!me.IsInCombat())
                {
                    EnterEvadeMode(EvadeReason.NoHostiles);
                    return;
                }

                _evadeTimer.Update(diff);
                if (_evadeTimer.Passed())
                    EnterEvadeMode(EvadeReason.Other);
            }
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
