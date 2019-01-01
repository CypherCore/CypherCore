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
using Game.DataStorage;
using Game.Entities;
using System;

namespace Game.Chat
{
    [CommandGroup("scene", RBACPermissions.CommandScene)]
    class SceneCommands
    {
        [Command("cancel", RBACPermissions.CommandSceneCancel)]
        static bool HandleCancelSceneCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            uint id = args.NextUInt32();

            if (!CliDB.SceneScriptPackageStorage.HasRecord(id))
                return false;

            target.GetSceneMgr().CancelSceneByPackageId(id);
            return true;
        }

        [Command("debug", RBACPermissions.CommandSceneDebug)]
        static bool HandleDebugSceneCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            if (player)
            {
                player.GetSceneMgr().ToggleDebugSceneMode();
                handler.SendSysMessage(player.GetSceneMgr().IsInDebugSceneMode() ? CypherStrings.CommandSceneDebugOn : CypherStrings.CommandSceneDebugOff);
            }

            return true;
        }

        [Command("play", RBACPermissions.CommandScenePlay)]
        static bool HandlePlaySceneCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint sceneId = args.NextUInt32();
            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            if (Global.ObjectMgr.GetSceneTemplate(sceneId) == null)
                return false;

            target.GetSceneMgr().PlayScene(sceneId);
            return true;
        }

        [Command("playpackage", RBACPermissions.CommandScenePlayPackage)]
        static bool HandlePlayScenePackageCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint scenePackageId = args.NextUInt32();
            if (!uint.TryParse(args.NextString(""), out uint flags))
                flags = (uint)SceneFlags.Unk16;

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            if (!CliDB.SceneScriptPackageStorage.HasRecord(scenePackageId))
                return false;

            target.GetSceneMgr().PlaySceneByPackageId(scenePackageId, (SceneFlags)flags);
            return true;
        }
    }
}
