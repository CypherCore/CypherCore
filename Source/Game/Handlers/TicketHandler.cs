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
using Framework.Database;
using Game.Networking;
using Game.Networking.Packets;
using Game.SupportSystem;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.GmTicketGetCaseStatus, Processing = PacketProcessing.Inplace)]
        void HandleGMTicketGetCaseStatus(GMTicketGetCaseStatus packet)
        {
            //TODO: Implement GmCase and handle this packet correctly
            var status = new GMTicketCaseStatus();
            SendPacket(status);
        }

        [WorldPacketHandler(ClientOpcodes.GmTicketGetSystemStatus, Processing = PacketProcessing.Inplace)]
        void HandleGMTicketSystemStatusOpcode(GMTicketGetSystemStatus packet)
        {
            // Note: This only disables the ticket UI at client side and is not fully reliable
            // Note: This disables the whole customer support UI after trying to send a ticket in disabled state (MessageBox: "GM Help Tickets are currently unavaiable."). UI remains disabled until the character relogs.
            var response = new GMTicketSystemStatusPkt();
            response.Status = Global.SupportMgr.GetSupportSystemStatus() ? 1 : 0;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.SubmitUserFeedback)]
        void HandleSubmitUserFeedback(SubmitUserFeedback userFeedback)
        {
            if (userFeedback.IsSuggestion)
            {
                if (!Global.SupportMgr.GetSuggestionSystemStatus())
                    return;

                var ticket = new SuggestionTicket(GetPlayer());
                ticket.SetPosition(userFeedback.Header.MapID, userFeedback.Header.Position);
                ticket.SetFacing(userFeedback.Header.Facing);
                ticket.SetNote(userFeedback.Note);

                Global.SupportMgr.AddTicket(ticket);
            }
            else
            {
                if (!Global.SupportMgr.GetBugSystemStatus())
                    return;

                var ticket = new BugTicket(GetPlayer());
                ticket.SetPosition(userFeedback.Header.MapID, userFeedback.Header.Position);
                ticket.SetFacing(userFeedback.Header.Facing);
                ticket.SetNote(userFeedback.Note);

                Global.SupportMgr.AddTicket(ticket);
            }
        }

        [WorldPacketHandler(ClientOpcodes.SupportTicketSubmitComplaint)]
        void HandleSupportTicketSubmitComplaint(SupportTicketSubmitComplaint packet)
        {
            if (!Global.SupportMgr.GetComplaintSystemStatus())
                return;

            var comp = new ComplaintTicket(GetPlayer());
            comp.SetPosition(packet.Header.MapID, packet.Header.Position);
            comp.SetFacing(packet.Header.Facing);
            comp.SetChatLog(packet.ChatLog);
            comp.SetTargetCharacterGuid(packet.TargetCharacterGUID);
            comp.SetComplaintType((GMSupportComplaintType)packet.ComplaintType);
            comp.SetNote(packet.Note);

            Global.SupportMgr.AddTicket(comp);
        }

        [WorldPacketHandler(ClientOpcodes.BugReport)]
        void HandleBugReport(BugReport bugReport)
        {
            // Note: There is no way to trigger this with standard UI except /script ReportBug("text")
            if (!Global.SupportMgr.GetBugSystemStatus())
                return;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BUG_REPORT);
            stmt.AddValue(0, bugReport.Text);
            stmt.AddValue(1, bugReport.DiagInfo);
            DB.Characters.Execute(stmt);
        }

        [WorldPacketHandler(ClientOpcodes.Complaint)]
        void HandleComplaint(Complaint packet)
        {    // NOTE: all chat messages from this spammer are automatically ignored by the spam reporter until logout in case of chat spam.
             // if it's mail spam - ALL mails from this spammer are automatically removed by client

            var result = new ComplaintResult();
            result.ComplaintType = packet.ComplaintType;
            result.Result = 0;
            SendPacket(result);
        }
    }
}
