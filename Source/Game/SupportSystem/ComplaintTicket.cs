// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Game.Chat;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.SupportSystem
{
    public class ComplaintTicket : Ticket
    {
        private SupportTicketSubmitComplaint.SupportTicketChatLog _chatLog;
        private float _facing;
        private ReportMajorCategory _majorCategory;
        private ReportMinorCategory _minorCategoryFlags = ReportMinorCategory.TextChat;
        private string _note;
        private ReportType _reportType;
        private ObjectGuid _targetCharacterGuid;

        public ComplaintTicket()
        {
            _note = "";
        }

        public ComplaintTicket(Player player) : base(player)
        {
            _note = "";
            Id = Global.SupportMgr.GenerateComplaintId();
        }

        public override void LoadFromDB(SQLFields fields)
        {
            byte idx = 0;
            Id = fields.Read<uint>(idx);
            PlayerGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _note = fields.Read<string>(++idx);
            CreateTime = fields.Read<ulong>(++idx);
            MapId = fields.Read<ushort>(++idx);
            Pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
            _facing = fields.Read<float>(++idx);
            _targetCharacterGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _reportType = (ReportType)fields.Read<int>(++idx);
            _majorCategory = (ReportMajorCategory)fields.Read<int>(++idx);
            _minorCategoryFlags = (ReportMinorCategory)fields.Read<int>(++idx);
            int reportLineIndex = fields.Read<int>(++idx);

            if (reportLineIndex != -1)
                _chatLog.ReportLineIndex = (uint)reportLineIndex;

            long closedBy = fields.Read<long>(++idx);

            if (closedBy == 0)
                ClosedBy = ObjectGuid.Empty;
            else if (closedBy < 0)
                ClosedBy.SetRawValue(0, (ulong)closedBy);
            else
                ClosedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

            ulong assignedTo = fields.Read<ulong>(++idx);

            if (assignedTo == 0)
                AssignedTo = ObjectGuid.Empty;
            else
                AssignedTo = ObjectGuid.Create(HighGuid.Player, assignedTo);

            Comment = fields.Read<string>(++idx);
        }

        public void LoadChatLineFromDB(SQLFields fields)
        {
            _chatLog.Lines.Add(new SupportTicketSubmitComplaint.SupportTicketChatLine(fields.Read<long>(0), fields.Read<string>(1)));
        }

        public override void SaveToDB()
        {
            var trans = new SQLTransaction();

            byte idx = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_COMPLAINT);
            stmt.AddValue(idx, Id);
            stmt.AddValue(++idx, PlayerGuid.GetCounter());
            stmt.AddValue(++idx, _note);
            stmt.AddValue(++idx, CreateTime);
            stmt.AddValue(++idx, MapId);
            stmt.AddValue(++idx, Pos.X);
            stmt.AddValue(++idx, Pos.Y);
            stmt.AddValue(++idx, Pos.Z);
            stmt.AddValue(++idx, _facing);
            stmt.AddValue(++idx, _targetCharacterGuid.GetCounter());
            stmt.AddValue(++idx, (int)_reportType);
            stmt.AddValue(++idx, (int)_majorCategory);
            stmt.AddValue(++idx, (int)_minorCategoryFlags);

            if (_chatLog.ReportLineIndex.HasValue)
                stmt.AddValue(++idx, _chatLog.ReportLineIndex.Value);
            else
                stmt.AddValue(++idx, -1); // empty ReportLineIndex

            stmt.AddValue(++idx, ClosedBy.GetCounter());
            stmt.AddValue(++idx, AssignedTo.GetCounter());
            stmt.AddValue(++idx, Comment);
            trans.Append(stmt);

            uint lineIndex = 0;

            foreach (var c in _chatLog.Lines)
            {
                idx = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GM_COMPLAINT_CHATLINE);
                stmt.AddValue(idx, Id);
                stmt.AddValue(++idx, lineIndex);
                stmt.AddValue(++idx, c.Timestamp);
                stmt.AddValue(++idx, c.Text);

                trans.Append(stmt);
                ++lineIndex;
            }

            DB.Characters.CommitTransaction(trans);
        }

        public override void DeleteFromDB()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT);
            stmt.AddValue(0, Id);
            DB.Characters.Execute(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT_CHATLOG);
            stmt.AddValue(0, Id);
            DB.Characters.Execute(stmt);
        }

        public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
        {
            ulong curTime = (ulong)GameTime.GetGameTime();

            StringBuilder ss = new();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, Id));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, GetPlayerName()));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - CreateTime, TimeFormat.ShortText, false)));

            if (!AssignedTo.IsEmpty())
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, GetAssignedToName()));

            if (detailed)
            {
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));

                if (!string.IsNullOrEmpty(Comment))
                    ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, Comment));
            }

            return ss.ToString();
        }

        public void SetFacing(float facing)
        {
            _facing = facing;
        }

        public void SetTargetCharacterGuid(ObjectGuid targetCharacterGuid)
        {
            _targetCharacterGuid = targetCharacterGuid;
        }

        public void SetReportType(ReportType reportType)
        {
            _reportType = reportType;
        }

        public void SetMajorCategory(ReportMajorCategory majorCategory)
        {
            _majorCategory = majorCategory;
        }

        public void SetMinorCategoryFlags(ReportMinorCategory minorCategoryFlags)
        {
            _minorCategoryFlags = minorCategoryFlags;
        }

        public void SetChatLog(SupportTicketSubmitComplaint.SupportTicketChatLog log)
        {
            _chatLog = log;
        }

        public void SetNote(string note)
        {
            _note = note;
        }

        private ObjectGuid GetTargetCharacterGuid()
        {
            return _targetCharacterGuid;
        }

        private ReportType GetReportType()
        {
            return _reportType;
        }

        private ReportMajorCategory GetMajorCategory()
        {
            return _majorCategory;
        }

        private ReportMinorCategory GetMinorCategoryFlags()
        {
            return _minorCategoryFlags;
        }

        private string GetNote()
        {
            return _note;
        }
    }
}