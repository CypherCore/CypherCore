// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.BladesEdge
{
    enum GameObjectIds
    {
        Door1 = 183971,
        Door2 = 183973,
        Door3 = 183970,
        Door4 = 183972,
        Buff1 = 184663,
        Buff2 = 184664
    }

    [Script(nameof(arena_blades_edge), 1672)]
    class arena_blades_edge : ArenaScript
    {
        List<ObjectGuid> _doorGUIDs = new();

        public arena_blades_edge(BattlegroundMap map) : base(map) { }

        public override void OnUpdate(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void OnInit()
        {
            AddDoor(GameObjectIds.Door1, 6287.277f, 282.1877f, 3.810925f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);
            AddDoor(GameObjectIds.Door2, 6189.546f, 241.7099f, 3.101481f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
            AddDoor(GameObjectIds.Door3, 6299.116f, 296.5494f, 3.308032f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
            AddDoor(GameObjectIds.Door4, 6177.708f, 227.3481f, 3.604374f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);
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
                CreateObject((uint)GameObjectIds.Buff1, 6249.042f, 275.3239f, 11.22033f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f);
                CreateObject((uint)GameObjectIds.Buff2, 6228.26f, 249.566f, 11.21812f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f);
            });
        }

        void AddDoor(GameObjectIds entry, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3, GameObjectState goState = GameObjectState.Ready)
        {
            GameObject go = CreateObject((uint)entry, x, y, z, o, rotation0, rotation1, rotation2, rotation3, goState);
            if (go != null)
                _doorGUIDs.Add(go.GetGUID());
        }
    }
}
