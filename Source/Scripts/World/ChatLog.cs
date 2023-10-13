// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Scripting;

namespace Scripts.World.Achievements
{
    [Script]
    class ChatLogScript : PlayerScript
    {
        public ChatLogScript() : base("ChatLogScript") { }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg)
        {
            switch (type)
            {
                case ChatMsg.Say:
                    LogChat($"Player {player.GetName()} says (language {lang}): {msg}");
                    break;

                case ChatMsg.Emote:
                    LogChat($"Player {player.GetName()} emotes: {msg}");
                    break;

                case ChatMsg.Yell:
                    LogChat($"Player {player.GetName()} yells (language {lang}): {msg}");
                    break;
            }
        }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver)
        {
            LogChat($"Player {player.GetName()} tells {(receiver != null ? receiver.GetName() : "<unknown>")}: {msg}");
        }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg, Group group)
        {
            //! Note:
            //! LangAddon can only be sent by client in "Party", "Raid", "Guild", "Battleground", "Whisper"
            switch (type)
            {
                case ChatMsg.Party:
                    LogChat($"Player {player.GetName()} tells group with leader {(group != null ? group.GetLeaderName() : "<unknown>")}: {msg}");
                    break;

                case ChatMsg.PartyLeader:
                    LogChat($"Leader {player.GetName()} tells group: {msg}");
                    break;

                case ChatMsg.Raid:
                    LogChat($"Player {player.GetName()} tells raid with leader {(group != null ? group.GetLeaderName() : "<unknown>")}: {msg}");
                    break;

                case ChatMsg.RaidLeader:
                    LogChat($"Leader player {player.GetName()} tells raid: {msg}");
                    break;

                case ChatMsg.RaidWarning:
                    LogChat($"Leader player {player.GetName()} warns raid with: {msg}");
                    break;

                case ChatMsg.InstanceChat:
                    LogChat($"Player {player.GetName()} tells instance with leader {(group != null ? group.GetLeaderName() : "<unknown>")}: {msg}");
                    break;

                case ChatMsg.InstanceChatLeader:
                    LogChat($"Leader player {player.GetName()} tells instance: {msg}");
                    break;
            }
        }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg, Guild guild)
        {
            switch (type)
            {
                case ChatMsg.Guild:
                    LogChat($"Player {player.GetName()} tells guild {(guild != null ? guild.GetName() : "<unknown>")}: {msg}");
                    break;

                case ChatMsg.Officer:
                    LogChat($"Player {player.GetName()} tells guild {(guild != null ? guild.GetName() : "<unknown>")} officers: {msg}");
                    break;
            }
        }

        public override void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel)
        {
            bool isSystem = channel != null &&
                            (channel.HasFlag(ChannelFlags.Trade) ||
                             channel.HasFlag(ChannelFlags.General) ||
                             channel.HasFlag(ChannelFlags.City) ||
                             channel.HasFlag(ChannelFlags.Lfg));

            if (isSystem)
            {
                LogChat($"Player {player.GetName()} tells channel {channel.GetName()}: {msg}");
            }
            else
            {
                string channelName = channel != null ? channel.GetName() : "<unknown>";
                LogChat($"Player {player.GetName()} tells channel {channelName}: {msg}");
            }
        }

        void LogChat(string msg)
        {
            Log.outDebug(LogFilter.ChatLog, msg);
        }
    }
}