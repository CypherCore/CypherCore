// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.NagrandArena
{
    enum GameObjectIds
    {
        Door1 = 183978,
        Door2 = 183980,
        Door3 = 183977,
        Door4 = 183979,
        Buff1 = 184663,
        Buff2 = 184664
    }

    [Script(nameof(arena_nagrand), 1505)]
    class arena_nagrand : ArenaScript
    {
        List<ObjectGuid> _doorGUIDs = new();

        public arena_nagrand(BattlegroundMap map) : base(map) { }

        public override void OnUpdate(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void OnInit()
        {
            AddDoor(GameObjectIds.Door1, 4031.854f, 2966.833f, 12.6462f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f);
            AddDoor(GameObjectIds.Door2, 4081.179f, 2874.97f, 12.39171f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f);
            AddDoor(GameObjectIds.Door3, 4023.709f, 2981.777f, 10.70117f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f);
            AddDoor(GameObjectIds.Door4, 4090.064f, 2858.438f, 10.23631f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f);
        }

        public override void OnStart()
        {
            foreach (ObjectGuid guid in _doorGUIDs)
            {
                GameObject door = battlegroundMap.GetGameObject(guid);
                if (door != null)
                {
                    door.UseDoorOrButton();
                    door.DespawnOrUnsummon(TimeSpan.FromMinutes(5));
                }
            }

            _scheduler.Schedule(TimeSpan.FromMinutes(1), _ =>
            {
                CreateObject((uint)GameObjectIds.Buff1, 4009.189941f, 2895.250000f, 13.052700f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f);
                CreateObject((uint)GameObjectIds.Buff2, 4103.330078f, 2946.350098f, 13.051300f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f);
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
