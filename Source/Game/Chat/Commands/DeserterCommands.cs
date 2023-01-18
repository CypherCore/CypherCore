// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
