// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Dynamic;
using Framework.IO;
using Game.Cache;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    public class QueryPlayerNames : ClientPacket
    {
        public QueryPlayerNames(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Players = new ObjectGuid[_worldPacket.ReadInt32()];
            for (var i = 0; i < Players.Length; ++i)
                Players[i] = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid[] Players;
    }

    public class QueryPlayerNamesResponse : ServerPacket
    {
        public List<NameCacheLookupResult> Players = new();

        public QueryPlayerNamesResponse() : base(ServerOpcodes.QueryPlayerNamesResponse) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Players.Count);
            foreach (NameCacheLookupResult lookupResult in Players)
                lookupResult.Write(_worldPacket);
        }
    }

    public class QueryCreature : ClientPacket
    {
        public QueryCreature(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CreatureID = _worldPacket.ReadUInt32();
        }

        public uint CreatureID;
    }

    public class QueryCreatureResponse : ServerPacket
    {
        public QueryCreatureResponse() : base(ServerOpcodes.QueryCreatureResponse, ConnectionType.Instance) { }

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

                _worldPacket.WriteInt8(Stats.CreatureType);
                _worldPacket.WriteInt32(Stats.CreatureFamily);
                _worldPacket.WriteInt8(Stats.Classification);

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
                _worldPacket.WriteInt32(Stats.QuestCurrencies.Count);
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

                foreach (var currencyItem in Stats.QuestCurrencies)
                    _worldPacket.WriteInt32(currencyItem);
            }
        }

        public bool Allow;
        public CreatureStats Stats;
        public uint CreatureID;
    }

    public class QueryPageText : ClientPacket
    {
        public QueryPageText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PageTextID = _worldPacket.ReadUInt32();
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
        public uint PageTextID;
    }

    public class QueryPageTextResponse : ServerPacket
    {
        public QueryPageTextResponse() : base(ServerOpcodes.QueryPageTextResponse) { }

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

        public uint PageTextID;
        public bool Allow;
        public List<PageTextInfo> Pages = new();

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
        public QueryNPCText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TextID = _worldPacket.ReadUInt32();
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
        public uint TextID;
    }

    public class QueryNPCTextResponse : ServerPacket
    {
        public QueryNPCTextResponse() : base(ServerOpcodes.QueryNpcTextResponse, ConnectionType.Instance) { }

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

        public uint TextID;
        public bool Allow;
        public float[] Probabilities = new float[SharedConst.MaxNpcTextOptions];
        public uint[] BroadcastTextID = new uint[SharedConst.MaxNpcTextOptions];
    }

    public class QueryGameObject : ClientPacket
    {
        public QueryGameObject(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GameObjectID = _worldPacket.ReadUInt32();
            Guid = _worldPacket.ReadPackedGuid();
        }

        public uint GameObjectID;
        public ObjectGuid Guid;
    }

    public class QueryGameObjectResponse : ServerPacket
    {
        public QueryGameObjectResponse() : base(ServerOpcodes.QueryGameObjectResponse, ConnectionType.Instance) { }

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

        public uint GameObjectID;
        public ObjectGuid Guid;
        public bool Allow;
        public GameObjectStats Stats;
    }

    public class QueryCorpseLocationFromClient : ClientPacket
    {
        public QueryCorpseLocationFromClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player;
    }

    public class CorpseLocation : ServerPacket
    {
        public CorpseLocation() : base(ServerOpcodes.CorpseLocation) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Valid);
            _worldPacket.FlushBits();

            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteInt32(ActualMapID);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WritePackedGuid(Transport);
            _worldPacket.WriteVector3(Position);
        }

        public ObjectGuid Player;
        public ObjectGuid Transport;
        public Vector3 Position;
        public int ActualMapID;
        public int MapID;
        public bool Valid;
    }

    public class QueryCorpseTransport : ClientPacket
    {
        public QueryCorpseTransport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
            Transport = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player;
        public ObjectGuid Transport;
    }

    public class CorpseTransportQuery : ServerPacket
    {
        public CorpseTransportQuery() : base(ServerOpcodes.CorpseTransportQuery) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteVector3(Position);
            _worldPacket.WriteFloat(Facing);
        }

        public ObjectGuid Player;
        public Vector3 Position;
        public float Facing;
    }

    public class QueryTime : ClientPacket
    {
        public QueryTime(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class QueryTimeResponse : ServerPacket
    {
        public QueryTimeResponse() : base(ServerOpcodes.QueryTimeResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt64(CurrentTime);
        }

        public long CurrentTime;
    }

    public class QuestPOIQuery : ClientPacket
    {
        public QuestPOIQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MissingQuestCount = _worldPacket.ReadInt32();

            for (byte i = 0; i < MissingQuestCount; ++i)
                MissingQuestPOIs[i] = _worldPacket.ReadUInt32();
        }

        public int MissingQuestCount;
        public uint[] MissingQuestPOIs = new uint[175];
    }

    public class QuestPOIQueryResponse : ServerPacket
    {
        public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);

            bool useCache = WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries);

            foreach (QuestPOIData questPOIData in QuestPOIDataStats)
            {
                if (useCache)
                    _worldPacket.WriteBytes(questPOIData.QueryDataBuffer);
                else
                    questPOIData.Write(_worldPacket);
            }
        }

        public List<QuestPOIData> QuestPOIDataStats = new();
    }

    class QueryQuestCompletionNPCs : ClientPacket
    {
        public QueryQuestCompletionNPCs(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint questCount = _worldPacket.ReadUInt32();
            QuestCompletionNPCs = new uint[questCount];

            for (uint i = 0; i < questCount; ++i)
                QuestCompletionNPCs[i] = _worldPacket.ReadUInt32();
        }

        public uint[] QuestCompletionNPCs;
    }

    class QuestCompletionNPCResponse : ServerPacket
    {
        public QuestCompletionNPCResponse() : base(ServerOpcodes.QuestCompletionNpcResponse, ConnectionType.Instance) { }

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

        public List<QuestCompletionNPC> QuestCompletionNPCs = new();
    }

    class QueryPetName : ClientPacket
    {
        public QueryPetName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid UnitGUID;
    }

    class QueryPetNameResponse : ServerPacket
    {
        public QueryPetNameResponse() : base(ServerOpcodes.QueryPetNameResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteBit(Allow);

            if (Allow)
            {
                _worldPacket.WriteBits(Name.GetByteCount(), 8);
                _worldPacket.WriteBit(HasDeclined);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteBits(DeclinedNames.name[i].GetByteCount(), 7);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteString(DeclinedNames.name[i]);

                _worldPacket.WriteInt64(Timestamp);
                _worldPacket.WriteString(Name);
            }

            _worldPacket.FlushBits();
        }

        public ObjectGuid UnitGUID;
        public bool Allow;

        public bool HasDeclined;
        public DeclinedName DeclinedNames = new();
        public long Timestamp;
        public string Name = "";
    }

    class ItemTextQuery : ClientPacket
    {
        public ItemTextQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Id = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Id;
    }

    class QueryItemTextResponse : ServerPacket
    {
        public QueryItemTextResponse() : base(ServerOpcodes.QueryItemTextResponse) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Valid);
            _worldPacket.WriteBits(Text.GetByteCount(), 13);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Text);
            _worldPacket.WritePackedGuid(Id);
        }

        public ObjectGuid Id;
        public bool Valid;
        public string Text;
    }

    class QueryRealmName : ClientPacket
    {
        public QueryRealmName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            VirtualRealmAddress = _worldPacket.ReadUInt32();
        }

        public uint VirtualRealmAddress;
    }

    class RealmQueryResponse : ServerPacket
    {
        public RealmQueryResponse() : base(ServerOpcodes.RealmQueryResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(VirtualRealmAddress);
            _worldPacket.WriteUInt8(LookupState);
            if (LookupState == 0)
                NameInfo.Write(_worldPacket);
        }

        public uint VirtualRealmAddress;
        public byte LookupState;
        public VirtualRealmNameInfo NameInfo;
    }

    class QueryTreasurePicker : ClientPacket
    {
        public uint QuestID;
        public uint TreasurePickerID;

        public QueryTreasurePicker(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
            TreasurePickerID = _worldPacket.ReadUInt32();
        }
    }

    class TreasurePickerResponse : ServerPacket
    {
        public uint QuestID;
        public uint TreasurePickerID;
        public TreasurePickerPick Treasure;

        public TreasurePickerResponse() : base(ServerOpcodes.TreasurePickerResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteUInt32(TreasurePickerID);
            Treasure.Write(_worldPacket);
        }
    }

    //Structs
    public class PlayerGuidLookupData
    {
        public bool Initialize(ObjectGuid guid, Player player = null)
        {
            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);
            if (characterInfo == null)
                return false;

            if (player != null)
            {
                Cypher.Assert(player.GetGUID() == guid);

                AccountID = player.GetSession().GetAccountGUID();
                BnetAccountID = player.GetSession().GetBattlenetAccountGUID();
                Name = player.GetName();
                RaceID = player.GetRace();
                Sex = player.GetNativeGender();
                ClassID = player.GetClass();
                Level = (byte)player.GetLevel();
                PvpFaction = (byte)(player.GetTeamId() == BattleGroundTeamId.Alliance ? 1 : 0);
                TimerunningSeasonID = player.m_activePlayerData.TimerunningSeasonID;

                DeclinedNames names = player.GetDeclinedNames();
                if (names != null)
                    DeclinedNames = new(names);
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
                PvpFaction = (byte)(Player.TeamIdForRace(characterInfo.RaceId) == BattleGroundTeamId.Alliance ? 1 : 0);
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
                data.WriteBits(DeclinedNames.name[i].GetByteCount(), 7);

            data.FlushBits();
            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteString(DeclinedNames.name[i]);

            data.WritePackedGuid(AccountID);
            data.WritePackedGuid(BnetAccountID);
            data.WritePackedGuid(GuidActual);
            data.WriteUInt64(GuildClubMemberID);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8((byte)RaceID);
            data.WriteUInt8((byte)Sex);
            data.WriteUInt8((byte)ClassID);
            data.WriteUInt8(Level);
            data.WriteUInt8(PvpFaction);
            data.WriteInt32(TimerunningSeasonID);
            data.WriteString(Name);
        }

        public bool IsDeleted;
        public ObjectGuid AccountID;
        public ObjectGuid BnetAccountID;
        public ObjectGuid GuidActual;
        public string Name = "";
        public ulong GuildClubMemberID;   // same as bgs.protocol.club.v1.MemberId.unique_id
        public uint VirtualRealmAddress;
        public Race RaceID = Race.None;
        public Gender Sex = Gender.None;
        public Class ClassID = Class.None;
        public byte Level;
        public byte PvpFaction;
        public int TimerunningSeasonID;
        public DeclinedName DeclinedNames = new();
    }

    public class GuildGuidLookupData
    {
        public uint VirtualRealmAddress;
        public ObjectGuid Guid;
        public string Name = "";

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(VirtualRealmAddress);
            data.WritePackedGuid(Guid);
            data.WriteBits(Name.GetByteCount(), 7);
            data.FlushBits();

            data.WriteString(Name);
        }
    }

    public struct NameCacheLookupResult
    {
        public ObjectGuid Player;
        public byte Result; // 0 - full packet, != 0 - only guid
        public PlayerGuidLookupData Data;
        public GuildGuidLookupData GuildData;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Result);
            data.WritePackedGuid(Player);
            data.WriteBit(Data != null);
            data.WriteBit(GuildData != null);
            data.FlushBits();

            if (Data != null)
                Data.Write(data);

            if (GuildData != null)
                GuildData.Write(data);
        }
    }

    public class CreatureXDisplay
    {
        public CreatureXDisplay(uint creatureDisplayID, float displayScale, float probability)
        {
            CreatureDisplayID = creatureDisplayID;
            Scale = displayScale;
            Probability = probability;
        }

        public uint CreatureDisplayID;
        public float Scale = 1.0f;
        public float Probability = 1.0f;
    }

    public class CreatureDisplayStats
    {
        public float TotalProbability;
        public List<CreatureXDisplay> CreatureDisplay = new();
    }

    public class CreatureStats
    {
        public string Title;
        public string TitleAlt;
        public string CursorName;
        public sbyte CreatureType;
        public int CreatureFamily;
        public sbyte Classification;
        public CreatureDisplayStats Display = new();
        public float HpMulti;
        public float EnergyMulti;
        public bool Leader;
        public List<uint> QuestItems = new();
        public List<int> QuestCurrencies = new();
        public uint CreatureMovementInfoID;
        public int HealthScalingExpansion;
        public uint RequiredExpansion;
        public uint VignetteID;
        public int Class;
        public int CreatureDifficultyID;
        public int WidgetSetID;
        public int WidgetSetUnitConditionID;
        public uint[] Flags = new uint[2];
        public uint[] ProxyCreatureID = new uint[SharedConst.MaxCreatureKillCredit];
        public StringArray Name = new(SharedConst.MaxCreatureNames);
        public StringArray NameAlt = new(SharedConst.MaxCreatureNames);
    }

    public struct DBQueryRecord
    {
        public uint RecordID;
    }

    public class GameObjectStats
    {
        public string[] Name = new string[4];
        public string IconName;
        public string CastBarCaption;
        public string UnkString;
        public uint Type;
        public uint DisplayID;
        public int[] Data = new int[SharedConst.MaxGOData];
        public float Size;
        public List<uint> QuestItems = new();
        public uint ContentTuningId;
    }

    class QuestCompletionNPC
    {
        public uint QuestID;
        public List<uint> NPCs = new();
    }

    struct TreasurePickItem
    {
        public ItemInstance Item;
        public uint Quantity;
        public QuestRewardContextFlags? ContextFlags;

        public void Write(WorldPacket data)
        {
            Item.Write(data);
            data.WriteUInt32(Quantity);
            data.WriteBit(ContextFlags.HasValue);
            data.FlushBits();

            if (ContextFlags.HasValue)
                data.WriteInt32((int)ContextFlags.Value);
        }
    }

    struct TreasurePickCurrency
    {
        public uint CurrencyID;
        public uint Quantity;
        public QuestRewardContextFlags? ContextFlags;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(CurrencyID);
            data.WriteUInt32(Quantity);
            data.WriteBit(ContextFlags.HasValue);
            data.FlushBits();

            if (ContextFlags.HasValue)
                data.WriteInt32((int)ContextFlags.Value);
        }
    }

    enum TreasurePickerBonusContext
    {
        None = 0,
        WarMode = 1
    }

    class TreasurePickerBonus
    {
        public List<TreasurePickItem> ItemPicks = new();
        public List<TreasurePickCurrency> CurrencyPicks = new();
        public ulong Gold;
        public TreasurePickerBonusContext Context;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ItemPicks.Count);
            data.WriteInt32(CurrencyPicks.Count);
            data.WriteUInt64(Gold);
            data.WriteBits(Context, 1);
            data.FlushBits();

            foreach (TreasurePickItem treasurePickerItem in ItemPicks)
                treasurePickerItem.Write(data);

            foreach (TreasurePickCurrency treasurePickCurrency in CurrencyPicks)
                treasurePickCurrency.Write(data);
        }
    }

    class TreasurePickerPick
    {
        public List<TreasurePickItem> ItemPicks = new();
        public List<TreasurePickCurrency> CurrencyPicks = new();
        public List<TreasurePickerBonus> Bonuses = new();
        public ulong Gold;
        public int Flags;
        public bool IsChoice;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ItemPicks.Count);
            data.WriteInt32(CurrencyPicks.Count);
            data.WriteUInt64(Gold);
            data.WriteInt32(Bonuses.Count);
            data.WriteInt32(Flags);
            data.WriteBit(IsChoice);
            data.FlushBits();

            foreach (TreasurePickItem treasurePickItem in ItemPicks)
                treasurePickItem.Write(data);

            foreach (TreasurePickCurrency treasurePickCurrency in CurrencyPicks)
                treasurePickCurrency.Write(data);

            foreach (TreasurePickerBonus treasurePickerBonus in Bonuses)
                treasurePickerBonus.Write(data);
        }
    }
}
