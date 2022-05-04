/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
        public const string LocaleTrickOrTreat0 = "Trick or Treat!";
        public const string LocaleTrickOrTreat2 = "Des bonbons ou des blagues!";
        public const string LocaleTrickOrTreat3 = "Süßes oder Saures!";
        public const string LocaleTrickOrTreat6 = "¡Truco o trato!";

        public const string LocaleInnkeeper0 = "Make this inn my home.";
        public const string LocaleInnkeeper2 = "Faites de cette auberge votre foyer.";
        public const string LocaleInnkeeper3 = "Ich möchte dieses Gasthaus zu meinem Heimatort machen.";
        public const string LocaleInnkeeper6 = "Fija tu hogar en esta taberna.";

        public const string LocaleVendor0 = "I want to browse your goods.";
        public const string LocaleVendor2 = "Je voudrais regarder vos articles.";
        public const string LocaleVendor3 = "Ich sehe mich nur mal um.";
        public const string LocaleVendor6 = "Quiero ver tus mercancías.";
    }

    [Script]
    class npc_innkeeper : ScriptedAI
    {
        public npc_innkeeper(Creature creature) : base(creature) { }

        public override bool OnGossipHello(Player player)
        {
            if (Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) && !player.HasAura(SpellIds.TrickOrTreated))
            {
                string localizedEntry;
                switch (player.GetSession().GetSessionDbcLocale())
                {
                    case Locale.frFR:
                        localizedEntry = Gossip.LocaleTrickOrTreat2;
                        break;
                    case Locale.deDE:
                        localizedEntry = Gossip.LocaleTrickOrTreat3;
                        break;
                    case Locale.esES:
                        localizedEntry = Gossip.LocaleTrickOrTreat6;
                        break;
                    case Locale.enUS:
                    default: 
                        localizedEntry = Gossip.LocaleTrickOrTreat0;
                        break;
                }
                player.AddGossipItem(GossipOptionIcon.None, localizedEntry, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
            }

            if (me.IsQuestGiver())

                if (me.IsVendor())
                {
                    string localizedEntry;
                    switch (player.GetSession().GetSessionDbcLocale())
                    {
                        case Locale.frFR: 
                            localizedEntry = Gossip.LocaleVendor2;
                            break;
                        case Locale.deDE: 
                            localizedEntry = Gossip.LocaleVendor3;
                            break;
                        case Locale.esES: 
                            localizedEntry = Gossip.LocaleVendor6;
                            break;
                        case Locale.enUS:
                        default: localizedEntry = Gossip.LocaleVendor0;
                            break;
                    }
                    player.AddGossipItem(GossipOptionIcon.Vendor, localizedEntry, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);
                }

            if (me.IsInnkeeper())
            {
                string localizedEntry;
                switch (player.GetSession().GetSessionDbcLocale())
                {
                    case Locale.frFR: 
                        localizedEntry = Gossip.LocaleInnkeeper2;
                        break;
                    case Locale.deDE: 
                        localizedEntry = Gossip.LocaleInnkeeper3;
                        break;
                    case Locale.esES:
                        localizedEntry = Gossip.LocaleInnkeeper6;
                        break;
                    case Locale.enUS:
                    default: localizedEntry = Gossip.LocaleInnkeeper0;
                        break;
                }
                player.AddGossipItem(GossipOptionIcon.Binder, localizedEntry, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInn);
            }

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

