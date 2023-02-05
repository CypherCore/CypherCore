// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            GMTicketCaseStatus status = new();
            SendPacket(status);
        }

        [WorldPacketHandler(ClientOpcodes.GmTicketGetSystemStatus, Processing = PacketProcessing.Inplace)]
        void HandleGMTicketSystemStatusOpcode(GMTicketGetSystemStatus packet)
        {
            // Note: This only disables the ticket UI at client side and is not fully reliable
            // Note: This disables the whole customer support UI after trying to send a ticket in disabled state (MessageBox: "GM Help Tickets are currently unavaiable."). UI remains disabled until the character relogs.
            GMTicketSystemStatusPkt response = new();
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

                SuggestionTicket ticket = new(GetPlayer());
                ticket.SetPosition(userFeedback.Header.MapID, userFeedback.Header.Position);
                ticket.SetFacing(userFeedback.Header.Facing);
                ticket.SetNote(userFeedback.Note);

                Global.SupportMgr.AddTicket(ticket);
            }
            else
            {
                if (!Global.SupportMgr.GetBugSystemStatus())
                    return;

                BugTicket ticket = new(GetPlayer());
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

            ComplaintTicket comp = new(GetPlayer());
            comp.SetPosition(packet.Header.MapID, packet.Header.Position);
            comp.SetFacing(packet.Header.Facing);
            comp.SetChatLog(packet.ChatLog);
            comp.SetTargetCharacterGuid(packet.TargetCharacterGUID);
            comp.SetReportType((ReportType)packet.ReportType);
            comp.SetMajorCategory((ReportMajorCategory)packet.MajorCategory);
            comp.SetMinorCategoryFlags((ReportMinorCategory)packet.MinorCategoryFlags);
            comp.SetNote(packet.Note);

            Global.SupportMgr.AddTicket(comp);
        }

        [WorldPacketHandler(ClientOpcodes.BugReport)]
        void HandleBugReport(BugReport bugReport)
        {
            // Note: There is no way to trigger this with standard UI except /script ReportBug("text")
            if (!Global.SupportMgr.GetBugSystemStatus())
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_BUG_REPORT);
            stmt.AddValue(0, bugReport.Text);
            stmt.AddValue(1, bugReport.DiagInfo);
            DB.Characters.Execute(stmt);
        }

        [WorldPacketHandler(ClientOpcodes.Complaint)]
        void HandleComplaint(Complaint packet)
        {    // NOTE: all chat messages from this spammer are automatically ignored by the spam reporter until logout in case of chat spam.
             // if it's mail spam - ALL mails from this spammer are automatically removed by client

            ComplaintResult result = new();
            result.ComplaintType = packet.ComplaintType;
            result.Result = 0;
            SendPacket(result);
        }
    }
}
