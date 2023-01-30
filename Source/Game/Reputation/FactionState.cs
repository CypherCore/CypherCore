// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public class FactionState
    {
        public ReputationFlags Flags { get; set; }
        public uint Id { get; set; }
        public bool NeedSave { get; set; }
        public bool NeedSend { get; set; }
        public uint ReputationListID { get; set; }
        public int Standing { get; set; }
        public int VisualStandingIncrease { get; set; }
    }
}