/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Game.Network.Packets
{
    public class QueryPlayerName : ClientPacket
    {
        public QueryPlayerName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player { get; set; }
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

        public ObjectGuid Player { get; set; }
        public ResponseCodes Result; // 0 - full packet, != 0 - only guid
        public PlayerGuidLookupData Data { get; set; }
    }

    public class QueryCreature : ClientPacket
    {
        public QueryCreature(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CreatureID = _worldPacket.ReadUInt32();
        }

        public uint CreatureID { get; set; }
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
                _worldPacket.WriteBits(Stats.Title.Length + 1, 11);
                _worldPacket.WriteBits(Stats.TitleAlt.Length + 1, 11);
                _worldPacket.WriteBits(Stats.CursorName.Length + 1, 6);
                _worldPacket.WriteBit(Stats.Leader);

                for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
                {
                    _worldPacket.WriteBits(Stats.Name[i].Length + 1, 11);
                    _worldPacket.WriteBits(Stats.NameAlt[i].Length + 1, 11);
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

                for (var i = 0; i < SharedConst.MaxCreatureModelIds; ++i)
                    _worldPacket.WriteUInt32(Stats.CreatureDisplayID[i]);

                _worldPacket.WriteFloat(Stats.HpMulti);
                _worldPacket.WriteFloat(Stats.EnergyMulti);

                _worldPacket.WriteUInt32(Stats.QuestItems.Count);
                _worldPacket.WriteUInt32(Stats.CreatureMovementInfoID);
                _worldPacket.WriteInt32(Stats.HealthScalingExpansion);
                _worldPacket.WriteUInt32(Stats.RequiredExpansion);
                _worldPacket.WriteInt32(Stats.VignetteID);

                if (!string.IsNullOrEmpty(Stats.Title))
                    _worldPacket.WriteCString(Stats.Title);

                if (!string.IsNullOrEmpty(Stats.TitleAlt))
                    _worldPacket.WriteCString(Stats.TitleAlt);

                if (!string.IsNullOrEmpty(Stats.CursorName))
                    _worldPacket.WriteCString(Stats.CursorName);

                foreach (var questItem in Stats.QuestItems)
                    _worldPacket.WriteInt32(questItem);
            }
        }

        public bool Allow { get; set; }
        public CreatureStats Stats { get; set; }
        public uint CreatureID { get; set; }
    }

    public class QueryPageText : ClientPacket
    {
        public QueryPageText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PageTextID = _worldPacket.ReadUInt32();
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID { get; set; }
        public uint PageTextID { get; set; }
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

        public uint PageTextID { get; set; }
        public bool Allow { get; set; }
        public List<PageTextInfo> Pages { get; set; } = new List<PageTextInfo>();

        public struct PageTextInfo
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt32(ID);
                data.WriteUInt32(NextPageID);
                data.WriteInt32(PlayerConditionID);
                data.WriteUInt8(Flags);
                data.WriteBits(Text.Length, 12);
                data.FlushBits();

                data.WriteString(Text);
            }

            public uint ID { get; set; }
            public uint NextPageID { get; set; }
            public int PlayerConditionID { get; set; }
            public byte Flags { get; set; }
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

        public ObjectGuid Guid { get; set; }
        public uint TextID { get; set; }
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

        public uint TextID { get; set; }
        public bool Allow { get; set; }
        public float[] Probabilities { get; set; } = new float[SharedConst.MaxNpcTextOptions];
        public uint[] BroadcastTextID { get; set; } = new uint[SharedConst.MaxNpcTextOptions];
    }

    public class QueryGameObject : ClientPacket
    {
        public QueryGameObject(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GameObjectID = _worldPacket.ReadUInt32();
            Guid = _worldPacket.ReadPackedGuid();
        }

        public uint GameObjectID { get; set; }
        public ObjectGuid Guid { get; set; }
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
                foreach (int questItem in Stats.QuestItems)
                    statsData.WriteInt32(questItem);

                statsData.WriteUInt32(Stats.RequiredLevel);

            }

            _worldPacket.WriteUInt32(statsData.GetSize());
            if (statsData.GetSize() != 0)
                _worldPacket.WriteBytes(statsData);
        }

        public uint GameObjectID { get; set; }
        public bool Allow { get; set; }
        public GameObjectStats Stats { get; set; }
    }

    public class QueryCorpseLocationFromClient : ClientPacket
    {
        public QueryCorpseLocationFromClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player { get; set; }
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

        public ObjectGuid Player { get; set; }
        public ObjectGuid Transport { get; set; }
        public Vector3 Position { get; set; }
        public int ActualMapID { get; set; }
        public int MapID { get; set; }
        public bool Valid { get; set; }
    }

    public class QueryCorpseTransport : ClientPacket
    {
        public QueryCorpseTransport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();
            Transport = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Player { get; set; }
        public ObjectGuid Transport { get; set; }
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

        public ObjectGuid Player { get; set; }
        public Vector3 Position { get; set; }
        public float Facing { get; set; }
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

        public long CurrentTime { get; set; }
    }

    public class QuestPOIQuery : ClientPacket
    {
        public QuestPOIQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MissingQuestCount = _worldPacket.ReadInt32();

            for (byte i = 0; i < 50; ++i)
                MissingQuestPOIs[i] = _worldPacket.ReadUInt32();
        }

        public int MissingQuestCount { get; set; }
        public uint[] MissingQuestPOIs { get; set; } = new uint[50];
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
                    _worldPacket.WriteInt32(questPOIBlobData.WorldMapAreaID);
                    _worldPacket.WriteInt32(questPOIBlobData.Floor);
                    _worldPacket.WriteInt32(questPOIBlobData.Priority);
                    _worldPacket.WriteInt32(questPOIBlobData.Flags);
                    _worldPacket.WriteInt32(questPOIBlobData.WorldEffectID);
                    _worldPacket.WriteInt32(questPOIBlobData.PlayerConditionID);
                    _worldPacket.WriteInt32(questPOIBlobData.UnkWoD1);
                    _worldPacket.WriteInt32(questPOIBlobData.QuestPOIBlobPointStats.Count);

                    foreach (QuestPOIBlobPoint questPOIBlobPoint in questPOIBlobData.QuestPOIBlobPointStats)
                    {
                        _worldPacket.WriteInt32(questPOIBlobPoint.X);
                        _worldPacket.WriteInt32(questPOIBlobPoint.Y);
                    }
                }
            }
        }

        public List<QuestPOIData> QuestPOIDataStats { get; set; } = new List<QuestPOIData>();
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

        public uint[] QuestCompletionNPCs { get; set; }
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

        public List<QuestCompletionNPC> QuestCompletionNPCs { get; set; } = new List<QuestCompletionNPC>();
    }

    class QueryPetName : ClientPacket
    {
        public QueryPetName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid UnitGUID { get; set; }
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
                _worldPacket.WriteBits(Name.Length, 8);
                _worldPacket.WriteBit(HasDeclined);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteBits(DeclinedNames.name[i].Length, 7);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    _worldPacket.WriteString(DeclinedNames.name[i]);

                _worldPacket.WriteUInt32(Timestamp);
                _worldPacket.WriteString(Name);
            }

            _worldPacket.FlushBits();
        }

        public ObjectGuid UnitGUID { get; set; }
        public bool Allow { get; set; }

        public bool HasDeclined { get; set; }
        public DeclinedName DeclinedNames { get; set; } = new DeclinedName();
        public uint Timestamp { get; set; }
        public string Name { get; set; } = "";
    }

    class ItemTextQuery : ClientPacket
    {
        public ItemTextQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Id = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Id { get; set; }
    }

    class QueryItemTextResponse : ServerPacket
    {
        public QueryItemTextResponse() : base(ServerOpcodes.QueryItemTextResponse) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Valid);
            _worldPacket.WriteBits(Text.Length, 13);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Text);
            _worldPacket.WritePackedGuid(Id);
        }

        public ObjectGuid Id { get; set; }
        public bool Valid { get; set; }
        public string Text { get; set; }
    }

    class QueryRealmName : ClientPacket
    {
        public QueryRealmName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            VirtualRealmAddress = _worldPacket.ReadUInt32();
        }

        public uint VirtualRealmAddress { get; set; }
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

        public uint VirtualRealmAddress { get; set; }
        public byte LookupState { get; set; }
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

        public Optional<uint> VirtualRealmAddress { get; set; } = new Optional<uint>(); // current realm (?) (identifier made from the Index, BattleGroup and Region)
        public Optional<uint> NativeRealmAddress { get; set; } = new Optional<uint>(); // original realm (?) (identifier made from the Index, BattleGroup and Region)
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
                Contract.Assert(player.GetGUID() == guid);

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
            data.WriteBits(Name.Length, 6);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteBits(DeclinedNames.name[i].Length, 7);

            data.FlushBits();
            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                data.WriteString(DeclinedNames.name[i]);

            data.WritePackedGuid(AccountID);
            data.WritePackedGuid(BnetAccountID);
            data.WritePackedGuid(GuidActual);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8(RaceID);
            data.WriteUInt8(Sex);
            data.WriteUInt8(ClassID);
            data.WriteUInt8(Level);
            data.WriteString(Name);
        }

        public bool IsDeleted { get; set; }
        public ObjectGuid AccountID { get; set; }
        public ObjectGuid BnetAccountID { get; set; }
        public ObjectGuid GuidActual { get; set; }
        public string Name { get; set; } = "";
        public uint VirtualRealmAddress { get; set; }
        public Race RaceID { get; set; } = Race.None;
        public Gender Sex { get; set; } = Gender.None;
        public Class ClassID { get; set; } = Class.None;
        public byte Level { get; set; }
        public DeclinedName DeclinedNames { get; set; } = new DeclinedName();
    }

    public class CreatureStats
    {
        public string Title = "";
        public string TitleAlt = "";
        public string CursorName = "";
        public int CreatureType { get; set; }
        public int CreatureFamily { get; set; }
        public int Classification { get; set; }
        public float HpMulti { get; set; }
        public float EnergyMulti { get; set; }
        public bool Leader { get; set; }
        public List<uint> QuestItems { get; set; } = new List<uint>();
        public uint CreatureMovementInfoID { get; set; }
        public int HealthScalingExpansion { get; set; }
        public uint RequiredExpansion { get; set; }
        public uint VignetteID { get; set; }
        public uint[] Flags { get; set; } = new uint[2];
        public uint[] ProxyCreatureID { get; set; } = new uint[SharedConst.MaxCreatureKillCredit];
        public uint[] CreatureDisplayID { get; set; } = new uint[SharedConst.MaxCreatureModelIds];
        public StringArray Name { get; set; } = new StringArray(SharedConst.MaxCreatureNames);
        public StringArray NameAlt { get; set; } = new StringArray(SharedConst.MaxCreatureNames);
    }

    public struct DBQueryRecord
    {
        public ObjectGuid GUID { get; set; }
        public uint RecordID { get; set; }
    }

    public class GameObjectStats
    {
        public string[] Name { get; set; } = new string[4];
        public string IconName;
        public string CastBarCaption;
        public string UnkString;
        public uint Type { get; set; }
        public uint DisplayID { get; set; }
        public int[] Data { get; set; } = new int[33];
        public float Size { get; set; }
        public List<int> QuestItems { get; set; } = new List<int>();
        public uint RequiredLevel { get; set; }
    }

    public struct QuestPOIBlobPoint
    {
        public QuestPOIBlobPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    public class QuestPOIBlobData
    {
        public int BlobIndex { get; set; }
        public int ObjectiveIndex { get; set; }
        public int QuestObjectiveID { get; set; }
        public int QuestObjectID { get; set; }
        public int MapID { get; set; }
        public int WorldMapAreaID { get; set; }
        public int Floor { get; set; }
        public int Priority { get; set; }
        public int Flags { get; set; }
        public int WorldEffectID { get; set; }
        public int PlayerConditionID { get; set; }
        public int UnkWoD1 { get; set; }
        public List<QuestPOIBlobPoint> QuestPOIBlobPointStats { get; set; } = new List<QuestPOIBlobPoint>();
    }

    public class QuestPOIData
    {
        public uint QuestID { get; set; }
        public List<QuestPOIBlobData> QuestPOIBlobDataStats { get; set; } = new List<QuestPOIBlobData>();
    }

    class QuestCompletionNPC
    {
        public uint QuestID { get; set; }
        public List<uint> NPCs { get; set; } = new List<uint>();
    }
}
