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
using Game.Chat;
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game.SupportSystem
{
    public class SupportManager : Singleton<SupportManager>
    {
        SupportManager() { }

        public void Initialize()
        {
            SetSupportSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportEnabled));
            SetTicketSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled));
            SetBugSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled));
            SetComplaintSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled));
            SetSuggestionSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled));
        }

        public T GetTicket<T>(uint Id) where T : Ticket
        {
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    return _bugTicketList.LookupByKey(Id) as T;
                case "ComplaintTicket":
                    return _complaintTicketList.LookupByKey(Id) as T;
                case "SuggestionTicket":
                    return _suggestionTicketList.LookupByKey(Id) as T;
            }

            return default(T);
        }

        public uint GetOpenTicketCount<T>() where T : Ticket
        {
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    return _openBugTicketCount;
                case "ComplaintTicket":
                    return _openComplaintTicketCount;
                case "SuggestionTicket":
                    return _openSuggestionTicketCount;
            }
            return 0;
        }

        public void LoadBugTickets()
        {
            uint oldMSTime = Time.GetMSTime();
            _bugTicketList.Clear();

            _lastBugId = 0;
            _openBugTicketCount = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_BUGS);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 GM bugs. DB table `gm_bug` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                BugTicket bug = new BugTicket();
                bug.LoadFromDB(result.GetFields());

                if (!bug.IsClosed())
                    ++_openBugTicketCount;

                uint id = bug.GetId();
                if (_lastBugId < id)
                    _lastBugId = id;

                _bugTicketList[id] = bug;
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GM bugs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadComplaintTickets()
        {
            uint oldMSTime = Time.GetMSTime();
            _complaintTicketList.Clear();

            _lastComplaintId = 0;
            _openComplaintTicketCount = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_COMPLAINTS);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 GM complaints. DB table `gm_complaint` is empty!");
                return;
            }

            uint count = 0;
            PreparedStatement chatLogStmt;
            SQLResult chatLogResult;
            do
            {
                ComplaintTicket complaint = new ComplaintTicket();
                complaint.LoadFromDB(result.GetFields());

                if (!complaint.IsClosed())
                    ++_openComplaintTicketCount;

                uint id = complaint.GetId();
                if (_lastComplaintId < id)
                    _lastComplaintId = id;

                chatLogStmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_COMPLAINT_CHATLINES);
                chatLogStmt.AddValue(0, id);
                chatLogResult = DB.Characters.Query(stmt);

                if (!chatLogResult.IsEmpty())
                {
                    do
                    {
                        complaint.LoadChatLineFromDB(chatLogResult.GetFields());
                    } while (chatLogResult.NextRow());
                }

                _complaintTicketList[id] = complaint;
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GM complaints in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadSuggestionTickets()
        {
            uint oldMSTime = Time.GetMSTime();
            _suggestionTicketList.Clear();

            _lastSuggestionId = 0;
            _openSuggestionTicketCount = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_SUGGESTIONS);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 GM suggestions. DB table `gm_suggestion` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                SuggestionTicket suggestion = new SuggestionTicket();
                suggestion.LoadFromDB(result.GetFields());

                if (!suggestion.IsClosed())
                    ++_openSuggestionTicketCount;

                uint id = suggestion.GetId();
                if (_lastSuggestionId < id)
                    _lastSuggestionId = id;

                _suggestionTicketList[id] = suggestion;
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GM suggestions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void AddTicket<T>(T ticket)where T : Ticket
        {
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    _bugTicketList[ticket.GetId()] = ticket as BugTicket;
                    if (!ticket.IsClosed())
                        ++_openBugTicketCount;
                    break;
                case "ComplaintTicket":
                    _complaintTicketList[ticket.GetId()] = ticket as ComplaintTicket;
                    if (!ticket.IsClosed())
                        ++_openComplaintTicketCount;
                    break;
                case "SuggestionTicket":
                    _suggestionTicketList[ticket.GetId()] = ticket as SuggestionTicket;
                    if (!ticket.IsClosed())
                        ++_openSuggestionTicketCount;
                    break;
            }

            ticket.SaveToDB();
        }

        public void RemoveTicket<T>(uint ticketId) where T : Ticket
        {
            T ticket = GetTicket<T>(ticketId);
            if (ticket != null)
            {
                ticket.DeleteFromDB();

                switch (typeof(T).Name)
                {
                    case "BugTicket":
                        _bugTicketList.Remove(ticketId);
                        break;
                    case "ComplaintTicket":
                        _complaintTicketList.Remove(ticketId);
                        break;
                    case "SuggestionTicket":
                        _suggestionTicketList.Remove(ticketId);
                        break;
                }
            }
        }

        public void CloseTicket<T>(uint ticketId, ObjectGuid closedBy) where T : Ticket
        {
            T ticket = GetTicket<T>(ticketId);
            if (ticket != null)
            {
                ticket.SetClosedBy(closedBy);
                if (!closedBy.IsEmpty())
                {
                    switch (typeof(T).Name)
                    {
                        case "BugTicket":
                            --_openBugTicketCount;
                            break;
                        case "ComplaintTicket":
                            --_openComplaintTicketCount;
                            break;
                        case "SuggestionTicket":
                            --_openSuggestionTicketCount;
                            break;
                    }
                }
                ticket.SaveToDB();
            }
        }

        public void ResetTickets<T>() where T : Ticket
        {
            PreparedStatement stmt;
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    _bugTicketList.Clear();

                    _lastBugId = 0;

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_BUGS);
                    DB.Characters.Execute(stmt);
                    break;
                case "ComplaintTicket":
                    _complaintTicketList.Clear();

                    _lastComplaintId = 0;

                    SQLTransaction trans = new SQLTransaction();
                    trans.Append(DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_COMPLAINTS));
                    trans.Append(DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_COMPLAINT_CHATLOGS));
                    DB.Characters.CommitTransaction(trans);
                    break;
                case "SuggestionTicket":
                    _suggestionTicketList.Clear();

                    _lastSuggestionId = 0;

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_SUGGESTIONS);
                    DB.Characters.Execute(stmt);
                    break;
            }


        }

        public void ShowList<T>(CommandHandler handler) where T : Ticket
        {
            handler.SendSysMessage(CypherStrings.CommandTicketshowlist);
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    foreach (var ticket in _bugTicketList.Values)
                        if (!ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
                case "ComplaintTicket":
                    foreach (var ticket in _complaintTicketList.Values)
                        if (!ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
                case "SuggestionTicket":
                    foreach (var ticket in _suggestionTicketList.Values)
                        if (!ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
            }
        }

        public void ShowClosedList<T>(CommandHandler handler) where T : Ticket
        {
            handler.SendSysMessage(CypherStrings.CommandTicketshowclosedlist);
            switch (typeof(T).Name)
            {
                case "BugTicket":
                    foreach (var ticket in _bugTicketList.Values)
                        if (ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
                case "ComplaintTicket":
                    foreach (var ticket in _complaintTicketList.Values)
                        if (ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
                case "SuggestionTicket":
                    foreach (var ticket in _suggestionTicketList.Values)
                        if (ticket.IsClosed())
                            handler.SendSysMessage(ticket.FormatViewMessageString(handler));
                    break;
            }
        }

        long GetAge(ulong t) { return (Time.UnixTime - (long)t) / Time.Day; }

        IEnumerable<KeyValuePair<uint, ComplaintTicket>> GetComplaintsByPlayerGuid(ObjectGuid playerGuid)
        {
            return _complaintTicketList.Where(ticket => ticket.Value.GetPlayerGuid() == playerGuid);
        }

        public bool GetSupportSystemStatus() { return _supportSystemStatus; }
        public bool GetTicketSystemStatus() { return _supportSystemStatus && _ticketSystemStatus; }
        public bool GetBugSystemStatus() { return _supportSystemStatus && _bugSystemStatus; }
        public bool GetComplaintSystemStatus() { return _supportSystemStatus && _complaintSystemStatus; }
        public bool GetSuggestionSystemStatus() { return _supportSystemStatus && _suggestionSystemStatus; }
        public ulong GetLastChange() { return _lastChange; }

        public void SetSupportSystemStatus(bool status) { _supportSystemStatus = status; }
        public void SetTicketSystemStatus(bool status) { _ticketSystemStatus = status; }
        public void SetBugSystemStatus(bool status) { _bugSystemStatus = status; }
        public void SetComplaintSystemStatus(bool status) { _complaintSystemStatus = status; }
        public void SetSuggestionSystemStatus(bool status) { _suggestionSystemStatus = status; }

        public void UpdateLastChange() { _lastChange = (ulong)Time.UnixTime; }

        public uint GenerateBugId() { return ++_lastBugId; }
        public uint GenerateComplaintId() { return ++_lastComplaintId; }
        public uint GenerateSuggestionId() { return ++_lastSuggestionId; }

        bool _supportSystemStatus;
        bool _ticketSystemStatus;
        bool _bugSystemStatus;
        bool _complaintSystemStatus;
        bool _suggestionSystemStatus;
        Dictionary<uint, BugTicket> _bugTicketList = new Dictionary<uint, BugTicket>();
        Dictionary<uint, ComplaintTicket> _complaintTicketList = new Dictionary<uint, ComplaintTicket>();
        Dictionary<uint, SuggestionTicket> _suggestionTicketList = new Dictionary<uint, SuggestionTicket>();
        uint _lastBugId;
        uint _lastComplaintId;
        uint _lastSuggestionId;
        uint _openBugTicketCount;
        uint _openComplaintTicketCount;
        uint _openSuggestionTicketCount;
        ulong _lastChange;
    }
}
