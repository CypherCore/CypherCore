// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    /// Client error enum See Blizzard_StoreUISecure.lua Last update : 9.0.2 36474
    public enum BpayError : uint
    {
        Ok = 0,
        PurchaseDenied = 1,
        PaymentFailed = 2,
        Other = 3,
        WrongCurrency = 12,
        BattlepayDisabled = 13,
        InvalidPaymentMethod = 25,
        InsufficientBalance = 28,
        ParentalControlsNoPurchase = 34,
        ConsumableTokenOwned = 46,
        TooManyTokens = 47
    }
}
