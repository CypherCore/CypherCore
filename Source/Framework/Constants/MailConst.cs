// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.﻿

namespace Framework.Constants
{
    public enum MailMessageType
    {
        Normal = 0,
        Auction = 2,
        Creature = 3,
        Gameobject = 4,
        Calendar = 5,
        Blackmarket = 6,
        CommerceAuction = 7,
        Auction2 = 8,
        ArtisansConsortium = 9
    }

    public enum MailCheckMask
    {
        None = 0x00,
        Read = 0x01,
        Returned = 0x02,                     // This Mail Was Returned. Do Not Allow Returning Mail Back Again.
        Copied = 0x04,                     // This Mail Was Copied. Do Not Allow Making A Copy Of Items In Mail.
        CodPayment = 0x08,
        HasBody = 0x10                      // This Mail Has Body Text.
    }

    public enum MailStationery
    {
        Test = 1,
        Default = 41,
        Gm = 61,
        Auction = 62,
        Val = 64,                           // Valentine
        Chr = 65,                           // Christmas
        Orp = 67                            // Orphan
    }

    public enum MailState
    {
        Unchanged = 1,
        Changed = 2,
        Deleted = 3
    }

    public enum MailShowFlags
    {
        Unk0 = 0x0001,
        Delete = 0x0002,                             // Forced Show Delete Button Instead Return Button
        Auction = 0x0004,                             // From Old Comment
        Unk2 = 0x0008,                             // Unknown, Cod Will Be Shown Even Without That Flag
        Return = 0x0010
    }

    public enum MailResponseType
    {
        Send = 0,
        MoneyTaken = 1,
        ItemTaken = 2,
        ReturnedToSender = 3,
        Deleted = 4,
        MadePermanent = 5
    }

    public enum MailResponseResult
    {
        Ok = 0,
        EquipError = 1,
        CannotSendToSelf = 2,
        NotEnoughMoney = 3,
        RecipientNotFound = 4,
        NotYourTeam = 5,
        InternalError = 6,
        DisabledForTrialAcc = 14,
        RecipientCapReached = 15,
        CantSendWrappedCod = 16,
        MailAndChatSuspended = 17,
        TooManyAttachments = 18,
        MailAttachmentInvalid = 19,
        ItemHasExpired = 21
    }
}
