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
