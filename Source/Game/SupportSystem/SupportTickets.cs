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
using Framework.GameMath;
using Game.Chat;
using Game.Entities;
using Game.Network.Packets;
using System.Text;

namespace Game.SupportSystem
{
    public class Ticket
    {
        protected uint _id;
        protected ObjectGuid _playerGuid;
        protected uint _mapId;
        protected Vector3 _pos;
        protected ulong _createTime;
        protected ObjectGuid _closedBy; // 0 = Open, -1 = Console, playerGuid = player abandoned ticket, other = GM who closed it.
        protected ObjectGuid _assignedTo;
        protected string _comment;

        public Ticket() { }
        public Ticket(Player player)
        {
            _createTime = (ulong)Time.UnixTime;
            _playerGuid = player.GetGUID();
        }

        public void TeleportTo(Player player)
        {
            player.TeleportTo(_mapId, _pos.X, _pos.Y, _pos.Z, 0.0f, 0);
        }

        public virtual string FormatViewMessageString(CommandHandler handler, bool detailed = false) { return ""; }
        public virtual string FormatViewMessageString(CommandHandler handler, string closedName, string assignedToName, string unassignedName, string deletedName)
        {
            StringBuilder ss = new StringBuilder();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, _id));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, GetPlayerName()));

            if (!string.IsNullOrEmpty(closedName))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketclosed, closedName));
            if (!string.IsNullOrEmpty(assignedToName))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, assignedToName));
            if (!string.IsNullOrEmpty(unassignedName))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistunassigned, unassignedName));
            if (!string.IsNullOrEmpty(deletedName))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketdeleted, deletedName));
            return ss.ToString();
        }

        public bool IsClosed() { return !_closedBy.IsEmpty(); }
        bool IsFromPlayer(ObjectGuid guid) { return guid == _playerGuid; }
        public bool IsAssigned() { return !_assignedTo.IsEmpty(); }
        public bool IsAssignedTo(ObjectGuid guid) { return guid == _assignedTo; }
        public bool IsAssignedNotTo(ObjectGuid guid) { return IsAssigned() && !IsAssignedTo(guid); }

        public uint GetId() { return _id; }
        public ObjectGuid GetPlayerGuid() { return _playerGuid; }
        public Player GetPlayer() { return Global.ObjAccessor.FindConnectedPlayer(_playerGuid); }
        public string GetPlayerName()
        {
            string name = "";
            if (!_playerGuid.IsEmpty())
                ObjectManager.GetPlayerNameByGUID(_playerGuid, out name);

            return name;
        }
        public Player GetAssignedPlayer() { return Global.ObjAccessor.FindConnectedPlayer(_assignedTo); }
        public ObjectGuid GetAssignedToGUID() { return _assignedTo; }
        public string GetAssignedToName()
        {
            string name;
            if (!_assignedTo.IsEmpty())
                if (ObjectManager.GetPlayerNameByGUID(_assignedTo, out name))
                    return name;

            return "";
        }
        string GetComment() { return _comment; }

        public virtual void SetAssignedTo(ObjectGuid guid, bool IsAdmin = false) { _assignedTo = guid; }
        public virtual void SetUnassigned() { _assignedTo.Clear(); }
        public void SetClosedBy(ObjectGuid value) { _closedBy = value; }
        public void SetComment(string comment) { _comment = comment; }
        public void SetPosition(uint mapId, Vector3 pos)
        {
            _mapId = mapId;
            _pos = pos;
        }

        public virtual void LoadFromDB(SQLFields fields) { }
        public virtual void SaveToDB() { }
        public virtual void DeleteFromDB() { }
    }

    public class BugTicket : Ticket
    {
        float _facing;
        string _note;


        public BugTicket()
        {
            _note = "";
        }

        public BugTicket(Player player) : base(player)
        {
            _note = "";
            _id = Global.SupportMgr.GenerateBugId();
        }

        public override void LoadFromDB(SQLFields fields)
        {
            byte idx = 0;
            _id = fields.Read<uint>(idx);
            _playerGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _note = fields.Read<string>(++idx);
            _createTime = fields.Read<uint>(++idx);
            _mapId = fields.Read<ushort>(++idx);
            _pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
            _facing = fields.Read<float>(++idx);

            long closedBy = fields.Read<long>(++idx);
            if (closedBy == 0)
                _closedBy = ObjectGuid.Empty;
            else if (closedBy < 0)
                _closedBy.SetRawValue(0, (ulong)closedBy);
            else
                _closedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

            ulong assignedTo = fields.Read<ulong>(++idx);
            if (assignedTo == 0)
                _assignedTo = ObjectGuid.Empty;
            else
                _assignedTo = ObjectGuid.Create(HighGuid.Player, assignedTo);

            _comment = fields.Read<string>(++idx);
        }

        public override void SaveToDB()
        {
            byte idx = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_BUG);
            stmt.AddValue(idx, _id);
            stmt.AddValue(++idx, _playerGuid.GetCounter());
            stmt.AddValue(++idx, _note);
            stmt.AddValue(++idx, _mapId);
            stmt.AddValue(++idx, _pos.X);
            stmt.AddValue(++idx, _pos.Y);
            stmt.AddValue(++idx, _pos.Z);
            stmt.AddValue(++idx, _facing);
            stmt.AddValue(++idx, _closedBy.GetCounter());
            stmt.AddValue(++idx, _assignedTo.GetCounter());
            stmt.AddValue(++idx, _comment);

            DB.Characters.Execute(stmt);
        }

        public override void DeleteFromDB()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_BUG);
            stmt.AddValue(0, _id);
            DB.Characters.Execute(stmt);
        }

        public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
        {
            ulong curTime = (ulong)Time.UnixTime;

            StringBuilder ss = new StringBuilder();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, _id));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, GetPlayerName()));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - _createTime, true, false)));

            if (!_assignedTo.IsEmpty())
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, GetAssignedToName()));

            if (detailed)
            {
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));
                if (!string.IsNullOrEmpty(_comment))
                    ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, _comment));
            }
            return ss.ToString();
        }

        string GetNote() { return _note; }

        public void SetFacing(float facing) { _facing = facing; }
        public void SetNote(string note) { _note = note; }
    }

    public class ComplaintTicket : Ticket
    {
        float _facing;
        ObjectGuid _targetCharacterGuid;
        GMSupportComplaintType _complaintType;
        SupportTicketSubmitComplaint.SupportTicketChatLog _chatLog;
        string _note;

        public ComplaintTicket()
        {
            _note = "";
        }

        public ComplaintTicket(Player player) : base(player)
        {
            _note = "";
            _id = Global.SupportMgr.GenerateComplaintId();
        }

        public override void LoadFromDB(SQLFields fields)
        {
            byte idx = 0;
            _id = fields.Read<uint>(idx);
            _playerGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _note = fields.Read<string>(++idx);
            _createTime = fields.Read<uint>(++idx);
            _mapId = fields.Read<ushort>(++idx);
            _pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
            _facing = fields.Read<float>(++idx);
            _targetCharacterGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _complaintType = (GMSupportComplaintType)fields.Read<byte>(++idx);
            int reportLineIndex = fields.Read<int>(++idx);
            if (reportLineIndex != -1)
                _chatLog.ReportLineIndex.Set((uint)reportLineIndex);

            long closedBy = fields.Read<long>(++idx);
            if (closedBy == 0)
                _closedBy = ObjectGuid.Empty;
            else if (closedBy < 0)
                _closedBy.SetRawValue(0, (ulong)closedBy);
            else
                _closedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

            ulong assignedTo = fields.Read<ulong>(++idx);
            if (assignedTo == 0)
                _assignedTo = ObjectGuid.Empty;
            else
                _assignedTo = ObjectGuid.Create(HighGuid.Player, assignedTo);

            _comment = fields.Read<string>(++idx);
        }

        public void LoadChatLineFromDB(SQLFields fields)
        {
            _chatLog.Lines.Add(new SupportTicketSubmitComplaint.SupportTicketChatLine(fields.Read<uint>(0), fields.Read<string>(1)));
        }

        public override void SaveToDB()
        {
            var trans = new SQLTransaction();

            byte idx = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_COMPLAINT);
            stmt.AddValue(idx, _id);
            stmt.AddValue(++idx, _playerGuid.GetCounter());
            stmt.AddValue(++idx, _note);
            stmt.AddValue(++idx, _mapId);
            stmt.AddValue(++idx, _pos.X);
            stmt.AddValue(++idx, _pos.Y);
            stmt.AddValue(++idx, _pos.Z);
            stmt.AddValue(++idx, _facing);
            stmt.AddValue(++idx, _targetCharacterGuid.GetCounter());
            stmt.AddValue(++idx, _complaintType);
            if (_chatLog.ReportLineIndex.HasValue)
                stmt.AddValue(++idx, _chatLog.ReportLineIndex.Value);
            else
                stmt.AddValue(++idx, -1); // empty ReportLineIndex
            stmt.AddValue(++idx, _closedBy.GetCounter());
            stmt.AddValue(++idx, _assignedTo.GetCounter());
            stmt.AddValue(++idx, _comment);
            trans.Append(stmt);

            uint lineIndex = 0;
            foreach (var c in _chatLog.Lines)
            {
                idx = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GM_COMPLAINT_CHATLINE);
                stmt.AddValue(idx, _id);
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
            stmt.AddValue(0, _id);
            DB.Characters.Execute(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT_CHATLOG);
            stmt.AddValue(0, _id);
            DB.Characters.Execute(stmt);
        }

        public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
        {
            ulong curTime = (ulong)Time.UnixTime;

            StringBuilder ss = new StringBuilder();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, _id));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, GetPlayerName()));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - _createTime, true, false)));

            if (!_assignedTo.IsEmpty())
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, GetAssignedToName()));

            if (detailed)
            {
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));
                if (!string.IsNullOrEmpty(_comment))
                    ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, _comment));
            }
            return ss.ToString();
        }

        ObjectGuid GetTargetCharacterGuid() { return _targetCharacterGuid; }
        GMSupportComplaintType GetComplaintType() { return _complaintType; }
        string GetNote() { return _note; }

        public void SetFacing(float facing) { _facing = facing; }
        public void SetTargetCharacterGuid(ObjectGuid targetCharacterGuid)
        {
            _targetCharacterGuid = targetCharacterGuid;
        }
        public void SetComplaintType(GMSupportComplaintType type) { _complaintType = type; }
        public void SetChatLog(SupportTicketSubmitComplaint.SupportTicketChatLog log) { _chatLog = log; }
        public void SetNote(string note) { _note = note; }
    }

    public class SuggestionTicket : Ticket
    {
        float _facing;
        string _note;

        public SuggestionTicket()
        {
            _note = "";
        }

        public SuggestionTicket(Player player) : base(player)
        {
            _note = "";
            _id = Global.SupportMgr.GenerateSuggestionId();
        }

        public override void LoadFromDB(SQLFields fields)
        {
            byte idx = 0;
            _id = fields.Read<uint>(idx);
            _playerGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
            _note = fields.Read<string>(++idx);
            _createTime = fields.Read<uint>(++idx);
            _mapId = fields.Read<ushort>(++idx);
            _pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
            _facing = fields.Read<float>(++idx);

            long closedBy = fields.Read<long>(++idx);
            if (closedBy == 0)
                _closedBy = ObjectGuid.Empty;
            else if (closedBy < 0)
                _closedBy.SetRawValue(0, (ulong)closedBy);
            else
                _closedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

            ulong assignedTo = fields.Read<ulong>(++idx);
            if (assignedTo == 0)
                _assignedTo = ObjectGuid.Empty;
            else
                _assignedTo = ObjectGuid.Create(HighGuid.Player, assignedTo);

            _comment = fields.Read<string>(++idx);
        }

        public override void SaveToDB()
        {
            byte idx = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_SUGGESTION);
            stmt.AddValue(idx, _id);
            stmt.AddValue(++idx, _playerGuid.GetCounter());
            stmt.AddValue(++idx, _note);
            stmt.AddValue(++idx, _mapId);
            stmt.AddValue(++idx, _pos.X);
            stmt.AddValue(++idx, _pos.Y);
            stmt.AddValue(++idx, _pos.Z);
            stmt.AddValue(++idx, _facing);
            stmt.AddValue(++idx, _closedBy.GetCounter());
            stmt.AddValue(++idx, _assignedTo.GetCounter());
            stmt.AddValue(++idx, _comment);

            DB.Characters.Execute(stmt);
        }

        public override void DeleteFromDB()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_SUGGESTION);
            stmt.AddValue(0, _id);
            DB.Characters.Execute(stmt);
        }

        public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
        {
            ulong curTime = (ulong)Time.UnixTime;

            StringBuilder ss = new StringBuilder();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, _id));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, GetPlayerName()));
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - _createTime, true, false)));

            string name;
            if (ObjectManager.GetPlayerNameByGUID(_assignedTo, out name))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, name));

            if (detailed)
            {
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));
                if (!string.IsNullOrEmpty(_comment))
                    ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, _comment));
            }
            return ss.ToString();
        }

        string GetNote() { return _note; }
        public void SetNote(string note) { _note = note; }

        public void SetFacing(float facing) { _facing = facing; }
    }
}
