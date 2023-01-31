using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    public enum VasPurchaseProgress
    {
        Invalid = 0,
        PrePurchase = 1,
        PaymentPending = 2,
        ApplyingLicense = 3,
        WaitingOnQueue = 4,
        Ready = 5,
        ProcessingFactionChange = 6,
        Complete = 7,
    }
}
