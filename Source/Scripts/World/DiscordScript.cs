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
                if (player.GetSession() && channel.GetName() == ConfigMgr.GetDefaultValue("Discord.GameChannelName", "world"))
                {
                    DiscordMessageChannel channel_discordMessage = player.GetTeamId() == (int)Team.Alliance ? DiscordMessageChannel.Discord_World_A : DiscordMessageChannel.Discord_World_H;

                    if (ConfigMgr.GetDefaultValue("AllowTwoSide.Interaction.Channel", false))
                        channel_discordMessage = DiscordMessageChannel.Discord_Both;

                    DiscordMessage newMessage = new DiscordMessage
                                                {
                                                    Channel = channel_discordMessage,
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
