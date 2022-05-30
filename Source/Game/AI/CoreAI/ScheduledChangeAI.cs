/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
    class ScheduledChangeAI : CreatureAI
    {
        public ScheduledChangeAI(Creature creature) : base(creature) { }

        public override void MoveInLineOfSight(Unit unit) { }

        public override void AttackStart(Unit unit) { }

        public override void JustStartedThreateningMe(Unit unit) { }

        public override void JustEnteredCombat(Unit unit) { }

        public override void UpdateAI(uint diff) { }

        public override void JustAppeared() { }

        public override void EnterEvadeMode(EvadeReason why) { }

        public override void OnCharmed(bool isNew) { }
    }
}
