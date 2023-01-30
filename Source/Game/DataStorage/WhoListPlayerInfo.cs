// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

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
}