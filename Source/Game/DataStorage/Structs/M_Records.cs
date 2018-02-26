/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using Framework.GameMath;
using System;

namespace Game.DataStorage
{
    public sealed class MailTemplateRecord
    {
        public uint Id;
        public LocalizedString Body;
    }

    public sealed class MapRecord
    {
        public uint Id;
        public uint Directory;
        public LocalizedString MapName;
        public string MapDescription0;                               // Horde
        public string MapDescription1;                               // Alliance
        public string ShortDescription;
        public string LongDescription;
        public MapFlags[] Flags = new MapFlags[2];
        public float MinimapIconScale;
        public Vector2 CorpsePos;                                        // entrance coordinates in ghost mode  (in most cases = normal entrance)
        public ushort AreaTableID;
        public ushort LoadingScreenID;
        public short CorpseMapID;                                              // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
        public ushort TimeOfDayOverride;
        public short ParentMapID;
        public short CosmeticParentMapID;
        public ushort WindSettingsID;
        public MapTypes InstanceType;
        public byte unk5;
        public byte ExpansionID;
        public byte MaxPlayers;
        public byte TimeOffset;

        //Helpers
        public Expansion Expansion() { return (Expansion)ExpansionID; }

        public bool IsDungeon() { return (InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Scenario) && !IsGarrison(); }
        public bool IsNonRaidDungeon() { return InstanceType == MapTypes.Instance; }
        public bool Instanceable()
        {
            return InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid
                || InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena || InstanceType == MapTypes.Scenario;
        }
        public bool IsRaid() { return InstanceType == MapTypes.Raid; }
        public bool IsBattleground() { return InstanceType == MapTypes.Battleground; }
        public bool IsBattleArena() { return InstanceType == MapTypes.Arena; }
        public bool IsBattlegroundOrArena() { return InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena; }
        public bool IsWorldMap() { return InstanceType == MapTypes.Common; }

        public bool GetEntrancePos(out uint mapid, out float x, out float y)
        {
            mapid = 0;
            x = 0;
            y = 0;

            if (CorpseMapID < 0)
                return false;
            mapid = (uint)CorpseMapID;
            x = CorpsePos.X;
            y = CorpsePos.Y;
            return true;
        }

        public bool IsContinent()
        {
            return Id == 0 || Id == 1 || Id == 530 || Id == 571 || Id == 870 || Id == 1116 || Id == 1220;
        }

        public bool IsDynamicDifficultyMap() { return Flags[0].HasAnyFlag(MapFlags.CanToggleDifficulty); }
        public bool IsGarrison() { return Flags[0].HasAnyFlag(MapFlags.Garrison); }
    }

    public sealed class MapDifficultyRecord
    {
        public uint Id;
        public LocalizedString Message_lang;                          // m_message_lang (text showed when transfer to map failed)
        public byte DifficultyID;
        public byte RaidDurationType;                                 // 1 means daily reset, 2 means weekly
        public byte MaxPlayers;                                       // m_maxPlayers some heroic versions have 0 when expected same amount as in normal version
        public byte LockID;
        public byte Flags;
        public byte ItemBonusTreeModID;
        public uint Context;
        public uint MapID;

        public uint GetRaidDuration()
        {
            if (RaidDurationType == 1)
                return 86400;
            if (RaidDurationType == 2)
                return 604800;
            return 0;
        }
    }

    public sealed class ModifierTreeRecord
    {
        public uint Id;
        public uint[] Asset = new uint[2];
        public uint Parent;
        public byte Type;
        public byte Unk700;
        public byte Operator;
        public byte Amount;
    }

    public sealed class MountRecord
    {
        public string Name;
        public string Description;
        public string SourceDescription;
        public uint SpellId;
        public float CameraPivotMultiplier;
        public ushort MountTypeId;
        public ushort Flags;
        public byte Source;
        public uint Id;
        public uint PlayerConditionId;
        public byte UiModelSceneID;
    }

    public sealed class MountCapabilityRecord
    {
        public uint RequiredSpell;
        public uint SpeedModSpell;
        public ushort RequiredRidingSkill;
        public ushort RequiredArea;
        public short RequiredMap;
        public MountCapabilityFlags Flags;
        public uint Id;
        public byte RequiredAura;
    }

    public sealed class MountTypeXCapabilityRecord
    {
        public uint Id;
        public ushort MountTypeID;
        public ushort MountCapabilityID;
        public byte OrderIndex;
    }

    public sealed class MountXDisplayRecord
    {
        public uint Id;
        public uint DisplayID;
        public uint PlayerConditionID;
        public uint MountID;
    }

    public sealed class MovieRecord
    {
        public uint Id;
        public uint AudioFileDataID;
        public uint SubtitleFileDataID;
        public byte Volume;
        public byte KeyID;
    }
}
