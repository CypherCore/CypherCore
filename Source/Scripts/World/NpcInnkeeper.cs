// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.AI;
using Framework.Constants;

namespace Scripts.World.NpcInnkeeper
{
    struct SpellIds
    {
        public const uint TrickOrTreated = 24755;
        public const uint Treat = 24715;
    }

    struct Gossip
    {
        public const uint MenuId = 9733;
        public const uint MenuEventId = 342;
    }

    [Script]
    class npc_innkeeper : ScriptedAI
    {
        public npc_innkeeper(Creature creature) : base(creature) { }

        public override bool OnGossipHello(Player player)
        {
            player.InitGossipMenu(Gossip.MenuId);
            if (Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) && !player.HasAura(SpellIds.TrickOrTreated))
                player.AddGossipItem(Gossip.MenuEventId, 0, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            if (me.IsQuestGiver())
                player.PrepareQuestMenu(me.GetGUID());

            if (me.IsVendor())
                player.AddGossipItem(Gossip.MenuId, 2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsInnkeeper())
                player.AddGossipItem(Gossip.MenuId, 1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInn);

            player.TalkedToCreature(me.GetEntry(), me.GetGUID());
            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            if (action == eTradeskill.GossipActionInfoDef + 1 && Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) && !player.HasAura(SpellIds.TrickOrTreated))
            {
                player.CastSpell(player, SpellIds.TrickOrTreated, true);

                if (RandomHelper.IRand(0, 1) != 0)
                    player.CastSpell(player, SpellIds.Treat, true);
                else
                {
                    uint trickspell = 0;
                    switch (RandomHelper.IRand(0, 13))
                    {
                        case 0:
                            trickspell = 24753;
                            break; // cannot cast, random 30sec
                        case 1:
                            trickspell = 24713;
                            break; // lepper gnome costume
                        case 2:
                            trickspell = 24735;
                            break; // male ghost costume
                        case 3:
                            trickspell = 24736;
                            break; // female ghostcostume
                        case 4:
                            trickspell = 24710;
                            break; // male ninja costume
                        case 5:
                            trickspell = 24711;
                            break; // female ninja costume
                        case 6:
                            trickspell = 24708;
                            break; // male pirate costume
                        case 7:
                            trickspell = 24709;
                            break; // female pirate costume
                        case 8:
                            trickspell = 24723;
                            break; // skeleton costume
                        case 9:
                            trickspell = 24753;
                            break; // Trick
                        case 10:
                            trickspell = 24924;
                            break; // Hallow's End Candy
                        case 11:
                            trickspell = 24925;
                            break; // Hallow's End Candy
                        case 12:
                            trickspell = 24926;
                            break; // Hallow's End Candy
                        case 13:
                            trickspell = 24927;
                            break; // Hallow's End Candy
                    }
                    player.CastSpell(player, trickspell, true);
                }
                player.CloseGossipMenu();
                return true;
            }

            player.CloseGossipMenu();

            switch (action)
            {
                case eTradeskill.GossipActionTrade:
                    player.GetSession().SendListInventory(me.GetGUID());
                    break;
                case eTradeskill.GossipActionInn:
                    player.SetBindPoint(me.GetGUID());
                    break;
            }
            return true;
        }
    }
}

