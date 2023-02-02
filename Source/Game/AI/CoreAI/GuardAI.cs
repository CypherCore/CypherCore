// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    public class GuardAI : ScriptedAI
    {
        public GuardAI(Creature creature) : base(creature) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override bool CanSeeAlways(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            if (unit != null)
                if (unit.IsControlledByPlayer() && me.IsEngagedBy(unit))
                    return true;

            return false;
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive())
            {
                me.GetMotionMaster().MoveIdle();
                me.CombatStop(true);
                EngagementOver();
                return;
            }

            Log.outTrace(LogFilter.ScriptsAi, $"GuardAI::EnterEvadeMode: {me.GetGUID()} enters evade mode.");

            me.RemoveAllAuras();
            me.CombatStop(true);
            EngagementOver();

            me.GetMotionMaster().MoveTargetedHome();
        }

        public override void JustDied(Unit killer)
        {
            if (killer != null)
            {
                Player player = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (player != null)
                    me.SendZoneUnderAttackMessage(player);
            }
        }
    }
}
