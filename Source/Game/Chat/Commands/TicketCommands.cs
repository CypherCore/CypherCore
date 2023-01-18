// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.SupportSystem;
using System;

namespace Game.Chat.Commands
{
    [CommandGroup("ticket")]
    class TicketCommands
    {
        [Command("togglesystem", RBACPermissions.CommandTicketTogglesystem, true)]
        static bool HandleToggleGMTicketSystem(CommandHandler handler)
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

        [CommandGroup("bug")]
        class TicketBugCommands
        {
            [Command("assign", RBACPermissions.CommandTicketBugAssign, true)]
            static bool HandleTicketBugAssignCommand(CommandHandler handler, uint ticketId, string targetName)
            {
                return HandleTicketAssignToCommand<BugTicket>(handler, ticketId, targetName);
            }

            [Command("close", RBACPermissions.CommandTicketBugClose, true)]
            static bool HandleTicketBugCloseCommand(CommandHandler handler, uint ticketId)
            {
                return HandleCloseByIdCommand<BugTicket>(handler, ticketId);
            }

            [Command("closedlist", RBACPermissions.CommandTicketBugClosedlist, true)]
            static bool HandleTicketBugClosedListCommand(CommandHandler handler)
            {
                return HandleClosedListCommand<BugTicket>(handler);
            }

            [Command("comment", RBACPermissions.CommandTicketBugComment, true)]
            static bool HandleTicketBugCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
            {
                return HandleCommentCommand<BugTicket>(handler, ticketId, comment);
            }

            [Command("delete", RBACPermissions.CommandTicketBugDelete, true)]
            static bool HandleTicketBugDeleteCommand(CommandHandler handler, uint ticketId)
            {
                return HandleDeleteByIdCommand<BugTicket>(handler, ticketId);
            }

            [Command("list", RBACPermissions.CommandTicketBugList, true)]
            static bool HandleTicketBugListCommand(CommandHandler handler)
            {
                return HandleListCommand<BugTicket>(handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketBugUnassign, true)]
            static bool HandleTicketBugUnAssignCommand(CommandHandler handler, uint ticketId)
            {
                return HandleUnAssignCommand<BugTicket>(handler, ticketId);
            }

            [Command("view", RBACPermissions.CommandTicketBugView, true)]
            static bool HandleTicketBugViewCommand(CommandHandler handler, uint ticketId)
            {
                return HandleGetByIdCommand<BugTicket>(handler, ticketId);
            }
        }

        [CommandGroup("complaint")]
        class TicketComplaintCommands
        {
            [Command("assign", RBACPermissions.CommandTicketComplaintAssign, true)]
            static bool HandleTicketComplaintAssignCommand(CommandHandler handler, uint ticketId, string targetName)
            {
                return HandleTicketAssignToCommand<ComplaintTicket>(handler, ticketId, targetName);
            }

            [Command("close", RBACPermissions.CommandTicketComplaintClose, true)]
            static bool HandleTicketComplaintCloseCommand(CommandHandler handler, uint ticketId)
            {
                return HandleCloseByIdCommand<ComplaintTicket>(handler, ticketId);
            }

            [Command("closedlist", RBACPermissions.CommandTicketComplaintClosedlist, true)]
            static bool HandleTicketComplaintClosedListCommand(CommandHandler handler)
            {
                return HandleClosedListCommand<ComplaintTicket>(handler);
            }

            [Command("comment", RBACPermissions.CommandTicketComplaintComment, true)]
            static bool HandleTicketComplaintCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
            {
                return HandleCommentCommand<ComplaintTicket>(handler, ticketId, comment);
            }

            [Command("delete", RBACPermissions.CommandTicketComplaintDelete, true)]
            static bool HandleTicketComplaintDeleteCommand(CommandHandler handler, uint ticketId)
            {
                return HandleDeleteByIdCommand<ComplaintTicket>(handler, ticketId);
            }

            [Command("list", RBACPermissions.CommandTicketComplaintList, true)]
            static bool HandleTicketComplaintListCommand(CommandHandler handler)
            {
                return HandleListCommand<ComplaintTicket>(handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketComplaintUnassign, true)]
            static bool HandleTicketComplaintUnAssignCommand(CommandHandler handler, uint ticketId)
            {
                return HandleUnAssignCommand<ComplaintTicket>(handler, ticketId);
            }

            [Command("view", RBACPermissions.CommandTicketComplaintView, true)]
            static bool HandleTicketComplaintViewCommand(CommandHandler handler, uint ticketId)
            {
                return HandleGetByIdCommand<ComplaintTicket>(handler, ticketId);
            }
        }

        [CommandGroup("suggestion")]
        class TicketSuggestionCommands
        {
            [Command("assign", RBACPermissions.CommandTicketSuggestionAssign, true)]
            static bool HandleTicketSuggestionAssignCommand(CommandHandler handler, uint ticketId, string targetName)
            {
                return HandleTicketAssignToCommand<SuggestionTicket>(handler, ticketId, targetName);
            }

            [Command("close", RBACPermissions.CommandTicketSuggestionClose, true)]
            static bool HandleTicketSuggestionCloseCommand(CommandHandler handler, uint ticketId)
            {
                return HandleCloseByIdCommand<SuggestionTicket>(handler, ticketId);
            }

            [Command("closedlist", RBACPermissions.CommandTicketSuggestionClosedlist, true)]
            static bool HandleTicketSuggestionClosedListCommand(CommandHandler handler)
            {
                return HandleClosedListCommand<SuggestionTicket>(handler);
            }

            [Command("comment", RBACPermissions.CommandTicketSuggestionComment, true)]
            static bool HandleTicketSuggestionCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
            {
                return HandleCommentCommand<SuggestionTicket>(handler, ticketId, comment);
            }

            [Command("delete", RBACPermissions.CommandTicketSuggestionDelete, true)]
            static bool HandleTicketSuggestionDeleteCommand(CommandHandler handler, uint ticketId)
            {
                return HandleDeleteByIdCommand<SuggestionTicket>(handler, ticketId);
            }

            [Command("list", RBACPermissions.CommandTicketSuggestionList, true)]
            static bool HandleTicketSuggestionListCommand(CommandHandler handler)
            {
                return HandleListCommand<SuggestionTicket>(handler);
            }

            [Command("unassign", RBACPermissions.CommandTicketSuggestionUnassign, true)]
            static bool HandleTicketSuggestionUnAssignCommand(CommandHandler handler, uint ticketId)
            {
                return HandleUnAssignCommand<SuggestionTicket>(handler, ticketId);
            }

            [Command("view", RBACPermissions.CommandTicketSuggestionView, true)]
            static bool HandleTicketSuggestionViewCommand(CommandHandler handler, uint ticketId)
            {
                return HandleGetByIdCommand<SuggestionTicket>(handler, ticketId);
            }
        }

        [CommandGroup("reset")]
        class TicketResetCommands
        {
            [Command("all", RBACPermissions.CommandTicketResetAll, true)]
            static bool HandleTicketResetAllCommand(CommandHandler handler)
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
            static bool HandleTicketResetBugCommand(CommandHandler handler)
            {
                return HandleResetCommand<BugTicket>(handler);
            }

            [Command("complaint", RBACPermissions.CommandTicketResetComplaint, true)]
            static bool HandleTicketResetComplaintCommand(CommandHandler handler)
            {
                return HandleResetCommand<ComplaintTicket>(handler);
            }

            [Command("suggestion", RBACPermissions.CommandTicketResetSuggestion, true)]
            static bool HandleTicketResetSuggestionCommand(CommandHandler handler)
            {
                return HandleResetCommand<SuggestionTicket>(handler);
            }
        }

        static bool HandleTicketAssignToCommand<T>(CommandHandler handler, uint ticketId, string targetName) where T : Ticket
        {
            if (targetName.IsEmpty())
                return false;

            if (!ObjectManager.NormalizePlayerName(ref targetName))
                return false;

            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null || ticket.IsClosed())
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            ObjectGuid targetGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(targetName);
            uint accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(targetGuid);
            // Target must exist and have administrative rights
            if (!Global.AccountMgr.HasPermission(accountId, RBACPermissions.CommandsBeAssignedTicket, Global.WorldMgr.GetRealm().Id.Index))
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
            ticket.SetAssignedTo(targetGuid, Global.AccountMgr.IsAdminAccount(Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.GetRealm().Id.Index)));
            ticket.SaveToDB();

            string msg = ticket.FormatViewMessageString(handler, null, targetName, null, null);
            handler.SendGlobalGMSysMessage(msg);
            return true;
        }

        static bool HandleCloseByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
        {
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

        static bool HandleClosedListCommand<T>(CommandHandler handler) where T : Ticket
        {
            Global.SupportMgr.ShowClosedList<T>(handler);
            return true;
        }

        static bool HandleCommentCommand<T>(CommandHandler handler, uint ticketId, QuotedString comment) where T : Ticket
        {
            if (comment.IsEmpty())
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

        static bool HandleDeleteByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
        {
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

        static bool HandleListCommand<T>(CommandHandler handler) where T : Ticket
        {
            Global.SupportMgr.ShowList<T>(handler);
            return true;
        }

        static bool HandleResetCommand<T>(CommandHandler handler) where T : Ticket
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

        static bool HandleUnAssignCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
        {
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
            AccountTypes security;
            Player assignedPlayer = ticket.GetAssignedPlayer();
            if (assignedPlayer && assignedPlayer.IsInWorld)
                security = assignedPlayer.GetSession().GetSecurity();
            else
            {
                ObjectGuid guid = ticket.GetAssignedToGUID();
                uint accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);
                security = Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.GetRealm().Id.Index);
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

        static bool HandleGetByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
        {
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
