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
using Framework.Database;
using Framework.IO;
using Game.Entities;

namespace Game.Chat
{
    [CommandGroup("gm", RBACPermissions.CommandGm)]
    class GMCommands
    {
        [Command("", RBACPermissions.CommandGm)]
        static bool HandleGMCommand(StringArguments args, CommandHandler handler)
        {
            Player _player = handler.GetSession().GetPlayer();

            if (args.Empty())
            {
                handler.SendNotification(_player.IsGameMaster() ? CypherStrings.GmOn : CypherStrings.GmOff);
                return true;
            }

            string param = args.NextString();
            if (param == "on")
            {
                _player.SetGameMaster(true);
                handler.SendNotification(CypherStrings.GmOn);
                _player.UpdateTriggerVisibility();
                return true;
            }
            if (param == "off")
            {
                _player.SetGameMaster(false);
                handler.SendNotification(CypherStrings.GmOff);
                _player.UpdateTriggerVisibility();
                return true;
            }

            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }

        [Command("chat", RBACPermissions.CommandGmChat)]
        static bool HandleGMChatCommand(StringArguments args, CommandHandler handler)
        {
            WorldSession session = handler.GetSession();
            if (session != null)
            {
                if (args.Empty())
                {
                    if (session.HasPermission(RBACPermissions.ChatUseStaffBadge) && session.GetPlayer().isGMChat())
                        session.SendNotification(CypherStrings.GmChatOn);
                    else
                        session.SendNotification(CypherStrings.GmChatOff);
                    return true;
                }

                string param = args.NextString();

                if (param == "on")
                {
                    session.GetPlayer().SetGMChat(true);
                    session.SendNotification(CypherStrings.GmChatOn);
                    return true;
                }

                if (param == "off")
                {
                    session.GetPlayer().SetGMChat(false);
                    session.SendNotification(CypherStrings.GmChatOff);
                    return true;
                }
            }

            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }

        [Command("fly", RBACPermissions.CommandGmFly)]
        static bool HandleGMFlyCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayer();
            if (target == null)
                target = handler.GetPlayer();

            string arg = args.NextString().ToLower();

            if (arg == "on")
            { 
                target.SetCanFly(true);
                target.SetCanTransitionBetweenSwimAndFly(true);
            }
            else if (arg == "off")
            { 
                target.SetCanFly(false);
                target.SetCanTransitionBetweenSwimAndFly(false);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }
            handler.SendSysMessage(CypherStrings.CommandFlymodeStatus, handler.GetNameLink(target), arg);
            return true;
        }

        [Command("ingame", RBACPermissions.CommandGmIngame, true)]
        static bool HandleGMListIngameCommand(StringArguments args, CommandHandler handler)
        {
            bool first = true;
            bool footer = false;

            var m = Global.ObjAccessor.GetPlayers();
            foreach (var pl in m)
            {
                AccountTypes accountType = pl.GetSession().GetSecurity();
                if ((pl.IsGameMaster() ||
                    (pl.GetSession().HasPermission(RBACPermissions.CommandsAppearInGmList) &&
                     accountType <= (AccountTypes)WorldConfig.GetIntValue(WorldCfg.GmLevelInGmList))) &&
                    (handler.GetSession() == null || pl.IsVisibleGloballyFor(handler.GetSession().GetPlayer())))
                {
                    if (first)
                    {
                        first = false;
                        footer = true;
                        handler.SendSysMessage(CypherStrings.GmsOnSrv);
                        handler.SendSysMessage("========================");
                    }
                    int size = pl.GetName().Length;
                    byte security = (byte)accountType;
                    int max = ((16 - size) / 2);
                    int max2 = max;
                    if ((max + max2 + size) == 16)
                        max2 = max - 1;
                    if (handler.GetSession() != null)
                        handler.SendSysMessage("|    {0} GMLevel {1}", pl.GetName(), security);
                    else
                        handler.SendSysMessage("|{0}{1}{2}|   {3}  |", max, " ", pl.GetName(), max2, " ", security);
                }
            }
            if (footer)
                handler.SendSysMessage("========================");
            if (first)
                handler.SendSysMessage(CypherStrings.GmsNotLogged);
            return true;
        }

        [Command("list", RBACPermissions.CommandGmList, true)]
        static bool HandleGMListFullCommand(StringArguments args, CommandHandler handler)
        {
            // Get the accounts with GM Level >0
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_GM_ACCOUNTS);
            stmt.AddValue(0, AccountTypes.Moderator);
            stmt.AddValue(1, Global.WorldMgr.GetRealm().Id.Realm);
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

        [Command("visible", RBACPermissions.CommandGmVisible)]
        static bool HandleGMVisibleCommand(StringArguments args, CommandHandler handler)
        {
            Player _player = handler.GetSession().GetPlayer();

            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.YouAre, _player.isGMVisible() ? Global.ObjectMgr.GetCypherString(CypherStrings.Visible) : Global.ObjectMgr.GetCypherString(CypherStrings.Invisible));
                return true;
            }

            uint VISUAL_AURA = 37800;
            string param = args.NextString();

            if (param == "on")
            {
                if (_player.HasAura(VISUAL_AURA, ObjectGuid.Empty))
                    _player.RemoveAurasDueToSpell(VISUAL_AURA);

                _player.SetGMVisible(true);
                _player.UpdateObjectVisibility();
                handler.GetSession().SendNotification(CypherStrings.InvisibleVisible);
                return true;
            }

            if (param == "off")
            {
                _player.AddAura(VISUAL_AURA, _player);
                _player.SetGMVisible(false);
                _player.UpdateObjectVisibility();
                handler.GetSession().SendNotification(CypherStrings.InvisibleInvisible);
                return true;
            }

            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }
    }
}
