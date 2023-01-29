// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class WorldObjectChangeAccumulator : Notifier
    {
        private readonly List<ObjectGuid> _plr_list = new();

        private readonly Dictionary<Player, UpdateData> _updateData;
        private readonly WorldObject _worldObject;

        public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d)
        {
            _updateData = d;
            _worldObject = obj;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                BuildPacket(player);

                if (!player.GetSharedVisionList().Empty())
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        BuildPacket(visionPlayer);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (!creature.GetSharedVisionList().Empty())
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        BuildPacket(visionPlayer);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                ObjectGuid guid = dynamicObject.GetCasterGUID();

                if (guid.IsPlayer())
                {
                    //Caster may be NULL if DynObj is in removelist
                    Player caster = Global.ObjAccessor.FindPlayer(guid);

                    if (caster != null)
                        if (caster.ActivePlayerData.FarsightObject == dynamicObject.GetGUID())
                            BuildPacket(caster);
                }
            }
        }

        private void BuildPacket(Player player)
        {
            // Only send update once to a player
            if (!_plr_list.Contains(player.GetGUID()) &&
                player.HaveAtClient(_worldObject))
            {
                _worldObject.BuildFieldsUpdate(player, _updateData);
                _plr_list.Add(player.GetGUID());
            }
        }
    }
}