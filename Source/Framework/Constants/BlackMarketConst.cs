// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public struct BlackMarketConst
    {
        public const ulong MaxBid = 1000000UL* MoneyConstants.Gold;
    }
    public enum BlackMarketError
    {
        Ok = 0,
        ItemNotFound = 1,
        AlreadyBid = 2,
        HigherBid = 4,
        DatabaseError = 6,
        NotEnoughMoney = 7,
        RestrictedAccountTrial = 9
    }

    public enum BMAHMailAuctionAnswers
    {
        Outbid = 0,
        Won = 1,
    }
}
