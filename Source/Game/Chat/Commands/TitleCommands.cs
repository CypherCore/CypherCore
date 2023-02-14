// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;
using System;

namespace Game.Chat.Commands
{
    [CommandGroup("titles")]
    class TitleCommands
    {
        [Command("current", RBACPermissions.CommandTitlesCurrent)]
        static bool HandleTitlesCurrentCommand(CommandHandler handler, uint titleId)
        {
            Player target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);
                return false;
            }

            string tNameLink = handler.GetNameLink(target);
            string titleNameStr = string.Format(target.GetNativeGender() == Gender.Male ? titleInfo.Name[handler.GetSessionDbcLocale()] : titleInfo.Name1[handler.GetSessionDbcLocale()].ConvertFormatSyntax(), target.GetName());

            target.SetTitle(titleInfo);
            target.SetChosenTitle(titleInfo.MaskID);

            handler.SendSysMessage(CypherStrings.TitleCurrentRes, titleId, titleNameStr, tNameLink);
            return true;
        }

        [Command("add", RBACPermissions.CommandTitlesAdd)]
        static bool HandleTitlesAddCommand(CommandHandler handler, uint titleId)
        {
            Player target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);
                return false;
            }

            string tNameLink = handler.GetNameLink(target);

            string titleNameStr = string.Format((target.GetNativeGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()].ConvertFormatSyntax(), target.GetName());

            target.SetTitle(titleInfo);
            handler.SendSysMessage(CypherStrings.TitleAddRes, titleId, titleNameStr, tNameLink);

            return true;
        }

        [Command("remove", RBACPermissions.CommandTitlesRemove)]
        static bool HandleTitlesRemoveCommand(CommandHandler handler, uint titleId)
        {
            Player target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);
                return false;
            }

            target.SetTitle(titleInfo, true);

            string tNameLink = handler.GetNameLink(target);
            string titleNameStr = string.Format((target.GetNativeGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()].ConvertFormatSyntax(), target.GetName());

            handler.SendSysMessage(CypherStrings.TitleRemoveRes, titleId, titleNameStr, tNameLink);

            if (!target.HasTitle(target.m_playerData.PlayerTitle))
            {
                target.SetChosenTitle(0);
                handler.SendSysMessage(CypherStrings.CurrentTitleReset, tNameLink);
            }

            return true;
        }

        [CommandGroup("set")]
        class TitleSetCommands
        {
            //Edit Player KnownTitles
            [Command("mask", RBACPermissions.CommandTitlesSetMask)]
            static bool HandleTitlesSetMaskCommand(CommandHandler handler, ulong mask)
            {
                Player target = handler.GetSelectedPlayer();
                if (!target)
                {
                    handler.SendSysMessage(CypherStrings.NoCharSelected);
                    return false;
                }

                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                ulong titles2 = mask;

                foreach (CharTitlesRecord tEntry in CliDB.CharTitlesStorage.Values)
                    titles2 &= ~(1ul << tEntry.MaskID);

                mask &= ~titles2;                                     // remove not existed titles

                target.SetKnownTitles(0, mask);
                handler.SendSysMessage(CypherStrings.Done);

                if (!target.HasTitle(target.m_playerData.PlayerTitle))
                {
                    target.SetChosenTitle(0);
                    handler.SendSysMessage(CypherStrings.CurrentTitleReset, handler.GetNameLink(target));
                }

                return true;
            }
        }
    }
}
