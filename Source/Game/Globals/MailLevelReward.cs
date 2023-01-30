// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class MailLevelReward
    {
        public MailLevelReward(ulong _raceMask = 0, uint _mailTemplateId = 0, uint _senderEntry = 0)
        {
            RaceMask = _raceMask;
            MailTemplateId = _mailTemplateId;
            SenderEntry = _senderEntry;
        }

        public uint MailTemplateId { get; set; }

        public ulong RaceMask { get; set; }
        public uint SenderEntry { get; set; }
    }
}