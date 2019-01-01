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
using Game.Network.Packets;

namespace Game.Arenas
{
    public class BladesEdgeArena : Arena
    {
        public override void StartingEventCloseDoors()
        {
            for (int i = BladeEdgeObjectTypes.Door1; i <= BladeEdgeObjectTypes.Door4; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            for (int i = BladeEdgeObjectTypes.Buff1; i <= BladeEdgeObjectTypes.Buff2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = BladeEdgeObjectTypes.Door1; i <= BladeEdgeObjectTypes.Door4; ++i)
                DoorOpen(i);

            for (int i = BladeEdgeObjectTypes.Buff1; i <= BladeEdgeObjectTypes.Buff2; ++i)
                SpawnBGObject(i, 60);
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch (trigger)
            {
                case 4538:                                          // buff trigger?
                case 4539:                                          // buff trigger?
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(0x9f3, 1);
            base.FillInitialWorldStates(packet);
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(BladeEdgeObjectTypes.Door1, BladeEfgeGameObjects.Door1, 6287.277f, 282.1877f, 3.810925f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(BladeEdgeObjectTypes.Door2, BladeEfgeGameObjects.Door2, 6189.546f, 241.7099f, 3.101481f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(BladeEdgeObjectTypes.Door3, BladeEfgeGameObjects.Door3, 6299.116f, 296.5494f, 3.308032f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(BladeEdgeObjectTypes.Door4, BladeEfgeGameObjects.Door4, 6177.708f, 227.3481f, 3.604374f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f, BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BatteGroundBE: Failed to spawn door object!");
                return false;
            }

            result &= AddObject(BladeEdgeObjectTypes.Buff1, BladeEfgeGameObjects.Buff1, 6249.042f, 275.3239f, 11.22033f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
            result &= AddObject(BladeEdgeObjectTypes.Buff2, BladeEfgeGameObjects.Buff2, 6228.26f, 249.566f, 11.21812f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BladesEdgeArena: Failed to spawn buff object!");
                return false;
            }

            return true;
        }
    }

    struct BladeEdgeObjectTypes
    {
        public const int Door1 = 0;
        public const int Door2 = 1;
        public const int Door3 = 2;
        public const int Door4 = 3;
        public const int Buff1 = 4;
        public const int Buff2 = 5;
        public const int Max = 6;
    }

    struct BladeEfgeGameObjects
    {
        public const uint Door1 = 183971;
        public const uint Door2 = 183973;
        public const uint Door3 = 183970;
        public const uint Door4 = 183972;
        public const uint Buff1 = 184663;
        public const uint Buff2 = 184664;
    }
}
