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
using Framework.Dynamic;
using Game.Entities;
using Game.Network.Packets;

namespace Game.Arenas
{
    class RingofValorArena : Arena
    {
        public RingofValorArena()
        {
            _events = new EventMap();
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(RingofValorObjectTypes.Elevator1, RingofValorGameObjects.Elevator1, 763.536377f, -294.535767f, 0.505383f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Elevator2, RingofValorGameObjects.Elevator2, 763.506348f, -273.873352f, 0.505383f, 0.000000f, 0, 0, 0, 0);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RingofValorArena: Failed to spawn elevator object!");
                return false;
            }

            result &= AddObject(RingofValorObjectTypes.Buff1, RingofValorGameObjects.Buff1, 735.551819f, -284.794678f, 28.276682f, 0.034906f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Buff2, RingofValorGameObjects.Buff2, 791.224487f, -284.794464f, 28.276682f, 2.600535f, 0, 0, 0, 0);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RingofValorArena: Failed to spawn buff object!");
                return false;
            }

            result &= AddObject(RingofValorObjectTypes.Fire1, RingofValorGameObjects.Fire1, 743.543457f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Fire2, RingofValorGameObjects.Fire2, 782.971802f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Firedoor1, RingofValorGameObjects.Firedoor1, 743.711060f, -284.099609f, 27.542587f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Firedoor2, RingofValorGameObjects.Firedoor2, 783.221252f, -284.133362f, 27.535686f, 0.000000f, 0, 0, 0, 0);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RingofValorArena: Failed to spawn fire/firedoor object!");
                return false;
            }

            result &= AddObject(RingofValorObjectTypes.Gear1, RingofValorGameObjects.Gear1, 763.664551f, -261.872986f, 26.686588f, 0.000000f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Gear2, RingofValorGameObjects.Gear2, 763.578979f, -306.146149f, 26.665222f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Pulley1, RingofValorGameObjects.Pulley1, 700.722290f, -283.990662f, 39.517582f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Pulley2, RingofValorGameObjects.Pulley2, 826.303833f, -283.996429f, 39.517582f, 0.000000f, 0, 0, 0, 0);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RingofValorArena: Failed to spawn gear/pully object!");
                return false;
            }

            result &= AddObject(RingofValorObjectTypes.Pilar1, RingofValorGameObjects.Pilar1, 763.632385f, -306.162384f, 25.909504f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Pilar2, RingofValorGameObjects.Pilar2, 723.644287f, -284.493256f, 24.648525f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Pilar3, RingofValorGameObjects.Pilar3, 763.611145f, -261.856750f, 25.909504f, 0.000000f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.Pilar4, RingofValorGameObjects.Pilar4, 802.211609f, -284.493256f, 24.648525f, 0.000000f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.PilarCollision1, RingofValorGameObjects.PilarCollision1, 763.632385f, -306.162384f, 30.639660f, 3.141593f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.PilarCollision2, RingofValorGameObjects.PilarCollision2, 723.644287f, -284.493256f, 32.382710f, 0.000000f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.PilarCollision3, RingofValorGameObjects.PilarCollision3, 763.611145f, -261.856750f, 30.639660f, 0.000000f, 0, 0, 0, 0);
            result &= AddObject(RingofValorObjectTypes.PilarCollision4, RingofValorGameObjects.PilarCollision4, 802.211609f, -284.493256f, 32.382710f, 3.141593f, 0, 0, 0, 0);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RingofValorArena: Failed to spawn pilar object!");
                return false;
            }

            return true;
        }

        public override void StartingEventOpenDoors()
        {
            // Buff respawn
            SpawnBGObject(RingofValorObjectTypes.Buff1, 90);
            SpawnBGObject(RingofValorObjectTypes.Buff2, 90);
            // Elevators
            DoorOpen(RingofValorObjectTypes.Elevator1);
            DoorOpen(RingofValorObjectTypes.Elevator2);

            _events.ScheduleEvent(RingofValorEvents.OpenFences, 20133);

            // Should be false at first, TogglePillarCollision will do it.
            TogglePillarCollision(true);
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch (trigger)
            {
                case 5224:
                case 5226:
                // fire was removed in 3.2.0
                case 5473:
                case 5474:
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(0xe1a, 1);
            base.FillInitialWorldStates(packet);
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case RingofValorEvents.OpenFences:
                        // Open fire (only at game start)
                        for (byte i = RingofValorObjectTypes.Fire1; i <= RingofValorObjectTypes.Firedoor2; ++i)
                            DoorOpen(i);
                        _events.ScheduleEvent(RingofValorEvents.CloseFire, 5000);
                        break;
                    case RingofValorEvents.CloseFire:
                        for (byte i = RingofValorObjectTypes.Fire1; i <= RingofValorObjectTypes.Firedoor2; ++i)
                            DoorClose(i);
                        // Fire got closed after five seconds, leaves twenty seconds before toggling pillars
                        _events.ScheduleEvent(RingofValorEvents.SwitchPillars, 20000);
                        break;
                    case RingofValorEvents.SwitchPillars:
                        TogglePillarCollision(true);
                        _events.Repeat(25000);
                        break;
                }
            });
        }

        void TogglePillarCollision(bool enable)
        {
            // Toggle visual pillars, pulley, gear, and collision based on previous state
            for (int i = RingofValorObjectTypes.Pilar1; i <= RingofValorObjectTypes.Gear2; ++i)
            {
                if (enable)
                    DoorOpen(i);
                else
                    DoorClose(i);
            }

            for (byte i = RingofValorObjectTypes.Pilar2; i <= RingofValorObjectTypes.Pulley2; ++i)
            {
                if (enable)
                    DoorClose(i);
                else
                     DoorOpen(i);
            }

            for (byte i = RingofValorObjectTypes.Pilar1; i <= RingofValorObjectTypes.PilarCollision4; ++i)
            {
                GameObject go = GetBGObject(i);
                if (go)
                {
                    if (i >= RingofValorObjectTypes.PilarCollision1)
                    {
                        GameObjectState state = ((go.GetGoInfo().Door.startOpen != 0) == enable) ? GameObjectState.Active : GameObjectState.Ready;
                        go.SetGoState(state);
                    }

                    foreach (var guid in GetPlayers().Keys)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player)
                            go.SendUpdateToPlayer(player);
                    }
                }
            }
        }

        EventMap _events;
    }

    struct RingofValorEvents
    {
        public const int OpenFences = 0;
        public const int SwitchPillars = 1;
        public const int CloseFire = 2;
    }

    struct RingofValorObjectTypes
    {
        public const int Buff1 = 1;
        public const int Buff2 = 2;
        public const int Fire1 = 3;
        public const int Fire2 = 4;
        public const int Firedoor1 = 5;
        public const int Firedoor2 = 6;

        public const int Pilar1 = 7;
        public const int Pilar3 = 8;
        public const int Gear1 = 9;
        public const int Gear2 = 10;

        public const int Pilar2 = 11;
        public const int Pilar4 = 12;
        public const int Pulley1 = 13;
        public const int Pulley2 = 14;

        public const int PilarCollision1 = 15;
        public const int PilarCollision2 = 16;
        public const int PilarCollision3 = 17;
        public const int PilarCollision4 = 18;

        public const int Elevator1 = 19;
        public const int Elevator2= 20;
        public const int Max = 21;
    }

    struct RingofValorGameObjects
    {
        public const uint Buff1 = 184663;
        public const uint Buff2 = 184664;
        public const uint Fire1 = 192704;
        public const uint Fire2 = 192705;

        public const uint Firedoor2 = 192387;
        public const uint Firedoor1 = 192388;
        public const uint Pulley1 = 192389;
        public const uint Pulley2 = 192390;
        public const uint Gear1 = 192393;
        public const uint Gear2 = 192394;
        public const uint Elevator1 = 194582;
        public const uint Elevator2 = 194586;

        public const uint PilarCollision1 = 194580; // Axe
        public const uint PilarCollision2 = 194579; // Arena
        public const uint PilarCollision3 = 194581; // Lightning
        public const uint PilarCollision4 = 194578; // Ivory

        public const uint Pilar1 = 194583; // Axe
        public const uint Pilar2 = 194584; // Arena
        public const uint Pilar3 = 194585; // Lightning
        public const uint Pilar4 = 194587;  // Ivory
    }
}
