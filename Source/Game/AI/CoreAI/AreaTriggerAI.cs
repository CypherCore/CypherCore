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

using Game.Entities;

namespace Game.AI
{
    public class AreaTriggerAI
    {
        public AreaTriggerAI(AreaTrigger a)
        {
            at = a;
        }

        // Called when the AreaTrigger has just been initialized, just before added to map
        public virtual void OnInitialize() { }

        // Called when the AreaTrigger has just been created
        public virtual void OnCreate() { }

        // Called on each AreaTrigger update
        public virtual void OnUpdate(uint diff) { }

        // Called when the AreaTrigger reach splineIndex
        public virtual void OnSplineIndexReached(int splineIndex) { }

        // Called when the AreaTrigger reach its destination
        public virtual void OnDestinationReached() { }

        // Called when an unit enter the AreaTrigger
        public virtual void OnUnitEnter(Unit unit) { }

        // Called when an unit exit the AreaTrigger, or when the AreaTrigger is removed
        public virtual void OnUnitExit(Unit unit) { }

        // Called when the AreaTrigger is removed
        public virtual void OnRemove() { }

        protected AreaTrigger at;
    }

    class NullAreaTriggerAI : AreaTriggerAI
    {
        public NullAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }
    }
}
