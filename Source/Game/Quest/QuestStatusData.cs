// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public class QuestStatusData
    {
        public bool Explored { get; set; }
        public ushort Slot { get; set; } = SharedConst.MaxQuestLogSize;
        public QuestStatus Status { get; set; }
        public uint Timer { get; set; }
    }
}