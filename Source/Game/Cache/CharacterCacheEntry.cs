using Framework.Constants;
using Game.Entities;

namespace Game.Cache
{
    public class CharacterCacheEntry
    {
        public ObjectGuid Guid;
        public uint AccountId { get; set; }
        public uint[] ArenaTeamId { get; set; } = new uint[SharedConst.MaxArenaSlot];
        public Class ClassId { get; set; }
        public ulong GuildId { get; set; }
        public bool IsDeleted { get; set; }
        public byte Level { get; set; }
        public string Name { get; set; }
        public Race RaceId { get; set; }
        public Gender Sex { get; set; }
    }
}