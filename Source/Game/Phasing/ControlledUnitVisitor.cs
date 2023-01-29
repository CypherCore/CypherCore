// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game
{
    internal class ControlledUnitVisitor
    {
        private readonly HashSet<WorldObject> _visited = new();

        public ControlledUnitVisitor(WorldObject owner)
        {
            _visited.Add(owner);
        }

        public void VisitControlledOf(Unit unit, Action<Unit> func)
        {
            foreach (Unit controlled in unit.Controlled)
                // Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
                if (!controlled.IsPlayer() &&
                    controlled.GetVehicle() == null)
                    if (_visited.Add(controlled))
                        func(controlled);

            foreach (ObjectGuid summonGuid in unit.SummonSlot)
                if (!summonGuid.IsEmpty())
                {
                    Creature summon = ObjectAccessor.GetCreature(unit, summonGuid);

                    if (summon != null)
                        if (_visited.Add(summon))
                            func(summon);
                }

            Vehicle vehicle = unit.GetVehicleKit();

            if (vehicle != null)
                foreach (var seatPair in vehicle.Seats)
                {
                    Unit passenger = Global.ObjAccessor.GetUnit(unit, seatPair.Value.Passenger.Guid);

                    if (passenger != null &&
                        passenger != unit)
                        if (_visited.Add(passenger))
                            func(passenger);
                }
        }
    }
}