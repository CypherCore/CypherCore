// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Networking.Packets;
using System;

namespace Game.Arenas
{
    class RuinsofLordaeronArena : Arena
    {
        public RuinsofLordaeronArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate) { }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            taskScheduler.Update(diff);
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(RuinsofLordaeronObjectTypes.Door1, RuinsofLordaeronObjectTypes.Door1, 1293.561f, 1601.938f, 31.60557f, -1.457349f, 0, 0, -0.6658813f, 0.7460576f);
            result &= AddObject(RuinsofLordaeronObjectTypes.Door2, RuinsofLordaeronObjectTypes.Door2, 1278.648f, 1730.557f, 31.60557f, 1.684245f, 0, 0, 0.7460582f, 0.6658807f);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RuinsofLordaeronArena: Failed to spawn door object!");
                return false;
            }

            result &= AddObject(RuinsofLordaeronObjectTypes.Buff1, RuinsofLordaeronObjectTypes.Buff1, 1328.719971f, 1632.719971f, 36.730400f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
            result &= AddObject(RuinsofLordaeronObjectTypes.Buff2, RuinsofLordaeronObjectTypes.Buff2, 1243.300049f, 1699.170044f, 34.872601f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "RuinsofLordaeronArena: Failed to spawn buff object!");
                return false;
            }

            return true;
        }

        public override void StartingEventCloseDoors()
        {
            for (int i = RuinsofLordaeronObjectTypes.Door1; i <= RuinsofLordaeronObjectTypes.Door2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = RuinsofLordaeronObjectTypes.Door1; i <= RuinsofLordaeronObjectTypes.Door2; ++i)
                DoorOpen(i);

            taskScheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                for (int i = RuinsofLordaeronObjectTypes.Door1; i <= RuinsofLordaeronObjectTypes.Door2; ++i)
                    DelObject(i);
            });

            for (int i = RuinsofLordaeronObjectTypes.Buff1; i <= RuinsofLordaeronObjectTypes.Buff2; ++i)
                SpawnBGObject(i, 60);
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch (trigger)
            {
                case 4696:                                          // buff trigger?
                case 4697:                                          // buff trigger?
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }
    }

    struct RuinsofLordaeronObjectTypes
    {
        public const int Door1 = 0;
        public const int Door2 = 1;
        public const int Buff1 = 2;
        public const int Buff2 = 3;
        public const int Max = 4;
    }

    struct RuinsofLordaeronGameObjects
    {
        public const uint Door1 = 185918;
        public const uint Door2 = 185917;
        public const uint Buff1 = 184663;
        public const uint Buff2 = 184664;
    }
}
