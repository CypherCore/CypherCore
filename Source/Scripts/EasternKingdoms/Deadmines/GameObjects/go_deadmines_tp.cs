// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects
{
    [GameObjectScript(19072)]
    public class go_deadmines_tp : GameObjectAI
    {
        public const string GOSSIP_BOSS_1 = "Press the button labeled 'Wood and Lumber.'";
        public const string GOSSIP_BOSS_2 = "Press the button labeled 'Metal and Scraps.'";
        public const string GOSSIP_BOSS_3 = "Press the button labeled 'Ship Parts.'";

        public go_deadmines_tp(GameObject go) : base(go)
        {
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint action)
        {
            if (player.HasAura(DMSharedSpells.NIGHTMARE_ELIXIR))
            {
                return false;
            }

            player.PlayerTalkClass.ClearMenus();
            player.CloseGossipMenu();
            switch (action)
            {
                case GossipAction.GOSSIP_ACTION_INFO_DEF:
                    player.TeleportTo(player.GetMapId(), -305.32f, -491.29f, 49.23f, 3.14f);
                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 1:
                    player.TeleportTo(player.GetMapId(), -201.09f, -606.04f, 19.30f, 3.14f);
                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 2:
                    player.TeleportTo(player.GetMapId(), -129.91f, -788.89f, 17.34f, 3.14f);
                    break;
            }
            return true;
        }

        public override bool OnGossipHello(Player player)
        {
            if (player.HasAura(DMSharedSpells.NIGHTMARE_ELIXIR))
            {
                return false;
            }

            InstanceScript instance = me.GetInstanceScript();
            if (instance == null)
            {
                return false;
            }

            if (instance.GetBossState(DMData.DATA_HELIX) == EncounterState.Done)
            {
                player.AddGossipItem(GossipOptionNpc.None, GOSSIP_BOSS_1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF);
            }
            if (instance.GetBossState(DMData.DATA_FOEREAPER) == EncounterState.Done)
            {
                player.AddGossipItem(GossipOptionNpc.None, GOSSIP_BOSS_2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
            }
            if (instance.GetBossState(DMData.DATA_RIPSNARL) == EncounterState.Done)
            {
                player.AddGossipItem(GossipOptionNpc.None, GOSSIP_BOSS_3, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);
            }
            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }
    }
}
