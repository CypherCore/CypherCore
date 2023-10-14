// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets.Mage
{
    [Script]
    class npc_pet_mage_mirror_image : ScriptedAI
    {
        const uint SpellMageCloneMe = 45204;
        const uint SpellMageFrostBolt = 59638;
        const uint SpellMageFireBlast = 59637;

        const uint TimerMirrorImageFireBlast = 6500;
        float ChaseDistance = 35.0f;

        uint _fireBlastTimer = 0;

        public npc_pet_mage_mirror_image(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            Unit owner = me.GetOwner();
            if (owner == null)
                return;

            // here mirror image casts on summoner spell (not present in client dbc) 49866
            // here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcast by mirror images (stats related?)
            // Clone Me!
            owner.CastSpell(me, SpellMageCloneMe, true);
        }

        // custom UpdateVictim implementation to handle special target selection
        // we prioritize between things that are in combat with owner based on the owner's threat to them
        bool UpdateVictimCustom()
        {
            Unit owner = me.GetOwner();
            if (owner == null)
                return false;

            if (!me.HasUnitState(UnitState.Casting) && !me.IsInCombat() && !owner.IsInCombat())
                return false;

            Unit currentTarget = me.GetVictim();
            if (currentTarget != null && !CanAIAttack(currentTarget))
            {
                me.InterruptNonMeleeSpells(true); // do not finish casting on invalid targets
                me.AttackStop();
                currentTarget = null;
            }

            // don't reselect if we're currently casting anyway
            if (currentTarget != null && me.HasUnitState(UnitState.Casting))
                return true;

            Unit selectedTarget = null;
            var mgr = owner.GetCombatManager();
            if (mgr.HasPvPCombat())
            { // select pvp target
                float minDistance = 0.0f;
                foreach (var pair in mgr.GetPvPCombatRefs())
                {
                    Unit target = pair.Value.GetOther(owner);
                    if (!target.IsPlayer())
                        continue;
                    if (!CanAIAttack(target))
                        continue;

                    float dist = owner.GetDistance(target);
                    if (selectedTarget == null || dist < minDistance)
                    {
                        selectedTarget = target;
                        minDistance = dist;
                    }
                }
            }

            if (selectedTarget == null)
            { // select pve target
                float maxThreat = 0.0f;
                foreach (var pair in mgr.GetPvECombatRefs())
                {
                    Unit target = pair.Value.GetOther(owner);
                    if (!CanAIAttack(target))
                        continue;

                    float threat = target.GetThreatManager().GetThreat(owner);
                    if (threat >= maxThreat)
                    {
                        selectedTarget = target;
                        maxThreat = threat;
                    }
                }
            }

            if (selectedTarget == null)
            {
                EnterEvadeMode(EvadeReason.NoHostiles);
                return false;
            }

            if (selectedTarget != me.GetVictim())
                AttackStartCaster(selectedTarget, ChaseDistance);
            return true;
        }

        public override void UpdateAI(uint diff)
        {
            Unit owner = me.GetOwner();
            if (owner == null)
            {
                me.DespawnOrUnsummon();
                return;
            }

            if (_fireBlastTimer != 0)
            {
                if (_fireBlastTimer <= diff)
                    _fireBlastTimer = 0;
                else
                    _fireBlastTimer -= diff;
            }

            if (!UpdateVictimCustom())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (_fireBlastTimer == 0)
            {
                DoCastVictim(SpellMageFireBlast);
                _fireBlastTimer = TimerMirrorImageFireBlast;
            }
            else
                DoCastVictim(SpellMageFrostBolt);
        }

        public override bool CanAIAttack(Unit who)
        {
            Unit owner = me.GetOwner();
            return owner != null && who.IsAlive() && me.IsValidAttackTarget(who) &&
                !who.HasBreakableByDamageCrowdControlAura() &&
                who.IsInCombatWith(owner) && base.CanAIAttack(who);
        }

        // Do not reload Creature templates on evade mode enter - prevent visual lost
        public override void EnterEvadeMode(EvadeReason why)
        {
            if (me.IsInEvadeMode() || !me.IsAlive())
                return;

            Unit owner = me.GetCharmerOrOwner();

            me.CombatStop(true);
            if (owner != null && !me.HasUnitState(UnitState.Follow))
            {
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, me.GetFollowAngle());
            }
        }
    }
}