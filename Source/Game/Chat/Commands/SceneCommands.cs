// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
