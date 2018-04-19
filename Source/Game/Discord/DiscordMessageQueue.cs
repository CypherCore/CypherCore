using System;
using System.Collections.Generic;
using System.Text;
using Game.Network.Packets;

namespace Game.Discord
{
    public static class DiscordMessageQueue
    {
        public static readonly List<DiscordMessage> DiscordMessages = new List<DiscordMessage>();

        public static void Add(DiscordMessage message)
        {
            DiscordMessages.Add( message );
        }

        public static void Clear()
        {
            DiscordMessages.Clear();
        }

        public static bool Empty()
        {
            return DiscordMessages.Count == 0;
        }
    }
}
