// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Game.Cache;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class QueryPlayerNames : ClientPacket
    {
        public ObjectGuid[] Players;

        public QueryPlayerNames(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Players = new ObjectGuid[_worldPacket.ReadInt32()];

            for (var i = 0; i < Players.Length; ++i)
                Players[i] = _worldPacket.ReadPackedGuid();
        }
    }

    public class QueryPlayerNamesResponse : ServerPacket
    {
        public List<NameCacheLookupResult> Players = new();

        public QueryPlayerNamesResponse() : base(ServerOpcodes.QueryPlayerNamesResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Players.Count);

            foreach (NameCacheLookupResult lookupResult in Players)
                lookupResult.Write(_worldPacket);
        }
    }

    public class QueryCreature : ClientPacket
    {
        public uint CreatureID;

        public QueryCreature(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            CreatureID = _worldPacket.ReadUInt32();
        }
    }

    public class QueryCreatureResponse : ServerPacket
    {
        public bool Allow;
        public uint CreatureID;
        public CreatureStats Stats;

        public QueryCreatureResponse() : base(ServerOpcodes.QueryCreatureResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CreatureID);
            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            if (Allow)
            {
                _worldPacket.WriteBits(Stats.Title.IsEmpty() ? 0 : Stats.Title.GetByteCount() + 1, 11);
                _worldPacket.WriteBits(Stats.TitleAlt.IsEmpty() ? 0 : Stats.TitleAlt.GetByteCount() + 1, 11);
                _worldPacket.WriteBits(Stats.CursorName.IsEmpty() ? 0 : Stats.CursorName.GetByteCount() + 1, 6);
                _worldPacket.WriteBit(Stats.Leader);

                for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
                {
                    _worldPacket.WriteBits(Stats.Name[i].GetByteCount() + 1, 11);
                    _worldPacket.WriteBits(Stats.NameAlt[i].GetByteCount() + 1, 11);
                }

                for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
                {
                    if (!string.IsNullOrEmpty(Stats.Name[i]))
                        _worldPacket.WriteCString(Stats.Name[i]);

                    if (!string.IsNullOrEmpty(Stats.NameAlt[i]))
                        _worldPacket.WriteCString(Stats.NameAlt[i]);
                }

                for (var i = 0; i < 2; ++i)
                    _worldPacket.WriteUInt32(Stats.Flags[i]);

                _worldPacket.WriteInt32(Stats.CreatureType);
                _worldPacket.WriteInt32(Stats.CreatureFamily);
                _worldPacket.WriteInt32(Stats.Classification);

                for (var i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                    _worldPacket.WriteUInt32(Stats.ProxyCreatureID[i]);

                _worldPacket.WriteInt32(Stats.Display.CreatureDisplay.Count);
                _worldPacket.WriteFloat(Stats.Display.TotalProbability);

                foreach (CreatureXDisplay display in Stats.Display.CreatureDisplay)
                {
                    _worldPacket.WriteUInt32(display.CreatureDisplayID);
                    _worldPacket.WriteFloat(display.Scale);
                    _worldPacket.WriteFloat(display.Probability);
                }

                _worldPacket.WriteFloat(Stats.HpMulti);
                _worldPacket.WriteFloat(Stats.EnergyMulti);

                _worldPacket.WriteInt32(Stats.QuestItems.Count);
                _worldPacket.WriteUInt32(Stats.CreatureMovementInfoID);
                _worldPacket.WriteInt32(Stats.HealthScalingExpansion);
                _worldPacket.WriteUInt32(Stats.RequiredExpansion);
                _worldPacket.WriteUInt32(Stats.VignetteID);
                _worldPacket.WriteInt32(Stats.Class);
                _worldPacket.WriteInt32(Stats.CreatureDifficultyID);
                _worldPacket.WriteInt32(Stats.WidgetSetID);
                _worldPacket.WriteInt32(Stats.WidgetSetUnitConditionID);

                if (!Stats.Title.IsEmpty())
                    _worldPacket.WriteCString(Stats.Title);

                if (!Stats.TitleAlt.IsEmpty())
                    _worldPacket.WriteCString(Stats.TitleAlt);

                if (!Stats.CursorName.IsEmpty())
                    _worldPacket.WriteCString(Stats.CursorName);

                foreach (var questItem in Stats.QuestItems)
                    _worldPacket.WriteUInt32(questItem);
            }
        }
    }

    public class QueryPageText : ClientPacket
    {
        public ObjectGuid ItemGUID;
        public uint PageTextID;

        public QueryPageText(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PageTextID = _worldPacket.ReadUInt32();
            ItemGUID = _worldPacket.ReadPackedGuid();
        }
    }

    public class QueryPageTextResponse : ServerPacket
    {
        public bool Allow;
        public List<PageTextInfo> Pages = new();

        public uint PageTextID;

        public QueryPageTextResponse() : base(ServerOpcodes.QueryPageTextResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(PageTextID);
            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            if (Allow)
            {
                _worldPacket.WriteInt32(Pages.Count);

                foreach (PageTextInfo pageText in Pages)
                    pageText.Write(_worldPacket);
            }
        }

        public struct PageTextInfo
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt32(Id);
                data.WriteUInt32(NextPageID);
                data.WriteInt32(PlayerConditionID);
                data.WriteUInt8(Flags);
                data.WriteBits(Text.GetByteCount(), 12);
                data.FlushBits();

                data.WriteString(Text);
            }

            public uint Id;
            public uint NextPageID;
            public int PlayerConditionID;
            public byte Flags;
            public string Text;
        }
    }

    public class QueryNPCText : ClientPacket
    {
        public ObjectGuid Guid;
        public uint TextID;

        public QueryNPCText(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TextID = _worldPacket.ReadUInt32();
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    public class QueryNPCTextResponse : ServerPacket
    {
        public bool Allow;
        public uint[] BroadcastTextID = new uint[SharedConst.MaxNpcTextOptions];
        public float[] Probabilities = new float[SharedConst.MaxNpcTextOptions];

        public uint TextID;

        public QueryNPCTextResponse() : base(ServerOpcodes.QueryNpcTextResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(TextID);
            _worldPacket.WriteBit(Allow);

            _worldPacket.WriteInt32(Allow ? SharedConst.MaxNpcTextOptions * (4 + 4) : 0);

            if (Allow)
            {
                for (uint i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
                    _worldPacket.WriteFloat(Probabilities[i]);

                for (uint i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
                    _worldPacket.WriteUInt32(BroadcastTextID[i]);
            }
        }
    }

    public class QueryGameObject : ClientPacket
    {
        public uint GameObjectID;
        public ObjectGuid Guid;

        public QueryGameObject(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            GameObjectID = _worldPacket.ReadUInt32();
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    public class QueryGameObjectResponse : ServerPacket
    {
        public bool Allow;

        public uint GameObjectID;
        public ObjectGuid Guid;
        public GameObjectStats Stats;

        public QueryGameObjectResponse() : base(ServerOpcodes.QueryGameObjectResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(GameObjectID);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            ByteBuffer statsData = new();

            if (Allow)
            {
                statsData.WriteUInt32(Stats.Type);
                statsData.WriteUInt32(Stats.DisplayID);

                for (int i = 0; i < 4; i++)
                    statsData.WriteCString(Stats.Name[i]);

                statsData.WriteCString(Stats.IconName);
                statsData.WriteCString(Stats.CastBarCaption);
                statsData.WriteCString(Stats.UnkString);

                for (uint i = 0; i < SharedConst.MaxGOData; i++)
                    statsData.WriteInt32(Stats.Data[i]);

                statsData.WriteFloat(Stats.Size);
                statsData.WriteUInt8((byte)Stats.QuestItems.Count);

                foreach (uint questItem in Stats.QuestItems)
                    statsData.WriteUInt32(questItem);

                statsData.WriteUInt32(Stats.ContentTuningId);
            }

            _worldPacket.WriteUInt32(statsData.GetSize());

            if (statsData.GetSize() != 0)
                _worldPacket.WriteBytes(statsData);
        }
    }

    public class QueryCorpseLocationFromClient : ClientPacket
    {
        public ObjectGuid Player;

        public QueryCorpseLocationFromClient(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
        }
    }

    public class CorpseLocation : ServerPacket
    {
        public int ActualMapID;
        public int MapID;

        public ObjectGuid Player;
        public Vector3 Position;
        public ObjectGuid Transport;
        public bool Valid;

        public CorpseLocation() : base(ServerOpcodes.CorpseLocation)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBit(Valid);
            _worldPacket.FlushBits();

            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteInt32(ActualMapID);
            _worldPacket.WriteVector3(Position);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WritePackedGuid(Transport);
        }
    }

    public class QueryCorpseTransport : ClientPacket
    {
        public ObjectGuid Player;
        public ObjectGuid Transport;

        public QueryCorpseTransport(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
            Transport = _worldPacket.ReadPackedGuid();
        }
    }

    public class CorpseTransportQuery : ServerPacket
    {
        public float Facing;

        public ObjectGuid Player;
        public Vector3 Position;

        public CorpseTransportQuery() : base(ServerOpcodes.CorpseTransportQuery)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteVector3(Position);
            _worldPacket.WriteFloat(Facing);
        }
    }

    public class QueryTime : ClientPacket
    {
        public QueryTime(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class QueryTimeResponse : ServerPacket
    {
        public long CurrentTime;

        public QueryTimeResponse() : base(ServerOpcodes.QueryTimeResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt64(CurrentTime);
        }
    }

    public class QuestPOIQuery : ClientPacket
    {
        public int MissingQuestCount;
        public uint[] MissingQuestPOIs = new uint[125];

        public QuestPOIQuery(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MissingQuestCount = _worldPacket.ReadInt32();

            for (byte i = 0; i < MissingQuestCount; ++i)
                MissingQuestPOIs[i] = _worldPacket.ReadUInt32();
        }
    }

    public class QuestPOIQueryResponse : ServerPacket
    {
        public List<QuestPOIData> QuestPOIDataStats = new();

        public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);

            bool useCache = WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries);

            foreach (QuestPOIData questPOIData in QuestPOIDataStats)
                if (useCache)
                    _worldPacket.WriteBytes(questPOIData.QueryDataBuffer);
                else
                    questPOIData.Write(_worldPacket);
        }
    }

    internal class QueryQuestCompletionNPCs : ClientPacket
    {
        public uint[] QuestCompletionNPCs;

        public QueryQuestCompletionNPCs(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint questCount = _worldPacket.ReadUInt32();
            QuestCompletionNPCs = new uint[questCount];

            for (uint i = 0; i < questCount; ++i)
                QuestCompletionNPCs[i] = _worldPacket.ReadUInt32();
        }
    }

    internal class QuestCompletionNPCResponse : ServerPacket
    {
        public List<QuestCompletionNPC> QuestCompletionNPCs = new();

        public QuestCompletionNPCResponse() : base(ServerOpcodes.QuestCompletionNpcResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(QuestCompletionNPCs.Count);

            foreach (var quest in QuestCompletionNPCs)
            {
                _worldPacket.WriteUInt32(quest.QuestID);

                _worldPacket.WriteInt32(quest.NPCs.Count);

                foreach (var npc in quest.NPCs)
                    _worldPacket.WriteUInt32(npc);
            }
        }
    }

    internal class QueryPetName : ClientPacket
    {
        public ObjectGuid UnitGUID;

        public QueryPetName(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class QueryPetNameResponse : ServerPacket
    {
        public bool Allow;
        public DeclinedName DeclinedNames = new();

        public bool HasDeclined;
        public string Name = "";
        public long Timestamp;

        public ObjectGuid UnitGUID;

        public QueryPetNameResponse() : base(ServerOpcodes.QueryPetNameResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteBit(Allow);

            if (Allow)
            {
                _worldPacket.WriteBits(Name.GetByteCount(), 8);
                _worldPacket.WriteBit(HasDeclined);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteString(DeclinedNames.Name[i]);

                _worldPacket.WriteInt64(Timestamp);
                _worldPacket.WriteString(Name);
            }

            _worldPacket.FlushBits();
        }
    }

    internal class ItemTextQuery : ClientPacket
    {
        public ObjectGuid Id;

        public ItemTextQuery(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Id = _worldPacket.ReadPackedGuid();
        }
    }

    internal class QueryItemTextResponse : ServerPacket
    {
        public ObjectGuid Id;
        public string Text;
        public bool Valid;

        public QueryItemTextResponse() : base(ServerOpcodes.QueryItemTextResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBit(Valid);
            _worldPacket.WriteBits(Text.GetByteCount(), 13);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Text);
            _worldPacket.WritePackedGuid(Id);
        }
    }

    internal class QueryRealmName : ClientPacket
    {
        public uint VirtualRealmAddress;

        public QueryRealmName(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            VirtualRealmAddress = _worldPacket.ReadUInt32();
        }
    }

    internal class RealmQueryResponse : ServerPacket
    {
        public byte LookupState;
        public VirtualRealmNameInfo NameInfo;

        public uint VirtualRealmAddress;

        public RealmQueryResponse() : base(ServerOpcodes.RealmQueryResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(VirtualRealmAddress);
            _worldPacket.WriteUInt8(LookupState);

            if (LookupState == 0)
                NameInfo.Write(_worldPacket);
        }
    }

    //Structs
    public class PlayerGuidLookupHint
    {
        public uint? NativeRealmAddress = new(); // original realm (?) (identifier made from the Index, BattleGroup and Region)

        public uint? VirtualRealmAddress = new(); // current realm (?) (identifier made from the Index, BattleGroup and Region)

        public void Write(WorldPacket data)
        {
            data.WriteBit(VirtualRealmAddress.HasValue);
            data.WriteBit(NativeRealmAddress.HasValue);
            data.FlushBits();

            if (VirtualRealmAddress.HasValue)
                data.WriteUInt32(VirtualRealmAddress.Value);

            if (NativeRealmAddress.HasValue)
                data.WriteUInt32(NativeRealmAddress.Value);
        }
    }

    public class PlayerGuidLookupData
    {
        public ObjectGuid AccountID;
        public ObjectGuid BnetAccountID;
        public Class ClassID = Class.None;
        public DeclinedName DeclinedNames = new();
        public ObjectGuid GuidActual;
        public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id

        public bool IsDeleted;
        public byte Level;
        public string Name = "";
        public Race RaceID = Race.None;
        public Gender Sex = Gender.None;
        public byte Unused915;
        public uint VirtualRealmAddress;

        public bool Initialize(ObjectGuid guid, Player player = null)
        {
            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);

            if (characterInfo == null)
                return false;

            if (player)
            {
                Cypher.Assert(player.GetGUID() == guid);

                AccountID = player.GetSession().GetAccountGUID();
                BnetAccountID = player.GetSession().GetBattlenetAccountGUID();
                Name = player.GetName();
                RaceID = player.GetRace();
                Sex = player.GetNativeGender();
                ClassID = player.GetClass();
                Level = (byte)player.GetLevel();

                DeclinedName names = player.GetDeclinedNames();

                if (names != null)
                    DeclinedNames = names;
            }
            else
            {
                uint accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);
                uint bnetAccountId = Global.BNetAccountMgr.GetIdByGameAccount(accountId);

                AccountID = ObjectGuid.Create(HighGuid.WowAccount, accountId);
                BnetAccountID = ObjectGuid.Create(HighGuid.BNetAccount, bnetAccountId);
                Name = characterInfo.Name;
                RaceID = characterInfo.RaceId;
                Sex = characterInfo.Sex;
                ClassID = characterInfo.ClassId;
                Level = characterInfo.Level;
            }

            IsDeleted = characterInfo.IsDeleted;
            GuidActual = guid;
            VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            return true;
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(IsDeleted);
            data.WriteBits(Name.GetByteCount(), 6);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

            data.FlushBits();

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteString(DeclinedNames.Name[i]);

            data.WritePackedGuid(AccountID);
            data.WritePackedGuid(BnetAccountID);
            data.WritePackedGuid(GuidActual);
            data.WriteUInt64(GuildClubMemberID);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8((byte)RaceID);
            data.WriteUInt8((byte)Sex);
            data.WriteUInt8((byte)ClassID);
            data.WriteUInt8(Level);
            data.WriteUInt8(Unused915);
            data.WriteString(Name);
        }
    }

    public class NameCacheUnused920
    {
        public uint Unused1;
        public ObjectGuid Unused2;
        public string Unused3 = "";

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Unused1);
            data.WritePackedGuid(Unused2);
            data.WriteBits(Unused3.GetByteCount(), 7);
            data.FlushBits();

            data.WriteString(Unused3);
        }
    }

    public struct NameCacheLookupResult
    {
        public ObjectGuid Player;
        public byte Result; // 0 - full packet, != 0 - only Guid
        public PlayerGuidLookupData Data;
        public NameCacheUnused920 Unused920;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Result);
            data.WritePackedGuid(Player);
            data.WriteBit(Data != null);
            data.WriteBit(Unused920 != null);
            data.FlushBits();

            Data?.Write(data);

            Unused920?.Write(data);
        }
    }

    public class CreatureXDisplay
    {
        public uint CreatureDisplayID;
        public float Probability = 1.0f;
        public float Scale = 1.0f;

        public CreatureXDisplay(uint creatureDisplayID, float displayScale, float probability)
        {
            CreatureDisplayID = creatureDisplayID;
            Scale = displayScale;
            Probability = probability;
        }
    }

    public class CreatureDisplayStats
    {
        public List<CreatureXDisplay> CreatureDisplay = new();
        public float TotalProbability;
    }

    public class CreatureStats
    {
        public int Class;
        public int Classification;
        public int CreatureDifficultyID;
        public int CreatureFamily;
        public uint CreatureMovementInfoID;
        public int CreatureType;
        public string CursorName;
        public CreatureDisplayStats Display = new();
        public float EnergyMulti;
        public uint[] Flags = new uint[2];
        public int HealthScalingExpansion;
        public float HpMulti;
        public bool Leader;
        public StringArray Name = new(SharedConst.MaxCreatureNames);
        public StringArray NameAlt = new(SharedConst.MaxCreatureNames);
        public uint[] ProxyCreatureID = new uint[SharedConst.MaxCreatureKillCredit];
        public List<uint> QuestItems = new();
        public uint RequiredExpansion;
        public string Title;
        public string TitleAlt;
        public uint VignetteID;
        public int WidgetSetID;
        public int WidgetSetUnitConditionID;
    }

    public struct DBQueryRecord
    {
        public uint RecordID;
    }

    public class GameObjectStats
    {
        public string CastBarCaption;
        public uint ContentTuningId;
        public int[] Data = new int[SharedConst.MaxGOData];
        public uint DisplayID;
        public string IconName;
        public string[] Name = new string[4];
        public List<uint> QuestItems = new();
        public float Size;
        public uint Type;
        public string UnkString;
    }

    internal class QuestCompletionNPC
    {
        public List<uint> NPCs = new();
        public uint QuestID;
    }
}