/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Guilds;
using System.Collections.Generic;

namespace Game.DataStorage
{
    public class WhoListPlayerInfo
    {
        public WhoListPlayerInfo(ObjectGuid guid, Team team, AccountTypes security, uint level, Class clss, Race race, uint zoneid, byte gender, bool visible, bool gamemaster, string playerName, string guildName, ObjectGuid guildguid)
        {
            Guid = guid;
            Team = team;
            Security = security;
            Level = level;
            Class = (byte)clss;
            Race = (byte)race;
            ZoneId = zoneid;
            Gender = gender;
            IsVisible = visible;
            IsGamemaster = gamemaster;
            PlayerName = playerName;
            GuildName = guildName;
            GuildGuid = guildguid;
        }

        public ObjectGuid Guid { get; }
        public Team Team { get; }
        public AccountTypes Security { get; }
        public uint Level { get; }
        public byte Class { get; }
        public byte Race { get; }
        public uint ZoneId { get; }
        public byte Gender { get; }
        public bool IsVisible { get; }
        public bool IsGamemaster { get; }
        public string PlayerName { get; }
        public string GuildName { get; }
        public ObjectGuid GuildGuid { get; }
    }

    public class WhoListStorageManager : Singleton<WhoListStorageManager>
    {
        List<WhoListPlayerInfo> _whoListStorage;

        WhoListStorageManager()
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
                if (player.GetMap() == null || player.GetSession().PlayerLoading())
                    continue;

                string playerName = player.GetName();
                string guildName = Global.GuildMgr.GetGuildNameById((uint)player.GetGuildId());

                Guild guild = player.GetGuild();
                ObjectGuid guildGuid = ObjectGuid.Empty;

                if (guild)
                    guildGuid = guild.GetGUID();

                _whoListStorage.Add(new WhoListPlayerInfo(player.GetGUID(), player.GetTeam(), player.GetSession().GetSecurity(), player.GetLevel(),
                    player.GetClass(), player.GetRace(), player.GetZoneId(), player.m_playerData.NativeSex, player.IsVisible(),
                    player.IsGameMaster(), playerName, guildName, guildGuid));
            }
        }

        public List<WhoListPlayerInfo> GetWhoList() { return _whoListStorage; }
    }
}
