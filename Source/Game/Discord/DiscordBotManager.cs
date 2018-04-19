using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Game.Discord
{
    public static class DiscordBotManager
    {
        public static async Task MessageReceived(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("Pong!");
        }
    }
}
