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
    public class NagrandArena : Arena
    {
        public override void StartingEventCloseDoors()
        {
            for (int i = NagrandArenaObjectTypes.Door1; i <= NagrandArenaObjectTypes.Door4; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = NagrandArenaObjectTypes.Door1; i <= NagrandArenaObjectTypes.Door4; ++i)
                DoorOpen(i);

            for (int i = NagrandArenaObjectTypes.Buff1; i <= NagrandArenaObjectTypes.Buff2; ++i)
                SpawnBGObject(i, 60);
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch (trigger)
            {
                case 4536:                                          // buff trigger?
                case 4537:                                          // buff trigger?
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(0xa11, 1);
            base.FillInitialWorldStates(packet);
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(NagrandArenaObjectTypes.Door1, NagrandArenaObjects.Door1, 4031.854f, 2966.833f, 12.6462f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(NagrandArenaObjectTypes.Door2, NagrandArenaObjects.Door2, 4081.179f, 2874.97f, 12.39171f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(NagrandArenaObjectTypes.Door3, NagrandArenaObjects.Door3, 4023.709f, 2981.777f, 10.70117f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(NagrandArenaObjectTypes.Door4, NagrandArenaObjects.Door4, 4090.064f, 2858.438f, 10.23631f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f, BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "NagrandArena: Failed to spawn door object!");
                return false;
            }

            result &= AddObject(NagrandArenaObjectTypes.Buff1, NagrandArenaObjects.Buff1, 4009.189941f, 2895.250000f, 13.052700f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
            result &= AddObject(NagrandArenaObjectTypes.Buff2, NagrandArenaObjects.Buff2, 4103.330078f, 2946.350098f, 13.051300f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "NagrandArena: Failed to spawn buff object!");
                return false;
            }

            return true;
        }
    }

    struct NagrandArenaObjectTypes
    {
        public const int Door1 = 0;
        public const int Door2 = 1;
        public const int Door3 = 2;
        public const int Door4 = 3;
        public const int Buff1 = 4;
        public const int Buff2 = 5;
        public const int Max = 6;
    }

    struct NagrandArenaObjects
    {
        public const uint Door1 = 183978;
        public const uint Door2 = 183980;
        public const uint Door3 = 183977;
        public const uint Door4 = 183979;
        public const uint Buff1 = 184663;
        public const uint Buff2 = 184664;
    }
}
