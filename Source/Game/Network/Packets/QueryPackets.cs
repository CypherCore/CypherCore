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

using Framework.Collections;
using Framework.Constants;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class QueryPlayerName : ClientPacket
    {
        public QueryPlayerName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player;
    }

    public class QueryPlayerNameResponse : ServerPacket
    {
        public QueryPlayerNameResponse() : base(ServerOpcodes.QueryPlayerNameResponse)
        {
            Data = new PlayerGuidLookupData();
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(Result);
            _worldPacket.WritePackedGuid(Player);

            if (Result == ResponseCodes.Success)
                Data.Write(_worldPacket);
        }

        public ObjectGuid Player;
        public ResponseCodes Result; // 0 - full packet, != 0 - only guid
        public PlayerGuidLookupData Data;
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

                _worldPacket.WriteInt32(Stats.CreatureType);
                _worldPacket.WriteInt32(Stats.CreatureFamily);
                _worldPacket.WriteInt32(Stats.Classification);

                for (var i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                    _worldPacket.WriteUInt32(Stats.ProxyCreatureID[i]);

                _worldPacket.WriteUInt32(Stats.Display.CreatureDisplay.Count);
                _worldPacket.WriteFloat(Stats.Display.TotalProbability);

                foreach (CreatureXDisplay display in Stats.Display.CreatureDisplay)
                {
                    _worldPacket.WriteUInt32(display.CreatureDisplayID);
                    _worldPacket.WriteFloat(display.Scale);
                    _worldPacket.WriteFloat(display.Probability);
                }

                _worldPacket.WriteFloat(Stats.HpMulti);
                _worldPacket.WriteFloat(Stats.EnergyMulti);

                _worldPacket.WriteUInt32(Stats.QuestItems.Count);
                _worldPacket.WriteUInt32(Stats.CreatureMovementInfoID);
                _worldPacket.WriteInt32(Stats.HealthScalingExpansion);
                _worldPacket.WriteUInt32(Stats.RequiredExpansion);
                _worldPacket.WriteInt32(Stats.VignetteID);
                _worldPacket.WriteInt32(Stats.Class);

                if (!Stats.Title.IsEmpty())
                    _worldPacket.WriteCString(Stats.Title);

                if (!Stats.TitleAlt.IsEmpty())
                    _worldPacket.WriteCString(Stats.TitleAlt);

                if (!Stats.CursorName.IsEmpty())
                    _worldPacket.WriteCString(Stats.CursorName);

                foreach (var questItem in Stats.QuestItems)
                    _worldPacket.WriteInt32(questItem);
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
                _worldPacket.WriteUInt32(Pages.Count);
                foreach (PageTextInfo pageText in Pages)
                    pageText.Write(_worldPacket);
            }
        }

        public uint PageTextID;
        public bool Allow;
        public List<PageTextInfo> Pages = new List<PageTextInfo>();

        public struct PageTextInfo
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt32(ID);
                data.WriteUInt32(NextPageID);
                data.WriteInt32(PlayerConditionID);
                data.WriteUInt8(Flags);
                data.WriteBits(Text.GetByteCount(), 12);
                data.FlushBits();

                data.WriteString(Text);
            }

            public uint ID;
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
            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            ByteBuffer statsData = new ByteBuffer();
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
                statsData.WriteUInt8(Stats.QuestItems.Count);
                foreach (uint questItem in Stats.QuestItems)
                    statsData.WriteUInt32(questItem);

                statsData.WriteUInt32(Stats.RequiredLevel);

            }

            _worldPacket.WriteUInt32(statsData.GetSize());
            if (statsData.GetSize() != 0)
                _worldPacket.WriteBytes(statsData);
        }

        public uint GameObjectID;
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
            _worldPacket.WriteVector3(Position);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WritePackedGuid(Transport);
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
            _worldPacket.WriteInt32(CurrentTime);
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
        public uint[] MissingQuestPOIs = new uint[100];
    }

    public class QuestPOIQueryResponse : ServerPacket
    {
        public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);
            _worldPacket.WriteInt32(QuestPOIDataStats.Count);

            foreach (QuestPOIData questPOIData in QuestPOIDataStats)
            {
                _worldPacket.WriteInt32(questPOIData.QuestID);

                _worldPacket.WriteInt32(questPOIData.QuestPOIBlobDataStats.Count);

                foreach (QuestPOIBlobData questPOIBlobData in questPOIData.QuestPOIBlobDataStats)
                {
                    _worldPacket.WriteInt32(questPOIBlobData.BlobIndex);
                    _worldPacket.WriteInt32(questPOIBlobData.ObjectiveIndex);
                    _worldPacket.WriteInt32(questPOIBlobData.QuestObjectiveID);
                    _worldPacket.WriteInt32(questPOIBlobData.QuestObjectID);
                    _worldPacket.WriteInt32(questPOIBlobData.MapID);
                    _worldPacket.WriteInt32(questPOIBlobData.UiMapID);
                    _worldPacket.WriteInt32(questPOIBlobData.Priority);
                    _worldPacket.WriteInt32(questPOIBlobData.Flags);
                    _worldPacket.WriteInt32(questPOIBlobData.WorldEffectID);
                    _worldPacket.WriteInt32(questPOIBlobData.PlayerConditionID);
                    _worldPacket.WriteInt32(questPOIBlobData.SpawnTrackingID);
                    _worldPacket.WriteInt32(questPOIBlobData.QuestPOIBlobPointStats.Count);

                    foreach (QuestPOIBlobPoint questPOIBlobPoint in questPOIBlobData.QuestPOIBlobPointStats)
                    {
                        _worldPacket.WriteInt32(questPOIBlobPoint.X);
                        _worldPacket.WriteInt32(questPOIBlobPoint.Y);
                    }

                    _worldPacket.WriteBit(questPOIBlobData.AlwaysAllowMergingBlobs);
                    _worldPacket.FlushBits();
                }
            }
        }

        public List<QuestPOIData> QuestPOIDataStats = new List<QuestPOIData>();
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
            _worldPacket.WriteUInt32(QuestCompletionNPCs.Count);
            foreach (var quest in QuestCompletionNPCs)
            {
                _worldPacket.WriteUInt32(quest.QuestID);

                _worldPacket.WriteUInt32(quest.NPCs.Count);
                foreach (var npc in quest.NPCs)
                    _worldPacket.WriteUInt32(npc);
            }
        }

        public List<QuestCompletionNPC> QuestCompletionNPCs = new List<QuestCompletionNPC>();
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

                _worldPacket.WriteUInt32(Timestamp);
                _worldPacket.WriteString(Name);
            }

            _worldPacket.FlushBits();
        }

        public ObjectGuid UnitGUID;
        public bool Allow;

        public bool HasDeclined;
        public DeclinedName DeclinedNames = new DeclinedName();
        public uint Timestamp;
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

    //Structs
    public class PlayerGuidLookupHint
    {
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

        public Optional<uint> VirtualRealmAddress = new Optional<uint>(); // current realm (?) (identifier made from the Index, BattleGroup and Region)
        public Optional<uint> NativeRealmAddress = new Optional<uint>(); // original realm (?) (identifier made from the Index, BattleGroup and Region)
    }

    public class PlayerGuidLookupData
    {
        public bool Initialize(ObjectGuid guid, Player player = null)
        {
            CharacterInfo characterInfo = Global.WorldMgr.GetCharacterInfo(guid);
            if (characterInfo == null)
                return false;

            if (player)
            {
                Cypher.Assert(player.GetGUID() == guid);

                AccountID = player.GetSession().GetAccountGUID();
                BnetAccountID = player.GetSession().GetBattlenetAccountGUID();
                Name = player.GetName();
                RaceID = player.GetRace();
                Sex = (Gender)player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender);
                ClassID = player.GetClass();
                Level = (byte)player.getLevel();

                DeclinedName names = player.GetDeclinedNames();
                if (names != null)
                    DeclinedNames = names;
            }
            else
            {
                uint accountId = ObjectManager.GetPlayerAccountIdByGUID(guid);
                uint bnetAccountId = Global.BNetAccountMgr.GetIdByGameAccount(accountId);

                AccountID = ObjectGuid.Create(HighGuid.WowAccount, accountId);
                BnetAccountID = ObjectGuid.Create(HighGuid.BNetAccount, bnetAccountId);
                Name = characterInfo.Name;
                RaceID = characterInfo.RaceID;
                Sex = characterInfo.Sex;
                ClassID = characterInfo.ClassID;
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
                data.WriteBits(DeclinedNames.name[i].GetByteCount(), 7);

            data.FlushBits();
            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteString(DeclinedNames.name[i]);

            data.WritePackedGuid(AccountID);
            data.WritePackedGuid(BnetAccountID);
            data.WritePackedGuid(GuidActual);
            data.WriteUInt64(GuildClubMemberID);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8(RaceID);
            data.WriteUInt8(Sex);
            data.WriteUInt8(ClassID);
            data.WriteUInt8(Level);
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
        public DeclinedName DeclinedNames = new DeclinedName();
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
        public List<CreatureXDisplay> CreatureDisplay = new List<CreatureXDisplay>();
    }

    public class CreatureStats
    {
        public string Title;
        public string TitleAlt;
        public string CursorName;
        public int CreatureType;
        public int CreatureFamily;
        public int Classification;
        public CreatureDisplayStats Display = new CreatureDisplayStats();
        public float HpMulti;
        public float EnergyMulti;
        public bool Leader;
        public List<uint> QuestItems = new List<uint>();
        public uint CreatureMovementInfoID;
        public int HealthScalingExpansion;
        public uint RequiredExpansion;
        public uint VignetteID;
        public int Class;
        public uint[] Flags = new uint[2];
        public uint[] ProxyCreatureID = new uint[SharedConst.MaxCreatureKillCredit];
        public StringArray Name = new StringArray(SharedConst.MaxCreatureNames);
        public StringArray NameAlt = new StringArray(SharedConst.MaxCreatureNames);
    }

    public struct DBQueryRecord
    {
        public ObjectGuid GUID;
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
        public List<uint> QuestItems = new List<uint>();
        public uint RequiredLevel;
    }

    public struct QuestPOIBlobPoint
    {
        public QuestPOIBlobPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X;
        public int Y;
    }

    public class QuestPOIBlobData
    {
        public int BlobIndex;
        public int ObjectiveIndex;
        public int QuestObjectiveID;
        public int QuestObjectID;
        public int MapID;
        public int UiMapID;
        public int Priority;
        public int Flags;
        public int WorldEffectID;
        public int PlayerConditionID;
        public int SpawnTrackingID;
        public List<QuestPOIBlobPoint> QuestPOIBlobPointStats = new List<QuestPOIBlobPoint>();
        public bool AlwaysAllowMergingBlobs;
    }

    public class QuestPOIData
    {
        public uint QuestID;
        public List<QuestPOIBlobData> QuestPOIBlobDataStats = new List<QuestPOIBlobData>();
    }

    class QuestCompletionNPC
    {
        public uint QuestID;
        public List<uint> NPCs = new List<uint>();
    }
}
