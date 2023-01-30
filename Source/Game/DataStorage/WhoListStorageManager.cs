// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;
using Game.Guilds;

namespace Game.DataStorage
{
    public class WhoListStorageManager : Singleton<WhoListStorageManager>
    {
        private readonly List<WhoListPlayerInfo> _whoListStorage;

        private WhoListStorageManager()
        {
            _whoListStorage = new List<WhoListPlayerInfo>();
        }

        public void Update()
        {
            // clear current list
            _whoListStorage.Clear();

            var players = Global.ObjAccessor.GetPlayers();

            foreach (var player in players)
            {
                if (player.GetMap() == null ||
                    player.GetSession().PlayerLoading())
                    continue;

                string playerName = player.GetName();
                string guildName = Global.GuildMgr.GetGuildNameById((uint)player.GetGuildId());

                Guild guild = player.GetGuild();
                ObjectGuid guildGuid = ObjectGuid.Empty;

                if (guild)
                    guildGuid = guild.GetGUID();

                _whoListStorage.Add(new WhoListPlayerInfo(player.GetGUID(),
                                                          player.GetTeam(),
                                                          player.GetSession().GetSecurity(),
                                                          player.GetLevel(),
                                                          player.GetClass(),
                                                          player.GetRace(),
                                                          player.GetZoneId(),
                                                          (byte)player.GetNativeGender(),
                                                          player.IsVisible(),
                                                          player.IsGameMaster(),
                                                          playerName,
                                                          guildName,
                                                          guildGuid));
            }
        }

        public List<WhoListPlayerInfo> GetWhoList()
        {
            return _whoListStorage;
        }
    }
}