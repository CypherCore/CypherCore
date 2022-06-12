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

namespace Game.Chat
{
    [CommandGroup("scene")]
    class SceneCommands
    {
        [Command("cancel", RBACPermissions.CommandSceneCancel)]
        static bool HandleCancelSceneCommand(CommandHandler handler, uint sceneScriptPackageId)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            if (!CliDB.SceneScriptPackageStorage.HasRecord(sceneScriptPackageId))
                return false;

            target.GetSceneMgr().CancelSceneByPackageId(sceneScriptPackageId);
            return true;
        }

        [Command("debug", RBACPermissions.CommandSceneDebug)]
        static bool HandleDebugSceneCommand(CommandHandler handler)
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
        static bool HandlePlaySceneCommand(CommandHandler handler, uint sceneId)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
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
        static bool HandlePlayScenePackageCommand(CommandHandler handler, uint sceneScriptPackageId, SceneFlags? flags)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            if (!CliDB.SceneScriptPackageStorage.HasRecord(sceneScriptPackageId))
                return false;

            target.GetSceneMgr().PlaySceneByPackageId(sceneScriptPackageId, flags.GetValueOrDefault(0));
            return true;
        }
    }
}
