// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting.Interfaces;

namespace Game.Scripting.Interfaces.IAuctionHouse
{
    public interface IAuctionHouseOnAuctionSuccessful : IScriptObject
    {
        void OnAuctionSuccessful(AuctionHouseObject ah, AuctionPosting auction);
    }
}