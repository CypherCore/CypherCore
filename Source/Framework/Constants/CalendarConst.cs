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

namespace Framework.Constants
{
    public enum CalendarMailAnswers
    {
        EventRemovedMailSubject = 0,
        InviteRemovedMailSubject = 0x100
    }

    public enum CalendarFlags
    {
        AllAllowed = 0x001,
        InvitesLocked = 0x010,
        WithoutInvites = 0x040,
        GuildEvent = 0x400
    }

    public enum CalendarModerationRank
    {
        Player = 0,
        Moderator = 1,
        Owner = 2
    }

    public enum CalendarSendEventType
    {
        Get = 0,
        Add = 1,
        Copy = 2
    }

    public enum CalendarEventType
    {
        Raid = 0,
        Dungeon = 1,
        Pvp = 2,
        Meeting = 3,
        Other = 4,
        Heroic = 5
    }

    public enum CalendarRepeatType
    {
        Never = 0,
        Weekly = 1,
        Biweekly = 2,
        Monthly = 3
    }

    public enum CalendarInviteStatus
    {
        Invited = 0,
        Accepted = 1,
        Declined = 2,
        Confirmed = 3,
        Out = 4,
        Standby = 5,
        SignedUp = 6,
        NotSignedUp = 7,
        Tentative = 8,
        Removed = 9     // Correct Name?
    }

    public enum CalendarError
    {
        Ok = 0,
        GuildEventsExceeded = 1,
        EventsExceeded = 2,
        SelfInvitesExceeded = 3,
        OtherInvitesExceeded = 4,
        Permissions = 5,
        EventInvalid = 6,
        NotInvited = 7,
        Internal = 8,
        GuildPlayerNotInGuild = 9,
        AlreadyInvitedToEventS = 10,
        PlayerNotFound = 11,
        NotAllied = 12,
        IgnoringYouS = 13,
        InvitesExceeded = 14,
        InvalidDate = 16,
        InvalidTime = 17,

        NeedsTitle = 19,
        EventPassed = 20,
        EventLocked = 21,
        DeleteCreatorFailed = 22,
        SystemDisabled = 24,
        RestrictedAccount = 25,
        ArenaEventsExceeded = 26,
        RestrictedLevel = 27,
        UserSquelched = 28,
        NoInvite = 29,

        EventWrongServer = 36,
        InviteWrongServer = 37,
        NoGuildInvites = 38,
        InvalidSignup = 39,
        NoModerator = 40
    }
}
