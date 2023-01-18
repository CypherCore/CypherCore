// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;

namespace Game.Chat.Commands
{
    //Holder for now.
    [CommandGroup("ahbot")]
    class AhBotCommands
    {
        [Command("rebuild", RBACPermissions.CommandAhbotRebuild, true)]
        static bool HandleAHBotRebuildCommand(CommandHandler handler, StringArguments args)
        {
            /*char* arg = strtok((char*)args, " ");

            bool all = false;
            if (arg && strcmp(arg, "all") == 0)
                all = true;

            sAuctionBot->Rebuild(all);*/
            return true;
        }

        [Command("reload", RBACPermissions.CommandAhbotReload, true)]
        static bool HandleAHBotReloadCommand(CommandHandler handler, StringArguments args)
        {
            //sAuctionBot->ReloadAllConfig();
            //handler->SendSysMessage(LANG_AHBOT_RELOAD_OK);
            return true;
        }

        [Command("status", RBACPermissions.CommandAhbotStatus, true)]
        static bool HandleAHBotStatusCommand(CommandHandler handler, StringArguments args)
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

        [CommandGroup("items")]
        class ItemsCommands
        {
            [Command("", RBACPermissions.CommandAhbotItems, true)]
            static bool HandleAHBotItemsAmountCommand(CommandHandler handler, StringArguments args)
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
            static bool HandleAHBotItemsAmountQualityBlue(CommandHandler handler, StringArguments args) { return true; }

            [Command("gray", RBACPermissions.CommandAhbotItemsGray, true)]
            static bool HandleAHBotItemsAmountQualityGray(CommandHandler handler, StringArguments args) { return true; }

            [Command("green", RBACPermissions.CommandAhbotItemsGreen, true)]
            static bool HandleAHBotItemsAmountQualityGreen(CommandHandler handler, StringArguments args) { return true; }

            [Command("orange", RBACPermissions.CommandAhbotItemsOrange, true)]
            static bool HandleAHBotItemsAmountQualityOrange(CommandHandler handler, StringArguments args) { return true; }

            [Command("purple", RBACPermissions.CommandAhbotItemsPurple, true)]
            static bool HandleAHBotItemsAmountQualityPurple(CommandHandler handler, StringArguments args) { return true; }

            [Command("white", RBACPermissions.CommandAhbotItemsWhite, true)]
            static bool HandleAHBotItemsAmountQualityWhite(CommandHandler handler, StringArguments args) { return true; }

            [Command("yellow", RBACPermissions.CommandAhbotItemsYellow, true)]
            static bool HandleAHBotItemsAmountQualityYellow(CommandHandler handler, StringArguments args) { return true; }

            static bool HandleAHBotItemsAmountQualityCommand(CommandHandler handler, StringArguments args)
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

        [CommandGroup("ratio")]
        class RatioCommands
        {
            [Command("", RBACPermissions.CommandAhbotRatio, true)]
            static bool HandleAHBotItemsRatioCommand(CommandHandler handler, StringArguments args)
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
            static bool HandleAHBotItemsRatioHouseAlliance(CommandHandler handler, StringArguments args) { return true; }

            [Command("horde", RBACPermissions.CommandAhbotRatioHorde, true)]
            static bool HandleAHBotItemsRatioHouseHorde(CommandHandler handler, StringArguments args) { return true; }

            [Command("neutral", RBACPermissions.CommandAhbotRatioNeutral, true)]
            static bool HandleAHBotItemsRatioHouseNeutral(CommandHandler handler, StringArguments args) { return true; }

            static bool HandleAHBotItemsRatioHouseCommand(CommandHandler handler, StringArguments args)
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
