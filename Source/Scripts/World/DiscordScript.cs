using System;
using System.Collections.Generic;
using System.Text;
using Framework.Configuration;
using Framework.Constants;
using Game.Chat;
using Game.Discord;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    [Script]
    class DiscordScript : PlayerScript
    {
        public DiscordScript() : base("DiscordScript") {
        }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel)
        {
            base.OnChat(player, type, lang, msg, channel);

            if (ConfigMgr.GetDefaultValue("Discord.Enabled", false))
            {
                if (player.GetSession() && channel.GetName() == "world")
                {
                    DiscordMessage newMessage = new DiscordMessage
                                                {
                                                    Channel = player.GetTeamId() == (int)Team.Alliance ? DiscordMessageChannel.Discord_World_A : DiscordMessageChannel.Discord_World_H,
                                                    IsGm = player.isGMChat(),
                                                    CharacterName = player.GetName(),
                                                    Message = msg
                                                };

                    DiscordMessageQueue.Add( newMessage );
                }
            }
            
        }
    }
}
