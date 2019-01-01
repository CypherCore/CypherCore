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
