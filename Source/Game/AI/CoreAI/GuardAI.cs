/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
            if (!obj.isTypeMask(TypeMask.Unit))
                return false;

            var threatList = me.GetThreatManager().getThreatList();
            foreach (var refe in threatList)
                if (refe.getUnitGuid() == obj.GetGUID())
                    return true;

            return false;
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive())
            {
                me.GetMotionMaster().MoveIdle();
                me.CombatStop(true);
                me.DeleteThreatList();
                return;
            }

            Log.outDebug(LogFilter.Unit, "Guard entry: {0} enters evade mode.", me.GetEntry());

            me.RemoveAllAuras();
            me.DeleteThreatList();
            me.CombatStop(true);

            // Remove ChaseMovementGenerator from MotionMaster stack list, and add HomeMovementGenerator instead
            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Chase)
                me.GetMotionMaster().MoveTargetedHome();
        }

        public override void JustDied(Unit killer)
        {
            Player player = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player != null)
                me.SendZoneUnderAttackMessage(player);
        }
    }
}
