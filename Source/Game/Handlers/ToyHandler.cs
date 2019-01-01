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
using Game.Network;
using Game.Network.Packets;
using Game.Spells;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AddToy)]
        void HandleAddToy(AddToy packet)
        {
            if (packet.Guid.IsEmpty())
                return;

            Item item = _player.GetItemByGuid(packet.Guid);
            if (!item)
            {
                _player.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            if (!Global.DB2Mgr.IsToyItem(item.GetEntry()))
                return;

            InventoryResult msg = _player.CanUseItem(item);
            if (msg != InventoryResult.Ok)
            {
                _player.SendEquipError(msg, item);
                return;
            }

            if (_collectionMgr.AddToy(item.GetEntry(), false))
                _player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
        }

        [WorldPacketHandler(ClientOpcodes.UseToy)]
        void HandleUseToy(UseToy packet)
        {
            uint itemId = packet.Cast.Misc[0];
            ItemTemplate item = Global.ObjectMgr.GetItemTemplate(itemId);
            if (item == null)
                return;

            if (!_collectionMgr.HasToy(itemId))
                return;

            var effect = item.Effects.Find(eff => packet.Cast.SpellID == eff.SpellID);
            if (effect == null)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(packet.Cast.SpellID);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "HandleUseToy: unknown spell id: {0} used by Toy Item entry {1}", packet.Cast.SpellID, itemId);
                return;
            }

            if (_player.isPossessing())
                return;

            SpellCastTargets targets = new SpellCastTargets(_player, packet.Cast);

            Spell spell = new Spell(_player, spellInfo, TriggerCastFlags.None, ObjectGuid.Empty, false);

            SpellPrepare spellPrepare = new SpellPrepare();
            spellPrepare.ClientCastID = packet.Cast.CastID;
            spellPrepare.ServerCastID = spell.m_castId;
            SendPacket(spellPrepare);

            spell.m_fromClient = true;
            spell.m_castItemEntry = itemId;
            spell.m_misc.Data0 = packet.Cast.Misc[0];
            spell.m_misc.Data1 = packet.Cast.Misc[1];
            spell.m_castFlagsEx |= SpellCastFlagsEx.UseToySpell;
            spell.prepare(targets);
        }
    }
}
