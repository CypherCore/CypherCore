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
using Framework.IO;

namespace Game.Chat.Commands
{
    //Holder for now.
    [CommandGroup("ahbot", RBACPermissions.CommandAhbot)]
    class AhBotCommands
    {
        [Command("rebuild", RBACPermissions.CommandAhbotRebuild, true)]
        static bool HandleAHBotRebuildCommand(StringArguments args, CommandHandler handler)
        {
            /*char* arg = strtok((char*)args, " ");

            bool all = false;
            if (arg && strcmp(arg, "all") == 0)
                all = true;

            sAuctionBot->Rebuild(all);*/
            return true;
        }

        [Command("reload", RBACPermissions.CommandAhbotReload, true)]
        static bool HandleAHBotReloadCommand(StringArguments args, CommandHandler handler)
        {
            //sAuctionBot->ReloadAllConfig();
            //handler->SendSysMessage(LANG_AHBOT_RELOAD_OK);
            return true;
        }

        [Command("status", RBACPermissions.CommandAhbotStatus, true)]
        static bool HandleAHBotStatusCommand(StringArguments args, CommandHandler handler)
        {
            /*   char* arg = strtok((char*)args, " ");
            if (!arg)
                return false;

            bool all = false;
            if (strcmp(arg, "all") == 0)
                all = true;

            AuctionHouseBotStatusInfo statusInfo;
            sAuctionBot->PrepareStatusInfos(statusInfo);

            WorldSession* session = handler->GetSession();

            if (!session)
            {
                handler->SendSysMessage(LANG_AHBOT_STATUS_BAR_CONSOLE);
                handler->SendSysMessage(LANG_AHBOT_STATUS_TITLE1_CONSOLE);
                handler->SendSysMessage(LANG_AHBOT_STATUS_MIDBAR_CONSOLE);
            }
            else
                handler->SendSysMessage(LANG_AHBOT_STATUS_TITLE1_CHAT);

            public uint fmtId = session ? LANG_AHBOT_STATUS_FORMAT_CHAT : LANG_AHBOT_STATUS_FORMAT_CONSOLE;

            handler->PSendSysMessage(fmtId, handler->GetCypherString(LANG_AHBOT_STATUS_ITEM_COUNT),
                statusInfo[AUCTION_HOUSE_ALLIANCE].ItemsCount,
                statusInfo[AUCTION_HOUSE_HORDE].ItemsCount,
                statusInfo[AUCTION_HOUSE_NEUTRAL].ItemsCount,
                statusInfo[AUCTION_HOUSE_ALLIANCE].ItemsCount +
                statusInfo[AUCTION_HOUSE_HORDE].ItemsCount +
                statusInfo[AUCTION_HOUSE_NEUTRAL].ItemsCount);

            if (all)
            {
                handler->PSendSysMessage(fmtId, handler->GetCypherString(LANG_AHBOT_STATUS_ITEM_RATIO),
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_ALLIANCE_ITEM_AMOUNT_RATIO),
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_HORDE_ITEM_AMOUNT_RATIO),
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_NEUTRAL_ITEM_AMOUNT_RATIO),
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_ALLIANCE_ITEM_AMOUNT_RATIO) +
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_HORDE_ITEM_AMOUNT_RATIO) +
                    sAuctionBotConfig->GetConfig(CONFIG_AHBOT_NEUTRAL_ITEM_AMOUNT_RATIO));

                if (!session)
                {
                    handler->SendSysMessage(LANG_AHBOT_STATUS_BAR_CONSOLE);
                    handler->SendSysMessage(LANG_AHBOT_STATUS_TITLE2_CONSOLE);
                    handler->SendSysMessage(LANG_AHBOT_STATUS_MIDBAR_CONSOLE);
                }
                else
                    handler->SendSysMessage(LANG_AHBOT_STATUS_TITLE2_CHAT);

                for (int i = 0; i < MAX_AUCTION_QUALITY; ++i)
                    handler->PSendSysMessage(fmtId, handler->GetCypherString(ahbotQualityIds[i]),
                        statusInfo[AUCTION_HOUSE_ALLIANCE].QualityInfo[i],
                        statusInfo[AUCTION_HOUSE_HORDE].QualityInfo[i],
                        statusInfo[AUCTION_HOUSE_NEUTRAL].QualityInfo[i],
                        sAuctionBotConfig->GetConfigItemQualityAmount(AuctionQuality(i)));
            }

            if (!session)
                handler->SendSysMessage(LANG_AHBOT_STATUS_BAR_CONSOLE);
            */
            return true;
        }

        [CommandGroup("items", RBACPermissions.CommandAhbotItems)]
        class ItemsCommands
        {
            [Command("", RBACPermissions.CommandAhbotItems, true)]
            static bool HandleAHBotItemsAmountCommand(StringArguments args, CommandHandler handler)
            {
                /*public uint qVals[MAX_AUCTION_QUALITY];
                char* arg = strtok((char*)args, " ");
                for (int i = 0; i < MAX_AUCTION_QUALITY; ++i)
                {
                    if (!arg)
                        return false;
                    qVals[i] = atoi(arg);
                    arg = strtok(NULL, " ");
                }

                sAuctionBot->SetItemsAmount(qVals);

                for (int i = 0; i < MAX_AUCTION_QUALITY; ++i)
                    handler->PSendSysMessage(LANG_AHBOT_ITEMS_AMOUNT, handler->GetCypherString(ahbotQualityIds[i]), sAuctionBotConfig->GetConfigItemQualityAmount(AuctionQuality(i)));
                */
                return true;
            }

            [Command("blue", RBACPermissions.CommandAhbotItemsBlue, true)]
            static bool HandleAHBotItemsAmountQualityBlue(StringArguments args, CommandHandler handler) { return true; }

            [Command("gray", RBACPermissions.CommandAhbotItemsGray, true)]
            static bool HandleAHBotItemsAmountQualityGray(StringArguments args, CommandHandler handler) { return true; }

            [Command("green", RBACPermissions.CommandAhbotItemsGreen, true)]
            static bool HandleAHBotItemsAmountQualityGreen(StringArguments args, CommandHandler handler) { return true; }

            [Command("orange", RBACPermissions.CommandAhbotItemsOrange, true)]
            static bool HandleAHBotItemsAmountQualityOrange(StringArguments args, CommandHandler handler) { return true; }

            [Command("purple", RBACPermissions.CommandAhbotItemsPurple, true)]
            static bool HandleAHBotItemsAmountQualityPurple(StringArguments args, CommandHandler handler) { return true; }

            [Command("white", RBACPermissions.CommandAhbotItemsWhite, true)]
            static bool HandleAHBotItemsAmountQualityWhite(StringArguments args, CommandHandler handler) { return true; }

            [Command("yellow", RBACPermissions.CommandAhbotItemsYellow, true)]
            static bool HandleAHBotItemsAmountQualityYellow(StringArguments args, CommandHandler handler) { return true; }

            static bool HandleAHBotItemsAmountQualityCommand(StringArguments args, CommandHandler handler)
            {
                /*
                char* arg = strtok((char*)args, " ");
                if (!arg)
                    return false;
                public uint qualityVal = atoi(arg);

                sAuctionBot->SetItemsAmountForQuality(Q, qualityVal);
                handler->PSendSysMessage(LANG_AHBOT_ITEMS_AMOUNT, handler->GetCypherString(ahbotQualityIds[Q]),
                    sAuctionBotConfig->GetConfigItemQualityAmount(Q));
                */
                return true;
            }
        }

        [CommandGroup("ratio", RBACPermissions.CommandAhbotRatio)]
        class RatioCommands
        {
            [Command("", RBACPermissions.CommandAhbotRatio, true)]
            static bool HandleAHBotItemsRatioCommand(StringArguments args, CommandHandler handler)
            {
                /*public uint rVal[MAX_AUCTION_QUALITY];
                char* arg = strtok((char*)args, " ");
                for (int i = 0; i < MAX_AUCTION_QUALITY; ++i)
                {
                    if (!arg)
                        return false;
                    rVal[i] = atoi(arg);
                    arg = strtok(NULL, " ");
                }

                sAuctionBot->SetItemsRatio(rVal[0], rVal[1], rVal[2]);

                for (int i = 0; i < MAX_AUCTION_HOUSE_TYPE; ++i)
                    handler->PSendSysMessage(LANG_AHBOT_ITEMS_RATIO, AuctionBotConfig::GetHouseTypeName(AuctionHouseType(i)), sAuctionBotConfig->GetConfigItemAmountRatio(AuctionHouseType(i)));
                */
                return true;
            }

            [Command("alliance", RBACPermissions.CommandAhbotRatioAlliance, true)]
            static bool HandleAHBotItemsRatioHouseAlliance(StringArguments args, CommandHandler handler) { return true; }

            [Command("horde", RBACPermissions.CommandAhbotRatioHorde, true)]
            static bool HandleAHBotItemsRatioHouseHorde(StringArguments args, CommandHandler handler) { return true; }

            [Command("neutral", RBACPermissions.CommandAhbotRatioNeutral, true)]
            static bool HandleAHBotItemsRatioHouseNeutral(StringArguments args, CommandHandler handler) { return true; }

            static bool HandleAHBotItemsRatioHouseCommand(StringArguments args, CommandHandler handler)
            {
                /*char* arg = strtok((char*)args, " ");
                if (!arg)
                    return false;
                public uint ratioVal = atoi(arg);

                sAuctionBot->SetItemsRatioForHouse(H, ratioVal);
                handler->PSendSysMessage(LANG_AHBOT_ITEMS_RATIO, AuctionBotConfig::GetHouseTypeName(H), sAuctionBotConfig->GetConfigItemAmountRatio(H));
                */
                return true;
            }
        }
    }
}
