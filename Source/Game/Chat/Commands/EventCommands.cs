// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Chat
{
    [CommandGroup("event")]
    internal class EventCommands
    {
        [Command("info", RBACPermissions.CommandEventInfo, true)]
        private static bool HandleEventInfoCommand(CommandHandler handler, ushort eventId)
        {
            var events = Global.GameEventMgr.GetEventMap();

            if (eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);

                return false;
            }

            GameEventData eventData = events[eventId];

            if (!eventData.IsValid())
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);

                return false;
            }

            var activeEvents = Global.GameEventMgr.GetActiveEventList();
            bool active = activeEvents.Contains(eventId);
            string activeStr = active ? Global.ObjectMgr.GetCypherString(CypherStrings.Active) : "";

            string startTimeStr = Time.UnixTimeToDateTime(eventData.Start).ToLongDateString();
            string endTimeStr = Time.UnixTimeToDateTime(eventData.End).ToLongDateString();

            uint delay = Global.GameEventMgr.NextCheck(eventId);
            long nextTime = GameTime.GetGameTime() + delay;
            string nextStr = nextTime >= eventData.Start && nextTime < eventData.End ? Time.UnixTimeToDateTime(GameTime.GetGameTime() + delay).ToShortTimeString() : "-";

            string occurenceStr = Time.secsToTimeString(eventData.Occurence * Time.Minute);
            string lengthStr = Time.secsToTimeString(eventData.Length * Time.Minute);

            handler.SendSysMessage(CypherStrings.EventInfo,
                                   eventId,
                                   eventData.Description,
                                   activeStr,
                                   startTimeStr,
                                   endTimeStr,
                                   occurenceStr,
                                   lengthStr,
                                   nextStr);

            return true;
        }

        [Command("activelist", RBACPermissions.CommandEventActivelist, true)]
        private static bool HandleEventActiveListCommand(CommandHandler handler)
        {
            uint counter = 0;

            var events = Global.GameEventMgr.GetEventMap();
            var activeEvents = Global.GameEventMgr.GetActiveEventList();

            string active = Global.ObjectMgr.GetCypherString(CypherStrings.Active);

            foreach (var eventId in activeEvents)
            {
                GameEventData eventData = events[eventId];

                if (handler.GetSession() != null)
                    handler.SendSysMessage(CypherStrings.EventEntryListChat, eventId, eventId, eventData.Description, active);
                else
                    handler.SendSysMessage(CypherStrings.EventEntryListConsole, eventId, eventData.Description, active);

                ++counter;
            }

            if (counter == 0)
                handler.SendSysMessage(CypherStrings.Noeventfound);

            return true;
        }

        [Command("start", RBACPermissions.CommandEventStart, true)]
        private static bool HandleEventStartCommand(CommandHandler handler, ushort eventId)
        {
            var events = Global.GameEventMgr.GetEventMap();

            if (eventId < 1 ||
                eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);

                return false;
            }

            GameEventData eventData = events[eventId];

            if (!eventData.IsValid())
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
        private static bool HandleEventStopCommand(CommandHandler handler, ushort eventId)
        {
            var events = Global.GameEventMgr.GetEventMap();

            if (eventId < 1 ||
                eventId >= events.Length)
            {
                handler.SendSysMessage(CypherStrings.EventNotExist);

                return false;
            }

            GameEventData eventData = events[eventId];

            if (!eventData.IsValid())
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