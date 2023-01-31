using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    public enum BpayDistributionStatus
    {
        NONE = 0,
        AVAILABLE = 1,
        ADD_TO_PROCESS = 2,
        PROCESS_COMPLETE = 3,
        FINISHED = 4
    }
}
