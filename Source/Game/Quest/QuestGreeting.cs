// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class QuestGreeting
    {
        public uint EmoteDelay { get; set; }

        public ushort EmoteType { get; set; }
        public string Text { get; set; }

        public QuestGreeting()
        {
            Text = "";
        }

        public QuestGreeting(ushort emoteType, uint emoteDelay, string text)
        {
            EmoteType = emoteType;
            EmoteDelay = emoteDelay;
            Text = text;
        }
    }
}