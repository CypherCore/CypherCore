// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Networking.Packets;

namespace Game.Entities
{
    public partial class Player
    {
        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly ActivePlayerData _activePlayerMask = new();
            private readonly ObjectFieldData _objectMask = new();
            private readonly Player _owner;
            private readonly PlayerData _playerMask = new();
            private readonly UnitData _unitMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Player owner)
            {
                _owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(_owner.GetMapId());

                _owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _unitMask.GetUpdateMask(), _playerMask.GetUpdateMask(), _activePlayerMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}