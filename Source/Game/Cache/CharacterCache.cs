using System;
using System.Collections.Generic;
using Game.Entities;
using Framework.Database;
using Framework.Constants;
using Game.Arenas;
using Game.Networking.Packets;

namespace Game.Cache
{
    public class CharacterCache : Singleton<CharacterCache>
    {
        Dictionary<ObjectGuid, CharacterCacheEntry> _characterCacheStore = new Dictionary<ObjectGuid, CharacterCacheEntry>();
        Dictionary<string, CharacterCacheEntry> _characterCacheByNameStore = new Dictionary<string, CharacterCacheEntry>();

        CharacterCache() { }

        public void LoadCharacterCacheStorage()
        {
            _characterCacheStore.Clear();
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.Characters.Query("SELECT guid, name, account, race, gender, class, level, deleteDate FROM characters");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "No character name data loaded, empty query");
                return;
            }

            do
            {
                AddCharacterCacheEntry(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(2), result.Read<string>(1), result.Read<byte>(4), result.Read<byte>(3), result.Read<byte>(5), result.Read<byte>(6), result.Read<uint>(7) != 0);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded character infos for {_characterCacheStore.Count} characters in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void AddCharacterCacheEntry(ObjectGuid guid, uint accountId, string name, byte gender, byte race, byte playerClass, byte level, bool isDeleted)
        {
            var data = new CharacterCacheEntry();
            data.Guid = guid;
            data.Name = name;
            data.AccountId = accountId;
            data.RaceId = (Race)race;
            data.Sex = (Gender)gender;
            data.ClassId = (Class)playerClass;
            data.Level = level;
            data.GuildId = 0;                           // Will be set in guild loading or guild setting
            for (byte i = 0; i < SharedConst.MaxArenaSlot; ++i)
                data.ArenaTeamId[i] = 0;                // Will be set in arena teams loading
            data.IsDeleted = isDeleted;

            // Fill Name to Guid Store
            _characterCacheByNameStore[name] = data;
            _characterCacheStore[guid] = data;
        }

        public void DeleteCharacterCacheEntry(ObjectGuid guid, string name)
        {
            _characterCacheStore.Remove(guid);
            _characterCacheByNameStore.Remove(name);
        }

        public void UpdateCharacterData(ObjectGuid guid, string name, byte? gender = null, byte? race = null)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return;

            string oldName = characterCacheEntry.Name;
            characterCacheEntry.Name = name;

            if (gender.HasValue)
                characterCacheEntry.Sex = (Gender)gender.Value;

            if (race.HasValue)
                characterCacheEntry.RaceId = (Race)race.Value;

            InvalidatePlayer invalidatePlayer = new InvalidatePlayer();
            invalidatePlayer.Guid = guid;
            Global.WorldMgr.SendGlobalMessage(invalidatePlayer);

            // Correct name -> pointer storage
            _characterCacheByNameStore.Remove(oldName);
            _characterCacheByNameStore[name] = characterCacheEntry;
        }

        public void UpdateCharacterLevel(ObjectGuid guid, byte level)
        {
            if (!_characterCacheStore.ContainsKey(guid))
                return;

            _characterCacheStore[guid].Level = level;
        }

        public void UpdateCharacterAccountId(ObjectGuid guid, uint accountId)
        {
            if (!_characterCacheStore.ContainsKey(guid))
                return;

            _characterCacheStore[guid].AccountId = accountId;
        }

        public void UpdateCharacterGuildId(ObjectGuid guid, ulong guildId)
        {
            if (!_characterCacheStore.ContainsKey(guid))
                return;

            _characterCacheStore[guid].GuildId = guildId;
        }

        public void UpdateCharacterArenaTeamId(ObjectGuid guid, byte slot, uint arenaTeamId)
        {
            if (!_characterCacheStore.ContainsKey(guid))
                return;

            _characterCacheStore[guid].ArenaTeamId[slot] = arenaTeamId;
        }

        public void UpdateCharacterInfoDeleted(ObjectGuid guid, bool deleted, string name = null)
        {
            if (!_characterCacheStore.ContainsKey(guid))
                return;

            _characterCacheStore[guid].IsDeleted = deleted;
            if (!name.IsEmpty())
                _characterCacheStore[guid].Name = name;
        }

        public bool HasCharacterCacheEntry(ObjectGuid guid)
        {
            return _characterCacheStore.ContainsKey(guid);
        }

        public CharacterCacheEntry GetCharacterCacheByGuid(ObjectGuid guid)
        {
            return _characterCacheStore.LookupByKey(guid);
        }

        public CharacterCacheEntry GetCharacterCacheByName(string name)
        {
            return _characterCacheByNameStore.LookupByKey(name);
        }

        public ObjectGuid GetCharacterGuidByName(string name)
        {
            var characterCacheEntry = _characterCacheByNameStore.LookupByKey(name);
            if (characterCacheEntry != null)
                return characterCacheEntry.Guid;

            return ObjectGuid.Empty;
        }

        public bool GetCharacterNameByGuid(ObjectGuid guid, out string name)
        {
            name = "Unknown";
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return false;

            name = characterCacheEntry.Name;
            return true;
        }

        public Team GetCharacterTeamByGuid(ObjectGuid guid)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return 0;

            return Player.TeamForRace(characterCacheEntry.RaceId);
        }

        public uint GetCharacterAccountIdByGuid(ObjectGuid guid)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return 0;

            return characterCacheEntry.AccountId;
        }

        public uint GetCharacterAccountIdByName(string name)
        {
            var characterCacheEntry = _characterCacheByNameStore.LookupByKey(name);
            if (characterCacheEntry != null)
                return characterCacheEntry.AccountId;

            return 0;
        }

        public byte GetCharacterLevelByGuid(ObjectGuid guid)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return 0;

            return characterCacheEntry.Level;
        }

        public ulong GetCharacterGuildIdByGuid(ObjectGuid guid)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return 0;

            return characterCacheEntry.GuildId;
        }

        public uint GetCharacterArenaTeamIdByGuid(ObjectGuid guid, byte type)
        {
            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return 0;

            return characterCacheEntry.ArenaTeamId[ArenaTeam.GetSlotByType(type)];
        }

        public bool GetCharacterNameAndClassByGUID(ObjectGuid guid, out string name, out byte _class)
        {
            name = "Unknown";
            _class = 0;

            var characterCacheEntry = _characterCacheStore.LookupByKey(guid);
            if (characterCacheEntry == null)
                return false;

            name = characterCacheEntry.Name;
            _class = (byte)characterCacheEntry.ClassId;
            return true;
        }

    }

    public class CharacterCacheEntry
    {
        public ObjectGuid Guid;
        public string Name;
        public uint AccountId;
        public Class ClassId;
        public Race RaceId;
        public Gender Sex;
        public byte Level;
        public ulong GuildId;
        public uint[] ArenaTeamId = new uint[SharedConst.MaxArenaSlot];
        public bool IsDeleted;
    }
}
