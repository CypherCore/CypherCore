// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Game.Chat;
using Game.Entities;

namespace Game.SupportSystem
{
    public class SuggestionTicket : Ticket
    {
        private float _facing;
        private string _note;

        public SuggestionTicket()
        {
            _note = "";
        }

        public SuggestionTicket(Player player) : base(player)
        {
            _note = "";
            Id = Global.SupportMgr.GenerateSuggestionId();
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

        public override void SaveToDB()
        {
            byte idx = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_SUGGESTION);
            stmt.AddValue(idx, Id);
            stmt.AddValue(++idx, PlayerGuid.GetCounter());
            stmt.AddValue(++idx, _note);
            stmt.AddValue(++idx, CreateTime);
            stmt.AddValue(++idx, MapId);
            stmt.AddValue(++idx, Pos.X);
            stmt.AddValue(++idx, Pos.Y);
            stmt.AddValue(++idx, Pos.Z);
            stmt.AddValue(++idx, _facing);
            stmt.AddValue(++idx, ClosedBy.GetCounter());
            stmt.AddValue(++idx, AssignedTo.GetCounter());
            stmt.AddValue(++idx, Comment);

            DB.Characters.Execute(stmt);
        }

        public override void DeleteFromDB()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_SUGGESTION);
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

        public void SetNote(string note)
        {
            _note = note;
        }

        public void SetFacing(float facing)
        {
            _facing = facing;
        }

        private string GetNote()
        {
            return _note;
        }
    }
}