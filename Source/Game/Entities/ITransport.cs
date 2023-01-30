// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Maps;

namespace Game.Entities
{
    public interface ITransport
    {
        ObjectGuid GetTransportGUID();

        // This method transforms supplied Transport offsets into global coordinates
        void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o);

        // This method transforms supplied global coordinates into local offsets
        void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o);

        float GetTransportOrientation();

        void AddPassenger(WorldObject passenger);

        ITransport RemovePassenger(WorldObject passenger);

        public static void UpdatePassengerPosition(ITransport transport, Map map, WorldObject passenger, float x, float y, float z, float o, bool setHomePosition)
        {
            // Transport teleported but passenger not yet (can happen for players)
            if (passenger.GetMap() != map)
                return;

            // Do not use Unit::UpdatePosition here, we don't want to remove Auras
            // as if regular movement occurred
            switch (passenger.GetTypeId())
            {
                case TypeId.Unit:
                    {
                        Creature creature = passenger.ToCreature();
                        map.CreatureRelocation(creature, x, y, z, o, false);

                        if (setHomePosition)
                        {
                            creature.GetTransportHomePosition(out x, out y, out z, out o);
                            transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                            creature.SetHomePosition(x, y, z, o);
                        }

                        break;
                    }
                case TypeId.Player:
                    //relocate only passengers in world and skip any player that might be still logging in/teleporting
                    if (passenger.IsInWorld &&
                        !passenger.ToPlayer().IsBeingTeleported())
                    {
                        map.PlayerRelocation(passenger.ToPlayer(), x, y, z, o);
                        passenger.ToPlayer().SetFallInformation(0, passenger.GetPositionZ());
                    }

                    break;
                case TypeId.GameObject:
                    map.GameObjectRelocation(passenger.ToGameObject(), x, y, z, o, false);
                    passenger.ToGameObject().RelocateStationaryPosition(x, y, z, o);

                    break;
                case TypeId.DynamicObject:
                    map.DynamicObjectRelocation(passenger.ToDynamicObject(), x, y, z, o);

                    break;
                case TypeId.AreaTrigger:
                    map.AreaTriggerRelocation(passenger.ToAreaTrigger(), x, y, z, o);

                    break;
                default:
                    break;
            }

            Unit unit = passenger.ToUnit();

            if (unit != null)
            {
                Vehicle vehicle = unit.GetVehicleKit();

                vehicle?.RelocatePassengers();
            }
        }

        static void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
        {
            float inx = x, iny = y, inz = z;
            o = Position.NormalizeOrientation(transO + o);

            x = transX + inx * MathF.Cos(transO) - iny * MathF.Sin(transO);
            y = transY + iny * MathF.Cos(transO) + inx * MathF.Sin(transO);
            z = transZ + inz;
        }

        static void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
        {
            o = Position.NormalizeOrientation(o - transO);

            z -= transZ;
            y -= transY; // y = searchedY * std::cos(o) + searchedX * std::sin(o)
            x -= transX; // x = searchedX * std::cos(o) + searchedY * std::sin(o + pi)
            float inx = x, iny = y;
            y = (iny - inx * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
            x = (inx + iny * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
        }

        int GetMapIdForSpawning();
    }
}