// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
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
            if (item == null)
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

            if (_collectionMgr.AddToy(item.GetEntry(), false, false))
                _player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
        }

        [WorldPacketHandler(ClientOpcodes.UseToy, Processing = PacketProcessing.Inplace)]
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

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(packet.Cast.SpellID, Difficulty.None);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "HandleUseToy: unknown spell id: {0} used by Toy Item entry {1}", packet.Cast.SpellID, itemId);
                return;
            }

            if (_player.IsPossessing())
                return;

            SpellCastTargets targets = new(_player, packet.Cast);

            Spell spell = new(_player, spellInfo, TriggerCastFlags.None);

            SpellPrepare spellPrepare = new();
            spellPrepare.ClientCastID = packet.Cast.CastID;
            spellPrepare.ServerCastID = spell.m_castId;
            SendPacket(spellPrepare);

            spell.m_fromClient = true;
            spell.m_castItemEntry = itemId;
            spell.m_misc.Data0 = packet.Cast.Misc[0];
            spell.m_misc.Data1 = packet.Cast.Misc[1];
            spell.m_castFlagsEx |= SpellCastFlagsEx.UseToySpell;
            spell.Prepare(targets);
        }

        [WorldPacketHandler(ClientOpcodes.ToyClearFanfare)]
        void HandleToyClearFanfare(ToyClearFanfare toyClearFanfare)
        {
            _collectionMgr.ToyClearFanfare(toyClearFanfare.ItemID);
        }
    }
}
