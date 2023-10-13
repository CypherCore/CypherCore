// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using static Global;

namespace Scripts.World.NpcInnkeeper
{
    [Script]
    class npc_innkeeper : ScriptedAI
    {
        const uint SpellTrickOrTreated = 24755;
        const uint SpellTreat = 24715;

        const uint NpcGossipMenu = 9733;
        const uint NpcGossipMenuEvent = 342;

        public npc_innkeeper(Creature creature) : base(creature) { }

        public override bool OnGossipHello(Player player)
        {
            player.InitGossipMenu(NpcGossipMenu);
            if (GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) && !player.HasAura(SpellTrickOrTreated))
                player.AddGossipItem(NpcGossipMenuEvent, 0, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            if (me.IsQuestGiver())

                if (me.IsVendor())
                    player.AddGossipItem(NpcGossipMenu, 2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsInnkeeper())
                player.AddGossipItem(NpcGossipMenu, 1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInn);

            player.TalkedToCreature(me.GetEntry(), me.GetGUID());
            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            if (action == eTradeskill.GossipActionInfoDef + 1 && GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) && !player.HasAura(SpellTrickOrTreated))
            {
                player.CastSpell(player, SpellTrickOrTreated, true);

                if (RandomHelper.URand(0, 1) != 0)
                    player.CastSpell(player, SpellTreat, true);
                else
                {
                    uint trickspell = 0;
                    switch (RandomHelper.URand(0, 13))
                    {
                        case 0: trickspell = 24753; break; // cannot cast, random TimeSpan.FromSeconds(30)ec
                        case 1: trickspell = 24713; break; // lepper gnome Costume
                        case 2: trickspell = 24735; break; // male ghost Costume
                        case 3: trickspell = 24736; break; // female ghostCostume
                        case 4: trickspell = 24710; break; // male ninja Costume
                        case 5: trickspell = 24711; break; // female ninja Costume
                        case 6: trickspell = 24708; break; // male pirate Costume
                        case 7: trickspell = 24709; break; // female pirate Costume
                        case 8: trickspell = 24723; break; // skeleton Costume
                        case 9: trickspell = 24753; break; // Trick
                        case 10: trickspell = 24924; break; // Hallow's End Candy
                        case 11: trickspell = 24925; break; // Hallow's End Candy
                        case 12: trickspell = 24926; break; // Hallow's End Candy
                        case 13: trickspell = 24927; break; // Hallow's End Candy
                    }
                    player.CastSpell(player, trickspell, true);
                }
                player.ClearGossipMenu();
                return true;
            }

            player.ClearGossipMenu();

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

