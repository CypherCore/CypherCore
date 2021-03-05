﻿/*
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
    [CommandGroup("titles", RBACPermissions.CommandTitles)]
    class TitleCommands
    {
        [Command("current", RBACPermissions.CommandTitlesCurrent)]
        static bool HandleTitlesCurrentCommand(StringArguments args, CommandHandler handler)
        {
            // number or [name] Shift-click form |color|Htitle:title_id|h[name]|h|r
            var id_p = handler.ExtractKeyFromLink(args, "Htitle");
            if (string.IsNullOrEmpty(id_p))
                return false;

            if (!uint.TryParse(id_p, out var id) || id == 0)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            var target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            var titleInfo = CliDB.CharTitlesStorage.LookupByKey(id);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            var tNameLink = handler.GetNameLink(target);

            target.SetTitle(titleInfo);                            // to be sure that title now known
            target.SetChosenTitle(titleInfo.MaskID);

            handler.SendSysMessage(CypherStrings.TitleCurrentRes, id, (target.GetGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()], tNameLink);
            return true;
        }

        [Command("add", RBACPermissions.CommandTitlesAdd)]
        static bool HandleTitlesAddCommand(StringArguments args, CommandHandler handler)
        {
            // number or [name] Shift-click form |color|Htitle:title_id|h[name]|h|r
            var id_p = handler.ExtractKeyFromLink(args, "Htitle");
            if (string.IsNullOrEmpty(id_p))
                return false;

            if (!uint.TryParse(id_p, out var id) || id == 0)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            var target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            var titleInfo = CliDB.CharTitlesStorage.LookupByKey(id);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            var tNameLink = handler.GetNameLink(target);

            var titleNameStr = string.Format((target.GetGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()].ConvertFormatSyntax(), target.GetName());

            target.SetTitle(titleInfo);
            handler.SendSysMessage(CypherStrings.TitleAddRes, id, titleNameStr, tNameLink);

            return true;
        }

        [Command("remove", RBACPermissions.CommandTitlesRemove)]
        static bool HandleTitlesRemoveCommand(StringArguments args, CommandHandler handler)
        {
            // number or [name] Shift-click form |color|Htitle:title_id|h[name]|h|r
            var id_p = handler.ExtractKeyFromLink(args, "Htitle");
            if (string.IsNullOrEmpty(id_p))
                return false;

            if (!uint.TryParse(id_p, out var id) || id == 0)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            var target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            var titleInfo = CliDB.CharTitlesStorage.LookupByKey(id);
            if (titleInfo == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidTitleId, id);
                return false;
            }

            target.SetTitle(titleInfo, true);

            var tNameLink = handler.GetNameLink(target);

            var titleNameStr = string.Format((target.GetGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()].ConvertFormatSyntax(), target.GetName());

            handler.SendSysMessage(CypherStrings.TitleRemoveRes, id, titleNameStr, tNameLink);

            if (!target.HasTitle(target.m_playerData.PlayerTitle))
            {
                target.SetChosenTitle(0);
                handler.SendSysMessage(CypherStrings.CurrentTitleReset, tNameLink);
            }

            return true;
        }

        [CommandGroup("set", RBACPermissions.CommandTitlesSet)]
        class TitleSetCommands
        {
            //Edit Player KnownTitles
            [Command("mask", RBACPermissions.CommandTitlesSetMask)]
            static bool HandleTitlesSetMaskCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var titles = args.NextUInt64();

                var target = handler.GetSelectedPlayer();
                if (!target)
                {
                    handler.SendSysMessage(CypherStrings.NoCharSelected);
                    return false;
                }

                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                var titles2 = titles;

                foreach (var tEntry in CliDB.CharTitlesStorage.Values)
                    titles2 &= ~(1ul << tEntry.MaskID);

                titles &= ~titles2;                                     // remove not existed titles

                target.SetKnownTitles(0, titles);
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
