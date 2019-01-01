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

namespace Game.Chat
{
    [CommandGroup("event", RBACPermissions.CommandEvent)]
    class EventCommands
    {
        [Command("info", RBACPermissions.CommandEvent, true)]
        static bool HandleEventInfoCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // id or [name] Shift-click form |color|Hgameevent:id|h[name]|h|r
            string id = handler.extractKeyFromLink(args, "Hgameevent");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!ushort.TryParse(id, out ushort eventId))
                return false;

            var events = Global.GameEventMgr.GetEventMap();
            if (eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            GameEventData eventData = events[eventId];
            if (!eventData.isValid())
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            var activeEvents = Global.GameEventMgr.GetActiveEventList();
            bool active = activeEvents.Contains(eventId);
            string activeStr = active ? Global.ObjectMgr.GetCypherString(CypherStrings.Active) : "";

            string startTimeStr = Time.UnixTimeToDateTime(eventData.start).ToLongDateString();
            string endTimeStr = Time.UnixTimeToDateTime(eventData.end).ToLongDateString();

            uint delay = Global.GameEventMgr.NextCheck(eventId);
            long nextTime = Time.UnixTime + delay;
            string nextStr = nextTime >= eventData.start && nextTime < eventData.end ? Time.UnixTimeToDateTime(Time.UnixTime + delay).ToShortTimeString() : "-";

            string occurenceStr = Time.secsToTimeString(eventData.occurence * Time.Minute);
            string lengthStr = Time.secsToTimeString(eventData.length * Time.Minute);

            handler.SendSysMessage(CypherStrings.EventInfo, eventId, eventData.description, activeStr,
                startTimeStr, endTimeStr, occurenceStr, lengthStr, nextStr);
            return true;
        }

        [Command("activelist", RBACPermissions.CommandEventActivelist, true)]
        static bool HandleEventActiveListCommand(StringArguments args, CommandHandler handler)
        {
            uint counter = 0;

            var events = Global.GameEventMgr.GetEventMap();
            var activeEvents = Global.GameEventMgr.GetActiveEventList();

            string active = Global.ObjectMgr.GetCypherString(CypherStrings.Active);

            foreach (var eventId in activeEvents)
            {
                GameEventData eventData = events[eventId];

                if (handler.GetSession() != null)
                    handler.SendSysMessage(CypherStrings.EventEntryListChat, eventId, eventId, eventData.description, active);
                else
                    handler.SendSysMessage(CypherStrings.EventEntryListConsole, eventId, eventData.description, active);

                ++counter;
            }

            if (counter == 0)
                handler.SendSysMessage(CypherStrings.Noeventfound);

            return true;
        }

        [Command("start", RBACPermissions.CommandEventStart, true)]
        static bool HandleEventStartCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // id or [name] Shift-click form |color|Hgameevent:id|h[name]|h|r
            string id = handler.extractKeyFromLink(args, "Hgameevent");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!ushort.TryParse(id, out ushort eventId))
                return false;

            var events = Global.GameEventMgr.GetEventMap();
            if (eventId < 1 || eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            GameEventData eventData = events[eventId];
            if (!eventData.isValid())
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            var activeEvents = Global.GameEventMgr.GetActiveEventList();
            if (activeEvents.Contains(eventId))
            {
                handler.SendSysMessage(CypherStrings.EventAlreadyActive, eventId);
                return false;
            }

            Global.GameEventMgr.StartEvent(eventId, true);
            return true;
        }

        [Command("stop", RBACPermissions.CommandEventStop, true)]
        static bool HandleEventStopCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // id or [name] Shift-click form |color|Hgameevent:id|h[name]|h|r
            string id = handler.extractKeyFromLink(args, "Hgameevent");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!ushort.TryParse(id, out ushort eventId))
                return false;

            var events = Global.GameEventMgr.GetEventMap();
            if (eventId < 1 || eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            GameEventData eventData = events[eventId];
            if (!eventData.isValid())
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);
                return false;
            }

            var activeEvents = Global.GameEventMgr.GetActiveEventList();

            if (!activeEvents.Contains(eventId))
            {
                handler.SendSysMessage(CypherStrings.EventNotActive, eventId);
                return false;
            }

            Global.GameEventMgr.StopEvent(eventId, true);
            return true;
        }
    }
}
