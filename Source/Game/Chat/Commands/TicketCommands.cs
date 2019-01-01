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
using Game.Entities;
using Game.SupportSystem;

namespace Game.Chat.Commands
{
    [CommandGroup("ticket", RBACPermissions.CommandTicket, true)]
    class TicketCommands
    {
        [Command("togglesystem", RBACPermissions.CommandTicketTogglesystem, true)]
        static bool HandleTicketToggleSystem(StringArguments args, CommandHandler handler)
        {
            if (!WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled))
            {
                handler.SendSysMessage(CypherStrings.DisallowTicketsConfig);
                return true;
            }

            bool status = !Global.SupportMgr.GetSupportSystemStatus();
            Global.SupportMgr.SetSupportSystemStatus(status);
            handler.SendSysMessage(status ? CypherStrings.AllowTickets : CypherStrings.DisallowTickets);
            return true;
        }

        [CommandGroup("bug", RBACPermissions.CommandTicketBug, true)]
        class TicketBugCommands
        {
            [Command("assign", RBACPermissions.CommandTicketBugAssign, true)]
            static bool HandleTicketBugAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleTicketAssignToCommand<BugTicket>(args, handler);
            }

            [Command("close", RBACPermissions.CommandTicketBugClose, true)]
            static bool HandleTicketBugCloseCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCloseByIdCommand<BugTicket>(args, handler);
            }

            [Command("closedlist", RBACPermissions.CommandTicketBugClosedlist, true)]
            static bool HandleTicketBugClosedListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleClosedListCommand<BugTicket>(args, handler);
            }

            [Command("comment", RBACPermissions.CommandTicketBugComment, true)]
            static bool HandleTicketBugCommentCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCommentCommand<BugTicket>(args, handler);
            }

            [Command("delete", RBACPermissions.CommandTicketBugDelete, true)]
            static bool HandleTicketBugDeleteCommand(StringArguments args, CommandHandler handler)
            {
                return HandleDeleteByIdCommand<BugTicket>(args, handler);
            }

            [Command("list", RBACPermissions.CommandTicketBugList, true)]
            static bool HandleTicketBugListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleListCommand<BugTicket>(args, handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketBugUnassign, true)]
            static bool HandleTicketBugUnAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleUnAssignCommand<BugTicket>(args, handler);
            }

            [Command("view", RBACPermissions.CommandTicketBugView, true)]
            static bool HandleTicketBugViewCommand(StringArguments args, CommandHandler handler)
            {
                return HandleGetByIdCommand<BugTicket>(args, handler);
            }
        }

        [CommandGroup("complaint", RBACPermissions.CommandTicketComplaint, true)]
        class TicketComplaintCommands
        {
            [Command("assign", RBACPermissions.CommandTicketComplaintAssign, true)]
            static bool HandleTicketComplaintAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleTicketAssignToCommand<ComplaintTicket>(args, handler);
            }

            [Command("close", RBACPermissions.CommandTicketComplaintClose, true)]
            static bool HandleTicketComplaintCloseCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCloseByIdCommand<ComplaintTicket>(args, handler);
            }

            [Command("closedlist", RBACPermissions.CommandTicketComplaintClosedlist, true)]
            static bool HandleTicketComplaintClosedListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleClosedListCommand<ComplaintTicket>(args, handler);
            }

            [Command("comment", RBACPermissions.CommandTicketComplaintComment, true)]
            static bool HandleTicketComplaintCommentCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCommentCommand<ComplaintTicket>(args, handler);
            }

            [Command("delete", RBACPermissions.CommandTicketComplaintDelete, true)]
            static bool HandleTicketComplaintDeleteCommand(StringArguments args, CommandHandler handler)
            {
                return HandleDeleteByIdCommand<ComplaintTicket>(args, handler);
            }

            [Command("list", RBACPermissions.CommandTicketComplaintList, true)]
            static bool HandleTicketComplaintListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleListCommand<ComplaintTicket>(args, handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketComplaintUnassign, true)]
            static bool HandleTicketComplaintUnAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleUnAssignCommand<ComplaintTicket>(args, handler);
            }

            [Command("view", RBACPermissions.CommandTicketComplaintView, true)]
            static bool HandleTicketComplaintViewCommand(StringArguments args, CommandHandler handler)
            {
                return HandleGetByIdCommand<ComplaintTicket>(args, handler);
            }
        }

        [CommandGroup("suggestion", RBACPermissions.CommandTicketSuggestion, true)]
        class TicketSuggestionCommands
        {
            [Command("assign", RBACPermissions.CommandTicketSuggestionAssign, true)]
            static bool HandleTicketSuggestionAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleTicketAssignToCommand<SuggestionTicket>(args, handler);
            }

            [Command("close", RBACPermissions.CommandTicketSuggestionClose, true)]
            static bool HandleTicketSuggestionCloseCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCloseByIdCommand<SuggestionTicket>(args, handler);
            }

            [Command("closedlist", RBACPermissions.CommandTicketSuggestionClosedlist, true)]
            static bool HandleTicketSuggestionClosedListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleClosedListCommand<SuggestionTicket>(args, handler);
            }

            [Command("comment", RBACPermissions.CommandTicketSuggestionComment, true)]
            static bool HandleTicketSuggestionCommentCommand(StringArguments args, CommandHandler handler)
            {
                return HandleCommentCommand<SuggestionTicket>(args, handler);
            }

            [Command("delete", RBACPermissions.CommandTicketSuggestionDelete, true)]
            static bool HandleTicketSuggestionDeleteCommand(StringArguments args, CommandHandler handler)
            {
                return HandleDeleteByIdCommand<SuggestionTicket>(args, handler);
            }

            [Command("list", RBACPermissions.CommandTicketSuggestionList, true)]
            static bool HandleTicketSuggestionListCommand(StringArguments args, CommandHandler handler)
            {
                return HandleListCommand<SuggestionTicket>(args, handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketSuggestionUnassign, true)]
            static bool HandleTicketSuggestionUnAssignCommand(StringArguments args, CommandHandler handler)
            {
                return HandleUnAssignCommand<SuggestionTicket>(args, handler);
            }

            [Command("view", RBACPermissions.CommandTicketSuggestionView, true)]
            static bool HandleTicketSuggestionViewCommand(StringArguments args, CommandHandler handler)
            {
                return HandleGetByIdCommand<SuggestionTicket>(args, handler);
            }
        }

        [CommandGroup("reset", RBACPermissions.CommandTicketReset, true)]
        class TicketResetCommands
        {
            [Command("all", RBACPermissions.CommandTicketResetAll, true)]
            static bool HandleTicketResetAllCommand(StringArguments args, CommandHandler handler)
            {
                if (Global.SupportMgr.GetOpenTicketCount<BugTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<ComplaintTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<SuggestionTicket>() != 0)
                {
                    handler.SendSysMessage(CypherStrings.CommandTicketpending);
                    return true;
                }
                else
                {
                    Global.SupportMgr.ResetTickets<BugTicket>();
                    Global.SupportMgr.ResetTickets<ComplaintTicket>();
                    Global.SupportMgr.ResetTickets<SuggestionTicket>();
                    handler.SendSysMessage(CypherStrings.CommandTicketreset);
                }
                return true;
            }

            [Command("bug", RBACPermissions.CommandTicketResetBug, true)]
            static bool HandleTicketResetBugCommand(StringArguments args, CommandHandler handler)
            {
                return HandleResetCommand<BugTicket>(args, handler);
            }

            [Command("complaint", RBACPermissions.CommandTicketResetComplaint, true)]
            static bool HandleTicketResetComplaintCommand(StringArguments args, CommandHandler handler)
            {
                return HandleResetCommand<ComplaintTicket>(args, handler);
            }

            [Command("suggestion", RBACPermissions.CommandTicketResetSuggestion, true)]
            static bool HandleTicketResetSuggestionCommand(StringArguments args, CommandHandler handler)
            {
                return HandleResetCommand<SuggestionTicket>(args, handler);
            }
        }

        static bool HandleTicketAssignToCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();

            string target = args.NextString();
            if (string.IsNullOrEmpty(target))
                return false;

            if (!ObjectManager.NormalizePlayerName(ref target))
                return false;

            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            ObjectGuid targetGuid = ObjectManager.GetPlayerGUIDByName(target);
            uint accountId = ObjectManager.GetPlayerAccountIdByGUID(targetGuid);
            // Target must exist and have administrative rights
            if (!Global.AccountMgr.HasPermission(accountId, RBACPermissions.CommandsBeAssignedTicket, Global.WorldMgr.GetRealm().Id.Realm))
            {
                handler.SendSysMessage(CypherStrings.CommandTicketassignerrorA);
                return true;
            }

            // If already assigned, leave
            if (ticket.IsAssignedTo(targetGuid))
            {
                handler.SendSysMessage(CypherStrings.CommandTicketassignerrorB, ticket.GetId());
                return true;
            }

            // If assigned to different player other than current, leave
            //! Console can override though
            Player player = handler.GetSession() != null ? handler.GetSession().GetPlayer() : null;
            if (player && ticket.IsAssignedNotTo(player.GetGUID()))
            {
                handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.GetId());
                return true;
            }

            // Assign ticket
            ticket.SetAssignedTo(targetGuid, Global.AccountMgr.IsAdminAccount(Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.GetRealm().Id.Realm)));
            ticket.SaveToDB();

            string msg = ticket.FormatViewMessageString(handler, null, target, null, null);
            handler.SendGlobalGMSysMessage(msg);
            return true;
        }

        static bool HandleCloseByIdCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();
            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            // Ticket should be assigned to the player who tries to close it.
            // Console can override though
            Player player = handler.GetSession() != null ? handler.GetSession().GetPlayer() : null;
            if (player && ticket.IsAssignedNotTo(player.GetGUID()))
            {
                handler.SendSysMessage(CypherStrings.CommandTicketcannotclose, ticket.GetId());
                return true;
            }

            ObjectGuid closedByGuid = ObjectGuid.Empty;
            if (player)
                closedByGuid = player.GetGUID();
            else
                closedByGuid.SetRawValue(0, ulong.MaxValue);

            Global.SupportMgr.CloseTicket<T>(ticket.GetId(), closedByGuid);

            string msg = ticket.FormatViewMessageString(handler, player ? player.GetName() : "Console", null, null, null);
            handler.SendGlobalGMSysMessage(msg);

            return true;
        }

        static bool HandleClosedListCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            Global.SupportMgr.ShowClosedList<T>(handler);
            return true;
        }

        static bool HandleCommentCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();

            string comment = args.NextString("\n");
            if (string.IsNullOrEmpty(comment))
                return false;

            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            // Cannot comment ticket assigned to someone else
            //! Console excluded
            Player player = handler.GetSession() != null ? handler.GetSession().GetPlayer() : null;
            if (player && ticket.IsAssignedNotTo(player.GetGUID()))
            {
                handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.GetId());
                return true;
            }

            ticket.SetComment(comment);
            ticket.SaveToDB();
            Global.SupportMgr.UpdateLastChange();

            string msg = ticket.FormatViewMessageString(handler, null, ticket.GetAssignedToName(), null, null);
            msg += string.Format(handler.GetCypherString(CypherStrings.CommandTicketlistaddcomment), player ? player.GetName() : "Console", comment);
            handler.SendGlobalGMSysMessage(msg);

            return true;
        }

        static bool HandleDeleteByIdCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();
            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            if (!ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketclosefirst);
                return true;
            }

            string msg = ticket.FormatViewMessageString(handler, null, null, null, handler.GetSession() != null ? handler.GetSession().GetPlayer().GetName() : "Console");
            handler.SendGlobalGMSysMessage(msg);

            Global.SupportMgr.RemoveTicket<T>(ticket.GetId());

            return true;
        }

        static bool HandleListCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            Global.SupportMgr.ShowList<T>(handler);
            return true;
        }

        static bool HandleResetCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (Global.SupportMgr.GetOpenTicketCount<T>() != 0)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketpending);
                return true;
            }
            else
            {
                Global.SupportMgr.ResetTickets<T>();
                handler.SendSysMessage(CypherStrings.CommandTicketreset);
            }

            return true;
        }

        static bool HandleUnAssignCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();
            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }
            // Ticket must be assigned
            if (!ticket.IsAssigned())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotassigned, ticket.GetId());
                return true;
            }

            // Get security level of player, whom this ticket is assigned to
            AccountTypes security = AccountTypes.Player;
            Player assignedPlayer = ticket.GetAssignedPlayer();
            if (assignedPlayer && assignedPlayer.IsInWorld)
                security = assignedPlayer.GetSession().GetSecurity();
            else
            {
                ObjectGuid guid = ticket.GetAssignedToGUID();
                uint accountId = ObjectManager.GetPlayerAccountIdByGUID(guid);
                security = Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.GetRealm().Id.Realm);
            }

            // Check security
            //! If no m_session present it means we're issuing this command from the console
            AccountTypes mySecurity = handler.GetSession() != null ? handler.GetSession().GetSecurity() : AccountTypes.Console;
            if (security > mySecurity)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketunassignsecurity);
                return true;
            }

            string assignedTo = ticket.GetAssignedToName(); // copy assignedto name because we need it after the ticket has been unnassigned

            ticket.SetUnassigned();
            ticket.SaveToDB();
            string msg = ticket.FormatViewMessageString(handler, null, assignedTo, handler.GetSession() != null ? handler.GetSession().GetPlayer().GetName() : "Console", null);
            handler.SendGlobalGMSysMessage(msg);

            return true;
        }

        static bool HandleGetByIdCommand<T>(StringArguments args, CommandHandler handler) where T : Ticket
        {
            if (args.Empty())
                return false;

            uint ticketId = args.NextUInt32();
            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            handler.SendSysMessage(ticket.FormatViewMessageString(handler, true));
            return true;
        }
    }
}
