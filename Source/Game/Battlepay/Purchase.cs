using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Battlepay
{
    public class Purchase
    {
        public ObjectGuid TargetCharacter = new ObjectGuid();
        public ulong DistributionId;
        public ulong PurchaseID;
        public ulong CurrentPrice;
        public uint ClientToken;
        public uint ServerToken;
        public uint ProductID;
        public ushort Status;
        public bool Lock;
    }
}
