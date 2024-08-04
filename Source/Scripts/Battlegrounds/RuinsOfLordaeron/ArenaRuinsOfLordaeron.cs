// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.RuinsOfLordaeron
{
    enum GameObjectIds
    {
        Door1 = 185918,
        Door2 = 185917,
        Buff1 = 184663,
        Buff2 = 184664
    }

    [Script(nameof(arena_ruins_of_lordaeron), 572)]
    class arena_ruins_of_lordaeron : ArenaScript
    {
        List<ObjectGuid> _doorGUIDs = new();

        public arena_ruins_of_lordaeron(BattlegroundMap map) : base(map) { }

        public override void OnUpdate(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void OnInit()
        {
            AddDoor((uint)GameObjectIds.Door1, 1293.561f, 1601.938f, 31.60557f, -1.457349f, 0, 0, -0.6658813f, 0.7460576f);
            AddDoor((uint)GameObjectIds.Door2, 1278.648f, 1730.557f, 31.60557f, 1.684245f, 0, 0, 0.7460582f, 0.6658807f);
        }

        public override void OnStart()
        {
            foreach (ObjectGuid guid in _doorGUIDs)
            {
                GameObject door = battlegroundMap.GetGameObject(guid);
                if (door != null)
                {
                    door.UseDoorOrButton();
                    door.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                }
            }

            _scheduler.Schedule(TimeSpan.FromMinutes(1), _ =>
            {
                CreateObject((uint)GameObjectIds.Buff1, 1328.719971f, 1632.719971f, 36.730400f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f);
                CreateObject((uint)GameObjectIds.Buff2, 1243.300049f, 1699.170044f, 34.872601f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f);
            });
        }

        void AddDoor(uint entry, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3, GameObjectState goState = GameObjectState.Ready)
        {
            GameObject go = CreateObject(entry, x, y, z, o, rotation0, rotation1, rotation2, rotation3, goState);
            if (go != null)
                _doorGUIDs.Add(go.GetGUID());
        }
    }
}