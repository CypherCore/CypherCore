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
    public class Ticket
    {
        protected ObjectGuid AssignedTo;
        protected ObjectGuid ClosedBy; // 0 = Open, -1 = Console, playerGuid = player abandoned ticket, other = GM who closed it.
        protected string Comment;
        protected ulong CreateTime;
        protected uint Id;
        protected uint MapId;
        protected ObjectGuid PlayerGuid;
        protected Vector3 Pos;

        public Ticket()
        {
        }

        public Ticket(Player player)
        {
            CreateTime = (ulong)GameTime.GetGameTime();
            PlayerGuid = player.GetGUID();
        }

        public void TeleportTo(Player player)
        {
            player.TeleportTo(MapId, Pos.X, Pos.Y, Pos.Z, 0.0f, 0);
        }

        public virtual string FormatViewMessageString(CommandHandler handler, bool detailed = false)
        {
            return "";
        }

        public virtual string FormatViewMessageString(CommandHandler handler, string closedName, string assignedToName, string unassignedName, string deletedName)
        {
            StringBuilder ss = new();
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, Id));
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

        public bool IsClosed()
        {
            return !ClosedBy.IsEmpty();
        }

        public bool IsAssigned()
        {
            return !AssignedTo.IsEmpty();
        }

        public bool IsAssignedTo(ObjectGuid guid)
        {
            return guid == AssignedTo;
        }

        public bool IsAssignedNotTo(ObjectGuid guid)
        {
            return IsAssigned() && !IsAssignedTo(guid);
        }

        public uint GetId()
        {
            return Id;
        }

        public ObjectGuid GetPlayerGuid()
        {
            return PlayerGuid;
        }

        public Player GetPlayer()
        {
            return Global.ObjAccessor.FindConnectedPlayer(PlayerGuid);
        }

        public string GetPlayerName()
        {
            string name = "";

            if (!PlayerGuid.IsEmpty())
                Global.CharacterCacheStorage.GetCharacterNameByGuid(PlayerGuid, out name);

            return name;
        }

        public Player GetAssignedPlayer()
        {
            return Global.ObjAccessor.FindConnectedPlayer(AssignedTo);
        }

        public ObjectGuid GetAssignedToGUID()
        {
            return AssignedTo;
        }

        public string GetAssignedToName()
        {
            string name;

            if (!AssignedTo.IsEmpty())
                if (Global.CharacterCacheStorage.GetCharacterNameByGuid(AssignedTo, out name))
                    return name;

            return "";
        }

        public virtual void SetAssignedTo(ObjectGuid guid, bool IsAdmin = false)
        {
            AssignedTo = guid;
        }

        public virtual void SetUnassigned()
        {
            AssignedTo.Clear();
        }

        public void SetClosedBy(ObjectGuid value)
        {
            ClosedBy = value;
        }

        public void SetComment(string comment)
        {
            Comment = comment;
        }

        public void SetPosition(uint mapId, Vector3 pos)
        {
            MapId = mapId;
            Pos = pos;
        }

        public virtual void LoadFromDB(SQLFields fields)
        {
        }

        public virtual void SaveToDB()
        {
        }

        public virtual void DeleteFromDB()
        {
        }

        private bool IsFromPlayer(ObjectGuid guid)
        {
            return guid == PlayerGuid;
        }

        private string GetComment()
        {
            return Comment;
        }
    }
}