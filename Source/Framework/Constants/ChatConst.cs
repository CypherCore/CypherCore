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
 */﻿

namespace Framework.Constants
{
    public enum ChatNotify
    {
        JoinedNotice = 0x00,           //+ "%S Joined Channel.";
        LeftNotice = 0x01,           //+ "%S Left Channel.";
        //SuspendedNotice             = 0x01,           // "%S Left Channel.";
        YouJoinedNotice = 0x02,           //+ "Joined Channel: [%S]"; -- You Joined
        //YouChangedNotice           = 0x02,           // "Changed Channel: [%S]";
        YouLeftNotice = 0x03,           //+ "Left Channel: [%S]"; -- You Left
        WrongPasswordNotice = 0x04,           //+ "Wrong Password For %S.";
        NotMemberNotice = 0x05,           //+ "Not On Channel %S.";
        NotModeratorNotice = 0x06,           //+ "Not A Moderator Of %S.";
        PasswordChangedNotice = 0x07,           //+ "[%S] Password Changed By %S.";
        OwnerChangedNotice = 0x08,           //+ "[%S] Owner Changed To %S.";
        PlayerNotFoundNotice = 0x09,           //+ "[%S] Player %S Was Not Found.";
        NotOwnerNotice = 0x0a,           //+ "[%S] You Are Not The Channel Owner.";
        ChannelOwnerNotice = 0x0b,           //+ "[%S] Channel Owner Is %S.";
        ModeChangeNotice = 0x0c,           //?
        AnnouncementsOnNotice = 0x0d,           //+ "[%S] Channel Announcements Enabled By %S.";
        AnnouncementsOffNotice = 0x0e,           //+ "[%S] Channel Announcements Disabled By %S.";
        ModerationOnNotice = 0x0f,           //+ "[%S] Channel Moderation Enabled By %S.";
        ModerationOffNotice = 0x10,           //+ "[%S] Channel Moderation Disabled By %S.";
        MutedNotice = 0x11,           //+ "[%S] You Do Not Have Permission To Speak.";
        PlayerKickedNotice = 0x12,           //? "[%S] Player %S Kicked By %S.";
        BannedNotice = 0x13,           //+ "[%S] You Are Bannedstore From That Channel.";
        PlayerBannedNotice = 0x14,           //? "[%S] Player %S Bannedstore By %S.";
        PlayerUnbannedNotice = 0x15,           //? "[%S] Player %S Unbanned By %S.";
        PlayerNotBannedNotice = 0x16,           //+ "[%S] Player %S Is Not Bannedstore.";
        PlayerAlreadyMemberNotice = 0x17,           //+ "[%S] Player %S Is Already On The Channel.";
        InviteNotice = 0x18,           //+ "%2$S Has Invited You To Join The Channel '%1$S'.";
        InviteWrongFactionNotice = 0x19,           //+ "Target Is In The Wrong Alliance For %S.";
        WrongFactionNotice = 0x1a,           //+ "Wrong Alliance For %S.";
        InvalidNameNotice = 0x1b,           //+ "Invalid Channel Name";
        NotModeratedNotice = 0x1c,           //+ "%S Is Not Moderated";
        PlayerInvitedNotice = 0x1d,           //+ "[%S] You Invited %S To Join The Channel";
        PlayerInviteBannedNotice = 0x1e,           //+ "[%S] %S Has Been Bannedstore.";
        ThrottledNotice = 0x1f,           //+ "[%S] The Number Of Messages That Can Be Sent To This Channel Is Limited, Please Wait To Send Another Message.";
        NotInAreaNotice = 0x20,           //+ "[%S] You Are Not In The Correct Area For This Channel."; -- The User Is Trying To Send A Chat To A Zone Specific Channel, And They'Re Not Physically In That Zone.
        NotInLfgNotice = 0x21,           //+ "[%S] You Must Be Queued In Looking For Group Before Joining This Channel."; -- The User Must Be In The Looking For Group System To Join Lfg Chat Channels.
        VoiceOnNotice = 0x22,           //+ "[%S] Channel Voice Enabled By %S.";
        VoiceOffNotice = 0x23,            //+ "[%S] Channel Voice Disabled By %S.";
        TrialRestricted = 0x24,
        NotAllowedInChannel = 0x25
    }

    public enum ChannelFlags
    {
        None = 0x00,
        Custom = 0x01,
        // 0x02
        Trade = 0x04,
        NotLfg = 0x08,
        General = 0x10,
        City = 0x20,
        Lfg = 0x40,
        Voice = 0x80
        // General                  0x18 = 0x10 | 0x08
        // Trade                    0x3C = 0x20 | 0x10 | 0x08 | 0x04
        // LocalDefence             0x18 = 0x10 | 0x08
        // GuildRecruitment         0x38 = 0x20 | 0x10 | 0x08
        // LookingForGroup          0x50 = 0x40 | 0x10
    }

    public enum ChannelDBCFlags
    {
        None = 0x00000,
        Initial = 0x00001,              // General, Trade, Localdefense, Lfg
        ZoneDep = 0x00002,              // General, Trade, Localdefense, Guildrecruitment
        Global = 0x00004,              // Worlddefense
        Trade = 0x00008,              // Trade, Lfg
        CityOnly = 0x00010,              // Trade, Guildrecruitment, Lfg
        CityOnly2 = 0x00020,              // Trade, Guildrecruitment, Lfg
        Defense = 0x10000,              // Localdefense, Worlddefense
        GuildReq = 0x20000,              // Guildrecruitment
        Lfg = 0x40000,              // Lfg
        Unk1 = 0x80000               // General
    }

    public enum ChannelMemberFlags
    {
        None = 0x00,
        Owner = 0x01,
        Moderator = 0x02,
        Voiced = 0x04,
        Muted = 0x08,
        Custom = 0x10,
        MicMuted = 0x20
        // 0x40
        // 0x80
    }
}
