// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Maps.Notifiers
{
    public class VisibleNotifier : Notifier
    {
        internal UpdateData _i_data;

        internal Player _i_player;
        internal List<Unit> _i_visibleNow;
        internal List<ObjectGuid> _vis_guids;

        public VisibleNotifier(Player pl)
        {
            _i_player = pl;
            _i_data = new UpdateData(pl.GetMapId());
            _vis_guids = new List<ObjectGuid>(pl.ClientGUIDs);
            _i_visibleNow = new List<Unit>();
        }

        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                _vis_guids.Remove(obj.GetGUID());
                _i_player.UpdateVisibilityOf(obj, _i_data, _i_visibleNow);
            }
        }

        public void SendToSelf()
        {
            // at this moment i_clientGUIDs have guids that not iterate at grid level checks
            // but exist one case when this possible and object not out of range: transports
            Transport transport = _i_player.GetTransport<Transport>();

            if (transport)
                foreach (var obj in transport.GetPassengers())
                    if (_vis_guids.Contains(obj.GetGUID()))
                    {
                        _vis_guids.Remove(obj.GetGUID());

                        switch (obj.GetTypeId())
                        {
                            case TypeId.GameObject:
                                _i_player.UpdateVisibilityOf(obj.ToGameObject(), _i_data, _i_visibleNow);

                                break;
                            case TypeId.Player:
                                _i_player.UpdateVisibilityOf(obj.ToPlayer(), _i_data, _i_visibleNow);

                                if (!obj.IsNeedNotify(NotifyFlags.VisibilityChanged))
                                    obj.ToPlayer().UpdateVisibilityOf(_i_player);

                                break;
                            case TypeId.Unit:
                                _i_player.UpdateVisibilityOf(obj.ToCreature(), _i_data, _i_visibleNow);

                                break;
                            case TypeId.DynamicObject:
                                _i_player.UpdateVisibilityOf(obj.ToDynamicObject(), _i_data, _i_visibleNow);

                                break;
                            case TypeId.AreaTrigger:
                                _i_player.UpdateVisibilityOf(obj.ToAreaTrigger(), _i_data, _i_visibleNow);

                                break;
                            default:
                                break;
                        }
                    }

            foreach (var guid in _vis_guids)
            {
                _i_player.ClientGUIDs.Remove(guid);
                _i_data.AddOutOfRangeGUID(guid);

                if (guid.IsPlayer())
                {
                    Player pl = Global.ObjAccessor.FindPlayer(guid);

                    if (pl != null &&
                        pl.IsInWorld &&
                        !pl.IsNeedNotify(NotifyFlags.VisibilityChanged))
                        pl.UpdateVisibilityOf(_i_player);
                }
            }

            if (!_i_data.HasData())
                return;

            UpdateObject packet;
            _i_data.BuildPacket(out packet);
            _i_player.SendPacket(packet);

            foreach (var obj in _i_visibleNow)
                _i_player.SendInitialVisiblePackets(obj);
        }
    }
}