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
using Game.Entities;
using Game.Spells;

namespace Game.Chat.Commands
{
    struct Spells
    {
        public const uint LFGDundeonDeserter = 71041;
        public const uint BGDeserter = 26013;
    }

    [CommandGroup("deserter")]
    class DeserterCommands
    {
        [CommandGroup("instance")]
        class DeserterInstanceCommands
        {
            [Command("add", RBACPermissions.CommandDeserterInstanceAdd)]
            static bool HandleDeserterInstanceAdd(CommandHandler handler, uint time)
            {
                return HandleDeserterAdd(handler, time, true);
            }

            [Command("remove", RBACPermissions.CommandDeserterInstanceRemove)]
            static bool HandleDeserterInstanceRemove(CommandHandler handler)
            {
                return HandleDeserterRemove(handler, true);
            }
        }

        [CommandGroup("bg")]
        class DeserterBGCommands
        {
            [Command("add", RBACPermissions.CommandDeserterBgAdd)]
            static bool HandleDeserterBGAdd(CommandHandler handler, uint time)
            {
                return HandleDeserterAdd(handler, time, false);
            }

            [Command("remove", RBACPermissions.CommandDeserterBgRemove)]
            static bool HandleDeserterBGRemove(CommandHandler handler)
            {
                return HandleDeserterRemove(handler, false);
            }
        }

        static bool HandleDeserterAdd(CommandHandler handler, uint time, bool isInstance)
        {
            Player player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            Aura aura = player.AddAura(isInstance ? Spells.LFGDundeonDeserter : Spells.BGDeserter, player);
            if (aura == null)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }
            aura.SetDuration((int)(time * Time.InMilliseconds));

            return true;
        }

        static bool HandleDeserterRemove(CommandHandler handler, bool isInstance)
        {
            Player player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            player.RemoveAura(isInstance ? Spells.LFGDundeonDeserter : Spells.BGDeserter);

            return true;
        }
    }
}
