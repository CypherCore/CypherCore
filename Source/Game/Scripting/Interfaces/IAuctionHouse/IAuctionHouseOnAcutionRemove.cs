// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Scripting.Interfaces.IAuctionHouse
{
    public interface IAuctionHouseOnAcutionRemove : IScriptObject
    {
        void OnAuctionRemove(AuctionHouseObject ah, AuctionPosting auction);
    }
}