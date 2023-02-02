// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Entities;

namespace Game.Chat
{
    [CommandGroup("gm")]
    class GMCommands
    {
        [Command("chat", RBACPermissions.CommandGmChat)]
        static bool HandleGMChatCommand(CommandHandler handler, bool? enableArg)
        {
            WorldSession session = handler.GetSession();
            if (session != null)
            {
                if (!enableArg.HasValue)
                {
                    if (session.HasPermission(RBACPermissions.ChatUseStaffBadge) && session.GetPlayer().IsGMChat())
                        session.SendNotification(CypherStrings.GmChatOn);
                    else
                        session.SendNotification(CypherStrings.GmChatOff);
                    return true;
                }

                if (enableArg.HasValue)
                {
                    session.GetPlayer().SetGMChat(true);
                    session.SendNotification(CypherStrings.GmChatOn);
                }
                else
                {
                    session.GetPlayer().SetGMChat(false);
                    session.SendNotification(CypherStrings.GmChatOff);
                }

                return true;
            }

            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }

        [Command("fly", RBACPermissions.CommandGmFly)]
        static bool HandleGMFlyCommand(CommandHandler handler, bool enable)
        {
            Player target = handler.GetSelectedPlayer();
            if (target == null)
                target = handler.GetPlayer();

            if (enable)
            { 
                target.SetCanFly(true);
                target.SetCanTransitionBetweenSwimAndFly(true);
            }
            else
            { 
                target.SetCanFly(false);
                target.SetCanTransitionBetweenSwimAndFly(false);
            }

            handler.SendSysMessage(CypherStrings.CommandFlymodeStatus, handler.GetNameLink(target), enable ? "on" : "off");
            return true;
        }

        [Command("ingame", RBACPermissions.CommandGmIngame, true)]
        static bool HandleGMListIngameCommand(CommandHandler handler)
        {
            bool first = true;
            bool footer = false;

            foreach (var player in Global.ObjAccessor.GetPlayers())
            {
                AccountTypes playerSec = player.GetSession().GetSecurity();
                if ((player.IsGameMaster() ||
                    (player.GetSession().HasPermission(RBACPermissions.CommandsAppearInGmList) &&
                     playerSec <= (AccountTypes)WorldConfig.GetIntValue(WorldCfg.GmLevelInGmList))) &&
                    (handler.GetSession() == null || player.IsVisibleGloballyFor(handler.GetSession().GetPlayer())))
                {
                    if (first)
                    {
                        first = false;
                        footer = true;
                        handler.SendSysMessage(CypherStrings.GmsOnSrv);
                        handler.SendSysMessage("========================");
                    }
                    int size = player.GetName().Length;
                    byte security = (byte)playerSec;
                    int max = ((16 - size) / 2);
                    int max2 = max;
                    if ((max + max2 + size) == 16)
                        max2 = max - 1;
                    if (handler.GetSession() != null)
                        handler.SendSysMessage("|    {0} GMLevel {1}", player.GetName(), security);
                    else
                        handler.SendSysMessage("|{0}{1}{2}|   {3}  |", max, " ", player.GetName(), max2, " ", security);
                }
            }
            if (footer)
                handler.SendSysMessage("========================");
            if (first)
                handler.SendSysMessage(CypherStrings.GmsNotLogged);
            return true;
        }

        [Command("list", RBACPermissions.CommandGmList, true)]
        static bool HandleGMListFullCommand(CommandHandler handler)
        {
            // Get the accounts with GM Level >0
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_GM_ACCOUNTS);
            stmt.AddValue(0, (byte)AccountTypes.Moderator);
            stmt.AddValue(1, Global.WorldMgr.GetRealm().Id.Index);
            SQLResult result = DB.Login.Query(stmt);

            if (!result.IsEmpty())
            {
                handler.SendSysMessage( CypherStrings.Gmlist);
                handler.SendSysMessage("========================");
                // Cycle through them. Display username and GM level
                do
                {
                    string name = result.Read<string>(0);
                    byte security = result.Read<byte>(1);
                    int max = (16 - name.Length) / 2;
                    int max2 = max;
                    if ((max + max2 + name.Length) == 16)
                        max2 = max - 1;
                    string padding = "";
                    if (handler.GetSession() != null)
                        handler.SendSysMessage("|    {0} GMLevel {1}", name, security);
                    else
                        handler.SendSysMessage("|{0}{1}{2}|   {3}  |", padding.PadRight(max), name, padding.PadRight(max2), security);
                } while (result.NextRow());
                handler.SendSysMessage("========================");
            }
            else
                handler.SendSysMessage( CypherStrings.GmlistEmpty);
            return true;
        }

        [Command("off", RBACPermissions.CommandGm)]
        static bool HandleGMOffCommand(CommandHandler handler)
        {
            handler.GetPlayer().SetGameMaster(false);
            handler.GetPlayer().UpdateTriggerVisibility();
            handler.GetSession().SendNotification(CypherStrings.GmOff);
            return true;
        }

        [Command("on", RBACPermissions.CommandGm)]
        static bool HandleGMOnCommand(CommandHandler handler)
        {
            handler.GetPlayer().SetGameMaster(true);
            handler.GetPlayer().UpdateTriggerVisibility();
            handler.GetSession().SendNotification(CypherStrings.GmOn);
            return true;
        }

        [Command("visible", RBACPermissions.CommandGmVisible)]
        static bool HandleGMVisibleCommand(CommandHandler handler, bool? visibleArg)
        {
            Player _player = handler.GetSession().GetPlayer();

            if (!visibleArg.HasValue)
            {
                handler.SendSysMessage(CypherStrings.YouAre, _player.IsGMVisible() ? Global.ObjectMgr.GetCypherString(CypherStrings.Visible) : Global.ObjectMgr.GetCypherString(CypherStrings.Invisible));
                return true;
            }

            uint VISUAL_AURA = 37800;

            if (visibleArg.Value)
            {
                if (_player.HasAura(VISUAL_AURA, ObjectGuid.Empty))
                    _player.RemoveAurasDueToSpell(VISUAL_AURA);

                _player.SetGMVisible(true);
                _player.UpdateObjectVisibility();
                handler.GetSession().SendNotification(CypherStrings.InvisibleVisible);
            }
            else
            {
                _player.AddAura(VISUAL_AURA, _player);
                _player.SetGMVisible(false);
                _player.UpdateObjectVisibility();
                handler.GetSession().SendNotification(CypherStrings.InvisibleInvisible);
            }

            return true;
        }
    }
}
