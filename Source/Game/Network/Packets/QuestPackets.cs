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
using Framework.Dynamic;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class QuestGiverStatusQuery : ClientPacket
    {
        public QuestGiverStatusQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid QuestGiverGUID;
    }

    public class QuestGiverStatusMultipleQuery : ClientPacket
    {
        public QuestGiverStatusMultipleQuery(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class QuestGiverStatusPkt : ServerPacket
    {
        public QuestGiverStatusPkt() : base(ServerOpcodes.QuestGiverStatus, ConnectionType.Instance)
        {
            QuestGiver = new QuestGiverInfo();
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(QuestGiver.Guid);
            _worldPacket.WriteUInt32(QuestGiver.Status);
        }

        public QuestGiverInfo QuestGiver;
    }

    public class QuestGiverStatusMultiple : ServerPacket
    {
        public QuestGiverStatusMultiple() : base(ServerOpcodes.QuestGiverStatusMultiple, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(QuestGiver.Count);
            foreach (QuestGiverInfo questGiver in QuestGiver)
            {
                _worldPacket.WritePackedGuid(questGiver.Guid);
                _worldPacket.WriteUInt32(questGiver.Status);
            }
        }

        public List<QuestGiverInfo> QuestGiver = new List<QuestGiverInfo>();
    }

    public class QuestGiverHello : ClientPacket
    {
        public QuestGiverHello(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid QuestGiverGUID;
    }

    public class QueryQuestInfo : ClientPacket
    {
        public QueryQuestInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
            QuestGiver = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid QuestGiver;
        public uint QuestID;
    }

    public class QueryQuestInfoResponse : ServerPacket
    {
        public QueryQuestInfoResponse() : base(ServerOpcodes.QueryQuestInfoResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);

            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            if (Allow)
            {
                _worldPacket.WriteInt32(Info.QuestID);
                _worldPacket.WriteInt32(Info.QuestType);
                _worldPacket.WriteInt32(Info.QuestLevel);
                _worldPacket.WriteInt32(Info.QuestScalingFactionGroup);
                _worldPacket.WriteInt32(Info.QuestMaxScalingLevel);
                _worldPacket.WriteInt32(Info.QuestPackageID);
                _worldPacket.WriteInt32(Info.QuestMinLevel);
                _worldPacket.WriteInt32(Info.QuestSortID);
                _worldPacket.WriteInt32(Info.QuestInfoID);
                _worldPacket.WriteInt32(Info.SuggestedGroupNum);
                _worldPacket.WriteInt32(Info.RewardNextQuest);
                _worldPacket.WriteInt32(Info.RewardXPDifficulty);
                _worldPacket.WriteFloat(Info.RewardXPMultiplier);
                _worldPacket.WriteInt32(Info.RewardMoney);
                _worldPacket.WriteInt32(Info.RewardMoneyDifficulty);
                _worldPacket.WriteFloat(Info.RewardMoneyMultiplier);
                _worldPacket.WriteInt32(Info.RewardBonusMoney);

                foreach (uint id in Info.RewardDisplaySpell)
                    _worldPacket.WriteInt32(id);

                _worldPacket.WriteInt32(Info.RewardSpell);
                _worldPacket.WriteInt32(Info.RewardHonor);
                _worldPacket.WriteFloat(Info.RewardKillHonor);
                _worldPacket.WriteInt32(Info.RewardArtifactXPDifficulty);
                _worldPacket.WriteFloat(Info.RewardArtifactXPMultiplier);
                _worldPacket.WriteInt32(Info.RewardArtifactCategoryID);
                _worldPacket.WriteInt32(Info.StartItem);
                _worldPacket.WriteUInt32(Info.Flags);
                _worldPacket.WriteUInt32(Info.FlagsEx);
                _worldPacket.WriteUInt32(Info.FlagsEx2);

                for (uint i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    _worldPacket.WriteInt32(Info.RewardItems[i]);
                    _worldPacket.WriteInt32(Info.RewardAmount[i]);
                    _worldPacket.WriteInt32(Info.ItemDrop[i]);
                    _worldPacket.WriteInt32(Info.ItemDropQuantity[i]);
                }

                for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                {
                    _worldPacket.WriteInt32(Info.UnfilteredChoiceItems[i].ItemID);
                    _worldPacket.WriteInt32(Info.UnfilteredChoiceItems[i].Quantity);
                    _worldPacket.WriteInt32(Info.UnfilteredChoiceItems[i].DisplayID);
                }

                _worldPacket.WriteInt32(Info.POIContinent);
                _worldPacket.WriteFloat(Info.POIx);
                _worldPacket.WriteFloat(Info.POIy);
                _worldPacket.WriteInt32(Info.POIPriority);

                _worldPacket.WriteInt32(Info.RewardTitle);
                _worldPacket.WriteInt32(Info.RewardArenaPoints);
                _worldPacket.WriteInt32(Info.RewardSkillLineID);
                _worldPacket.WriteInt32(Info.RewardNumSkillUps);

                _worldPacket.WriteInt32(Info.PortraitGiver);
                _worldPacket.WriteInt32(Info.PortraitGiverMount);
                _worldPacket.WriteInt32(Info.PortraitTurnIn);

                for (uint i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
                {
                    _worldPacket.WriteInt32(Info.RewardFactionID[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionValue[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionOverride[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionCapIn[i]);
                }

                _worldPacket.WriteInt32(Info.RewardFactionFlags);

                for (uint i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
                {
                    _worldPacket.WriteInt32(Info.RewardCurrencyID[i]);
                    _worldPacket.WriteInt32(Info.RewardCurrencyQty[i]);
                }

                _worldPacket.WriteInt32(Info.AcceptedSoundKitID);
                _worldPacket.WriteInt32(Info.CompleteSoundKitID);

                _worldPacket.WriteInt32(Info.AreaGroupID);
                _worldPacket.WriteInt32(Info.TimeAllowed);

                _worldPacket.WriteUInt32(Info.Objectives.Count);
                _worldPacket.WriteInt64(Info.AllowableRaces);
                _worldPacket.WriteInt32(Info.TreasurePickerID);
                _worldPacket.WriteInt32(Info.Expansion);

                _worldPacket.WriteBits(Info.LogTitle.GetByteCount(), 9);
                _worldPacket.WriteBits(Info.LogDescription.GetByteCount(), 12);
                _worldPacket.WriteBits(Info.QuestDescription.GetByteCount(), 12);
                _worldPacket.WriteBits(Info.AreaDescription.GetByteCount(), 9);
                _worldPacket.WriteBits(Info.PortraitGiverText.GetByteCount(), 10);
                _worldPacket.WriteBits(Info.PortraitGiverName.GetByteCount(), 8);
                _worldPacket.WriteBits(Info.PortraitTurnInText.GetByteCount(), 10);
                _worldPacket.WriteBits(Info.PortraitTurnInName.GetByteCount(), 8);
                _worldPacket.WriteBits(Info.QuestCompletionLog.GetByteCount(), 11);
                _worldPacket.FlushBits();

                foreach (QuestObjective questObjective in Info.Objectives)
                {
                    _worldPacket.WriteUInt32(questObjective.ID);
                    _worldPacket.WriteUInt8(questObjective.Type);
                    _worldPacket.WriteInt8(questObjective.StorageIndex);
                    _worldPacket.WriteInt32(questObjective.ObjectID);
                    _worldPacket.WriteInt32(questObjective.Amount);
                    _worldPacket.WriteUInt32(questObjective.Flags);
                    _worldPacket.WriteUInt32(questObjective.Flags2);
                    _worldPacket.WriteFloat(questObjective.ProgressBarWeight);

                    _worldPacket.WriteInt32(questObjective.VisualEffects.Length);
                    foreach (var visualEffect in questObjective.VisualEffects)
                        _worldPacket.WriteInt32(visualEffect);

                    _worldPacket.WriteBits(questObjective.Description.GetByteCount(), 8);
                    _worldPacket.FlushBits();

                    _worldPacket.WriteString(questObjective.Description);
                }

                _worldPacket.WriteString(Info.LogTitle);
                _worldPacket.WriteString(Info.LogDescription);
                _worldPacket.WriteString(Info.QuestDescription);
                _worldPacket.WriteString(Info.AreaDescription);
                _worldPacket.WriteString(Info.PortraitGiverText);
                _worldPacket.WriteString(Info.PortraitGiverName);
                _worldPacket.WriteString(Info.PortraitTurnInText);
                _worldPacket.WriteString(Info.PortraitTurnInName);
                _worldPacket.WriteString(Info.QuestCompletionLog);
            }
        }

        public bool Allow;
        public QuestInfo Info = new QuestInfo();
        public uint QuestID;
    }

    public class QuestUpdateAddCredit : ServerPacket
    {
        public QuestUpdateAddCredit() : base(ServerOpcodes.QuestUpdateAddCredit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VictimGUID);
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteInt32(ObjectID);
            _worldPacket.WriteUInt16(Count);
            _worldPacket.WriteUInt16(Required);
            _worldPacket.WriteUInt8(ObjectiveType);
        }

        public ObjectGuid VictimGUID;
        public int ObjectID;
        public uint QuestID;
        public ushort Count;
        public ushort Required;
        public byte ObjectiveType;
    }

    class QuestUpdateAddCreditSimple : ServerPacket
    {
        public QuestUpdateAddCreditSimple() : base(ServerOpcodes.QuestUpdateAddCreditSimple, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteInt32(ObjectID);
            _worldPacket.WriteUInt8(ObjectiveType);
        }

        public uint QuestID;
        public int ObjectID;
        public QuestObjectiveType ObjectiveType;
    }

    class QuestUpdateAddPvPCredit : ServerPacket
    {
        public QuestUpdateAddPvPCredit() : base(ServerOpcodes.QuestUpdateAddPvpCredit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteUInt16(Count);
        }

        public uint QuestID;
        public ushort Count;
    }

    public class QuestGiverOfferRewardMessage : ServerPacket
    {
        public QuestGiverOfferRewardMessage() : base(ServerOpcodes.QuestGiverOfferRewardMessage) { }

        public override void Write()
        {
            QuestData.Write(_worldPacket);
            _worldPacket.WriteInt32(QuestPackageID);
            _worldPacket.WriteInt32(PortraitGiver);
            _worldPacket.WriteInt32(PortraitGiverMount);
            _worldPacket.WriteInt32(PortraitTurnIn);

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(RewardText.GetByteCount(), 12);
            _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
            _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);

            _worldPacket.WriteString(QuestTitle);
            _worldPacket.WriteString(RewardText);
            _worldPacket.WriteString(PortraitGiverText);
            _worldPacket.WriteString(PortraitGiverName);
            _worldPacket.WriteString(PortraitTurnInText);
            _worldPacket.WriteString(PortraitTurnInName);
        }

        public uint PortraitTurnIn;
        public uint PortraitGiver;
        public uint PortraitGiverMount;
        public string QuestTitle = "";
        public string RewardText = "";
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public QuestGiverOfferReward QuestData;
        public uint QuestPackageID;
    }

    public class QuestGiverChooseReward : ClientPacket
    {
        public QuestGiverChooseReward(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
            ItemChoiceID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestID;
        public uint ItemChoiceID;
    }

    public class QuestGiverQuestComplete : ServerPacket
    {
        public QuestGiverQuestComplete() : base(ServerOpcodes.QuestGiverQuestComplete) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteUInt32(XPReward);
            _worldPacket.WriteInt64(MoneyReward);
            _worldPacket.WriteUInt32(SkillLineIDReward);
            _worldPacket.WriteUInt32(NumSkillUpsReward);

            _worldPacket.WriteBit(UseQuestReward);
            _worldPacket.WriteBit(LaunchGossip);
            _worldPacket.WriteBit(LaunchQuest);
            _worldPacket.WriteBit(HideChatMessage);

            ItemReward.Write(_worldPacket);
        }

        public uint QuestID;
        public uint XPReward;
        public long MoneyReward;
        public uint SkillLineIDReward;
        public uint NumSkillUpsReward;
        public bool UseQuestReward;
        public bool LaunchGossip;
        public bool LaunchQuest;
        public bool HideChatMessage;
        public ItemInstance ItemReward = new ItemInstance();
    }

    public class QuestGiverCompleteQuest : ClientPacket
    {
        public QuestGiverCompleteQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
            FromScript = _worldPacket.HasBit();
        }

        public ObjectGuid QuestGiverGUID; // NPC / GameObject guid for normal quest completion. Player guid for self-completed quests
        public uint QuestID;
        public bool FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode
    }

    public class QuestGiverQuestDetails : ServerPacket
    {
        public QuestGiverQuestDetails() : base(ServerOpcodes.QuestGiverQuestDetails) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(QuestGiverGUID);
            _worldPacket.WritePackedGuid(InformUnit);
            _worldPacket.WriteInt32(QuestID);
            _worldPacket.WriteInt32(QuestPackageID);
            _worldPacket.WriteInt32(PortraitGiver);
            _worldPacket.WriteUInt32(PortraitGiverMount);
            _worldPacket.WriteInt32(PortraitTurnIn);
            _worldPacket.WriteUInt32(QuestFlags[0]); // Flags
            _worldPacket.WriteUInt32(QuestFlags[1]); // FlagsEx
            _worldPacket.WriteInt32(SuggestedPartyMembers);
            _worldPacket.WriteInt32(LearnSpells.Count);
            _worldPacket.WriteUInt32(DescEmotes.Count);
            _worldPacket.WriteUInt32(Objectives.Count);
            _worldPacket.WriteInt32(QuestStartItemID);

            foreach (uint spell in LearnSpells)
                _worldPacket.WriteUInt32(spell);

            foreach (QuestDescEmote emote in DescEmotes)
            {
                _worldPacket.WriteInt32(emote.Type);
                _worldPacket.WriteUInt32(emote.Delay);
            }

            foreach (QuestObjectiveSimple obj in Objectives)
            {
                _worldPacket.WriteInt32(obj.ID);
                _worldPacket.WriteInt32(obj.ObjectID);
                _worldPacket.WriteInt32(obj.Amount);
                _worldPacket.WriteInt8(obj.Type);
            }

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(DescriptionText.GetByteCount(), 12);
            _worldPacket.WriteBits(LogDescription.GetByteCount(), 12);
            _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
            _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
            _worldPacket.WriteBit(AutoLaunched);
            _worldPacket.WriteBit(StartCheat);
            _worldPacket.WriteBit(DisplayPopup);
            _worldPacket.FlushBits();

            Rewards.Write(_worldPacket);

            _worldPacket.WriteString(QuestTitle);
            _worldPacket.WriteString(DescriptionText);
            _worldPacket.WriteString(LogDescription);
            _worldPacket.WriteString(PortraitGiverText);
            _worldPacket.WriteString(PortraitGiverName);
            _worldPacket.WriteString(PortraitTurnInText);
            _worldPacket.WriteString(PortraitTurnInName);
        }

        public ObjectGuid QuestGiverGUID;
        public ObjectGuid InformUnit;
        public uint QuestID;
        public int QuestPackageID;
        public uint[] QuestFlags = new uint[2];
        public uint SuggestedPartyMembers;
        public QuestRewards Rewards = new QuestRewards();
        public List<QuestObjectiveSimple> Objectives = new List<QuestObjectiveSimple>();
        public List<QuestDescEmote> DescEmotes = new List<QuestDescEmote>();
        public List<uint> LearnSpells = new List<uint>();
        public uint PortraitTurnIn;
        public uint PortraitGiver;
        public uint PortraitGiverMount;
        public int QuestStartItemID;
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public string QuestTitle = "";
        public string LogDescription = "";
        public string DescriptionText = "";
        public bool DisplayPopup;
        public bool StartCheat;
        public bool AutoLaunched;
    }

    public class QuestGiverRequestItems : ServerPacket
    {
        public QuestGiverRequestItems() : base(ServerOpcodes.QuestGiverRequestItems) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(QuestGiverGUID);
            _worldPacket.WriteInt32(QuestGiverCreatureID);
            _worldPacket.WriteInt32(QuestID);
            _worldPacket.WriteInt32(CompEmoteDelay);
            _worldPacket.WriteInt32(CompEmoteType);
            _worldPacket.WriteUInt32(QuestFlags[0]);
            _worldPacket.WriteUInt32(QuestFlags[1]);
            _worldPacket.WriteInt32(SuggestPartyMembers);
            _worldPacket.WriteInt32(MoneyToGet);
            _worldPacket.WriteInt32(Collect.Count);
            _worldPacket.WriteInt32(Currency.Count);
            _worldPacket.WriteInt32(StatusFlags);

            foreach (QuestObjectiveCollect obj in Collect)
            {
                _worldPacket.WriteInt32(obj.ObjectID);
                _worldPacket.WriteInt32(obj.Amount);
                _worldPacket.WriteUInt32(obj.Flags);
            }
            foreach (QuestCurrency cur in Currency)
            {
                _worldPacket.WriteInt32(cur.CurrencyID);
                _worldPacket.WriteInt32(cur.Amount);
            }

            _worldPacket.WriteBit(AutoLaunched);
            _worldPacket.FlushBits();

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(CompletionText.GetByteCount(), 12);

            _worldPacket.WriteString(QuestTitle);
            _worldPacket.WriteString(CompletionText);
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestGiverCreatureID;
        public uint QuestID;
        public uint CompEmoteDelay;
        public uint CompEmoteType;
        public bool AutoLaunched;
        public uint SuggestPartyMembers;
        public int MoneyToGet;
        public List<QuestObjectiveCollect> Collect = new List<QuestObjectiveCollect>();
        public List<QuestCurrency> Currency = new List<QuestCurrency>();
        public int StatusFlags;
        public uint[] QuestFlags = new uint[2];
        public string QuestTitle = "";
        public string CompletionText = "";
    }

    public class QuestGiverRequestReward : ClientPacket
    {
        public QuestGiverRequestReward(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestID;
    }

    public class QuestGiverQueryQuest : ClientPacket
    {
        public QuestGiverQueryQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
            RespondToGiver = _worldPacket.HasBit();
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestID;
        public bool RespondToGiver;
    }

    public class QuestGiverAcceptQuest : ClientPacket
    {
        public ObjectGuid QuestGiverGUID;
        public uint QuestID;
        public bool StartCheat;

        public QuestGiverAcceptQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestGiverGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
            StartCheat = _worldPacket.HasBit();
        }
    }

    public class QuestLogRemoveQuest : ClientPacket
    {
        public QuestLogRemoveQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Entry = _worldPacket.ReadUInt8();
        }

        public byte Entry;
    }

    public class QuestGiverQuestListMessage : ServerPacket
    {
        public QuestGiverQuestListMessage() : base(ServerOpcodes.QuestGiverQuestListMessage) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(QuestGiverGUID);
            _worldPacket.WriteUInt32(GreetEmoteDelay);
            _worldPacket.WriteUInt32(GreetEmoteType);
            _worldPacket.WriteUInt32(QuestDataText.Count);
            _worldPacket.WriteBits(Greeting.GetByteCount(), 11);
            _worldPacket.FlushBits();

            foreach (GossipText gossip in QuestDataText)
            {
                _worldPacket.WriteUInt32(gossip.QuestID);
                _worldPacket.WriteUInt32(gossip.QuestType);
                _worldPacket.WriteUInt32(gossip.QuestLevel);
                _worldPacket.WriteUInt32(gossip.QuestMaxScalingLevel);
                _worldPacket.WriteUInt32(gossip.QuestFlags);
                _worldPacket.WriteUInt32(gossip.QuestFlagsEx);

                _worldPacket.WriteBit(gossip.Repeatable);
                _worldPacket.WriteBits(gossip.QuestTitle.GetByteCount(), 9);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(gossip.QuestTitle);
            }

            _worldPacket.WriteString(Greeting);
        }

        public ObjectGuid QuestGiverGUID;
        public uint GreetEmoteDelay;
        public uint GreetEmoteType;
        public List<GossipText> QuestDataText = new List<GossipText>();
        public string Greeting = "";
    }

    class QuestUpdateComplete : ServerPacket
    {
        public QuestUpdateComplete() : base(ServerOpcodes.QuestUpdateComplete) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
        }

        public uint QuestID;
    }

    class QuestConfirmAcceptResponse : ServerPacket
    {
        public QuestConfirmAcceptResponse() : base(ServerOpcodes.QuestConfirmAccept) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WritePackedGuid(InitiatedBy);

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 10);
            _worldPacket.WriteString(QuestTitle);
        }

        public ObjectGuid InitiatedBy;
        public uint QuestID;
        public string QuestTitle;
    }

    class QuestConfirmAccept : ClientPacket
    {
        public QuestConfirmAccept(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
        }

        public uint QuestID;
    }

    class QuestPushResultResponse : ServerPacket
    {
        public QuestPushResultResponse() : base(ServerOpcodes.QuestPushResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SenderGUID);
            _worldPacket.WriteUInt8(Result);
        }

        public ObjectGuid SenderGUID;
        public QuestPushReason Result;
    }

    class QuestPushResult : ClientPacket
    {
        public QuestPushResult(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SenderGUID = _worldPacket.ReadPackedGuid();
            QuestID = _worldPacket.ReadUInt32();
            Result = (QuestPushReason)_worldPacket.ReadUInt8();
        }

        public ObjectGuid SenderGUID;
        public uint QuestID;
        public QuestPushReason Result;
    }

    class QuestGiverInvalidQuest : ServerPacket
    {
        public QuestGiverInvalidQuest() : base(ServerOpcodes.QuestGiverInvalidQuest) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Reason);
            _worldPacket.WriteInt32(ContributionRewardID);

            _worldPacket.WriteBit(SendErrorMessage);
            _worldPacket.WriteBits(ReasonText.GetByteCount(), 9);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(ReasonText);
        }

        public QuestFailedReasons Reason;
        public int ContributionRewardID;
        public bool SendErrorMessage;
        public string ReasonText = "";
    }

    class QuestUpdateFailedTimer : ServerPacket
    {
        public QuestUpdateFailedTimer() : base(ServerOpcodes.QuestUpdateFailedTimer) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
        }

        public uint QuestID;
    }

    class QuestGiverQuestFailed : ServerPacket
    {
        public QuestGiverQuestFailed() : base(ServerOpcodes.QuestGiverQuestFailed) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteUInt32(Reason);
        }

        public uint QuestID;
        public InventoryResult Reason;
    }

    class PushQuestToParty : ClientPacket
    {
        public PushQuestToParty(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
        }

        public uint QuestID;
    }

    class DailyQuestsReset : ServerPacket
    {
        public DailyQuestsReset() : base(ServerOpcodes.DailyQuestsReset) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Count);
        }

        public int Count;
    }

    class QuestLogFull : ServerPacket
    {
        public QuestLogFull() : base(ServerOpcodes.QuestLogFull) { }

        public override void Write() { }
    }

    class RequestWorldQuestUpdate : ClientPacket
    {
        public RequestWorldQuestUpdate(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class WorldQuestUpdate : ServerPacket
    {
        public WorldQuestUpdate() : base(ServerOpcodes.WorldQuestUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(WorldQuestUpdates.Count);

            foreach (WorldQuestUpdateInfo worldQuestUpdate in WorldQuestUpdates)
            {
                _worldPacket.WriteInt32(worldQuestUpdate.LastUpdate);
                _worldPacket.WriteUInt32(worldQuestUpdate.QuestID);
                _worldPacket.WriteUInt32(worldQuestUpdate.Timer);
                _worldPacket.WriteInt32(worldQuestUpdate.VariableID);
                _worldPacket.WriteInt32(worldQuestUpdate.Value);
            }
        }

        List<WorldQuestUpdateInfo> WorldQuestUpdates = new List<WorldQuestUpdateInfo>();
    }

    class DisplayPlayerChoice : ServerPacket
    {
        public DisplayPlayerChoice() : base(ServerOpcodes.DisplayPlayerChoice) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ChoiceID);
            _worldPacket.WriteUInt32(Responses.Count);
            _worldPacket.WritePackedGuid(SenderGUID);
            _worldPacket.WriteInt32(UiTextureKitID);
            _worldPacket.WriteBits(Question.GetByteCount(), 8);
            _worldPacket.WriteBit(CloseChoiceFrame);
            _worldPacket.WriteBit(HideWarboardHeader);
            _worldPacket.WriteBit(KeepOpenAfterChoice);
            _worldPacket.FlushBits();

            foreach (PlayerChoiceResponse response in Responses)
                response.Write(_worldPacket);

            _worldPacket.WriteString(Question);
        }

        public ObjectGuid SenderGUID;
        public int ChoiceID;
        public int UiTextureKitID;
        public string Question;
        public List<PlayerChoiceResponse> Responses = new List<PlayerChoiceResponse>();
        public bool CloseChoiceFrame;
        public bool HideWarboardHeader;
        public bool KeepOpenAfterChoice;
    }

    class ChoiceResponse : ClientPacket
    {
        public ChoiceResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ChoiceID = _worldPacket.ReadInt32();
            ResponseID = _worldPacket.ReadInt32();
        }

        public int ChoiceID;
        public int ResponseID;
    }

    //Structs
    public class QuestGiverInfo
    {
        public QuestGiverInfo() { }
        public QuestGiverInfo(ObjectGuid guid, QuestGiverStatus status)
        {
            Guid = guid;
            Status = status;
        }

        public ObjectGuid Guid;
        public QuestGiverStatus Status = QuestGiverStatus.None;
    }

    public struct QuestInfoChoiceItem
    {
        public uint ItemID;
        public uint Quantity;
        public uint DisplayID;
    }

    public class QuestInfo
    {
        public QuestInfo()
        {
            LogTitle = "";
            LogDescription = "";
            QuestDescription = "";
            AreaDescription = "";
            PortraitGiverText = "";
            PortraitGiverName = "";
            PortraitTurnInText = "";
            PortraitTurnInName = "";
            QuestCompletionLog = "";
        }

        public uint QuestID;
        public int QuestType; // Accepted values: 0, 1 or 2. 0 == IsAutoComplete() (skip objectives/details)
        public int QuestLevel; // may be -1, static data, in other cases must be used dynamic level: Player.GetQuestLevel (0 is not known, but assuming this is no longer valid for quest intended for client)
        public int QuestScalingFactionGroup;
        public int QuestMaxScalingLevel = 255;
        public uint QuestPackageID;
        public int QuestMinLevel;
        public int QuestSortID; // zone or sort to display in quest log
        public uint QuestInfoID;
        public uint SuggestedGroupNum;
        public uint RewardNextQuest; // client will request this quest from NPC, if not 0
        public uint RewardXPDifficulty; // used for calculating rewarded experience
        public float RewardXPMultiplier = 1.0f;
        public int RewardMoney; // reward money (below max lvl)
        public uint RewardMoneyDifficulty;
        public float RewardMoneyMultiplier = 1.0f;
        public uint RewardBonusMoney;
        public uint[] RewardDisplaySpell = new uint[SharedConst.QuestRewardDisplaySpellCount]; // reward spell, this spell will be displayed (icon)
        public uint RewardSpell;
        public uint RewardHonor;
        public float RewardKillHonor;
        public int RewardArtifactXPDifficulty;
        public float RewardArtifactXPMultiplier;
        public int RewardArtifactCategoryID;
        public uint StartItem;
        public uint Flags;
        public uint FlagsEx;
        public uint FlagsEx2;
        public uint POIContinent;
        public float POIx;
        public float POIy;
        public uint POIPriority;
        public long AllowableRaces = -1;
        public string LogTitle;
        public string LogDescription;
        public string QuestDescription;
        public string AreaDescription;
        public uint RewardTitle; // new 2.4.0, player gets this title (id from CharTitles)
        public int RewardArenaPoints;
        public uint RewardSkillLineID; // reward skill id
        public uint RewardNumSkillUps; // reward skill points
        public uint PortraitGiver; // quest giver entry ?
        public uint PortraitGiverMount;
        public uint PortraitTurnIn; // quest turn in entry ?
        public string PortraitGiverText;
        public string PortraitGiverName;
        public string PortraitTurnInText;
        public string PortraitTurnInName;
        public string QuestCompletionLog;
        public uint RewardFactionFlags; // rep mask (unsure on what it does)
        public uint AcceptedSoundKitID;
        public uint CompleteSoundKitID;
        public uint AreaGroupID;
        public uint TimeAllowed;
        public int TreasurePickerID;
        public int Expansion;
        public List<QuestObjective> Objectives = new List<QuestObjective>();
        public uint[] RewardItems = new uint[SharedConst.QuestRewardItemCount];
        public uint[] RewardAmount = new uint[SharedConst.QuestRewardItemCount];
        public int[] ItemDrop = new int[SharedConst.QuestItemDropCount];
        public int[] ItemDropQuantity = new int[SharedConst.QuestItemDropCount];
        public QuestInfoChoiceItem[] UnfilteredChoiceItems = new QuestInfoChoiceItem[SharedConst.QuestRewardChoicesCount];
        public uint[] RewardFactionID = new uint[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
        public uint[] RewardCurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint[] RewardCurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
    }

    public struct QuestChoiceItem
    {
        public ItemInstance Item;
        public uint Quantity;
    }

    public class QuestRewards
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ChoiceItemCount);
            data.WriteUInt32(ItemCount);

            for (int i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                data.WriteUInt32(ItemID[i]);
                data.WriteUInt32(ItemQty[i]);
            }

            data.WriteUInt32(Money);
            data.WriteUInt32(XP);
            data.WriteUInt64(ArtifactXP);
            data.WriteUInt32(ArtifactCategoryID);
            data.WriteUInt32(Honor);
            data.WriteUInt32(Title);
            data.WriteUInt32(FactionFlags);

            for (int i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                data.WriteUInt32(FactionID[i]);
                data.WriteInt32(FactionValue[i]);
                data.WriteInt32(FactionOverride[i]);
                data.WriteInt32(FactionCapIn[i]);
            }

            foreach (var id in SpellCompletionDisplayID)
                data.WriteInt32(id);

            data.WriteUInt32(SpellCompletionID);

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                data.WriteUInt32(CurrencyID[i]);
                data.WriteUInt32(CurrencyQty[i]);
            }

            data.WriteUInt32(SkillLineID);
            data.WriteUInt32(NumSkillUps);
            data.WriteInt32(TreasurePickerID);

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                ChoiceItems[i].Item.Write(data);
                data.WriteUInt32(ChoiceItems[i].Quantity);
            }

            data.WriteBit(IsBoostSpell);
            data.FlushBits();
        }

        public uint ChoiceItemCount;
        public uint ItemCount;
        public uint Money;
        public uint XP;
        public uint ArtifactXP;
        public uint ArtifactCategoryID;
        public uint Honor;
        public uint Title;
        public uint FactionFlags;
        public int[] SpellCompletionDisplayID = new int[SharedConst.QuestRewardDisplaySpellCount];
        public uint SpellCompletionID;
        public uint SkillLineID;
        public uint NumSkillUps;
        public uint TreasurePickerID;
        public QuestChoiceItem[] ChoiceItems = new QuestChoiceItem[SharedConst.QuestRewardChoicesCount];
        public uint[] ItemID = new uint[SharedConst.QuestRewardItemCount];
        public uint[] ItemQty = new uint[SharedConst.QuestRewardItemCount];
        public uint[] FactionID = new uint[SharedConst.QuestRewardReputationsCount];
        public int[] FactionValue = new int[SharedConst.QuestRewardReputationsCount];
        public int[] FactionOverride = new int[SharedConst.QuestRewardReputationsCount];
        public int[] FactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
        public uint[] CurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint[] CurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
        public bool IsBoostSpell;
    }

    public struct QuestDescEmote
    {
        public QuestDescEmote(uint type = 0, uint delay = 0)
        {
            Type = type;
            Delay = delay;
        }

        public uint Type;
        public uint Delay;
    }

    public class QuestGiverOfferReward
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(QuestGiverGUID);
            data.WriteUInt32(QuestGiverCreatureID);
            data.WriteUInt32(QuestID);
            data.WriteUInt32(QuestFlags[0]); // Flags
            data.WriteUInt32(QuestFlags[1]); // FlagsEx
            data.WriteUInt32(SuggestedPartyMembers);

            data.WriteInt32(Emotes.Count);
            foreach (QuestDescEmote emote in Emotes)
            {
                data.WriteInt32(emote.Type);
                data.WriteInt32(emote.Delay);
            }

            data.WriteBit(AutoLaunched);
            data.FlushBits();

            Rewards.Write(data);
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestGiverCreatureID = 0;
        public uint QuestID = 0;
        public bool AutoLaunched = false;
        public uint SuggestedPartyMembers = 0;
        public QuestRewards Rewards = new QuestRewards();
        public List<QuestDescEmote> Emotes = new List<QuestDescEmote>();
        public uint[] QuestFlags = new uint[2]; // Flags and FlagsEx
    }

    public struct QuestObjectiveSimple
    {
        public uint ID;
        public int ObjectID;
        public int Amount;
        public byte Type;
    }

    public struct QuestObjectiveCollect
    {
        public QuestObjectiveCollect(uint objectID = 0, int amount = 0, uint flags = 0)
        {
            ObjectID = objectID;
            Amount = amount;
            Flags = flags;
        }

        public uint ObjectID;
        public int Amount;
        public uint Flags;
    }

    public struct QuestCurrency
    {
        public QuestCurrency(uint currencyID = 0, int amount = 0)
        {
            CurrencyID = currencyID;
            Amount = amount;
        }

        public uint CurrencyID;
        public int Amount;
    }

    public struct GossipText
    {
        public GossipText(uint questID, uint questType, uint questLevel, uint questMaxScalingLevel, uint questFlags, uint questFlagsEx, bool repeatable, string questTitle)
        {
            QuestID = questID;
            QuestType = questType;
            QuestLevel = questLevel;
            QuestMaxScalingLevel = questMaxScalingLevel;
            QuestFlags = questFlags;
            QuestFlagsEx = questFlagsEx;
            Repeatable = repeatable;
            QuestTitle = questTitle;
        }

        public uint QuestID;
        public uint QuestType;
        public uint QuestLevel;
        public uint QuestMaxScalingLevel;
        public uint QuestFlags;
        public uint QuestFlagsEx;
        public bool Repeatable;
        public string QuestTitle;
    }

    struct WorldQuestUpdateInfo
    {
        public WorldQuestUpdateInfo(int lastUpdate, uint questID, uint timer, int variableID, int value)
        {
            LastUpdate = lastUpdate;
            QuestID = questID;
            Timer = timer;
            VariableID = variableID;
            Value = value;
        }

        public int LastUpdate;
        public uint QuestID;
        public uint Timer;
        // WorldState
        public int VariableID;
        public int Value;
    }

    public sealed class PlayerChoiceResponseRewardEntry
    {
        public void Write(WorldPacket data)
        {
            Item = new ItemInstance();
            Item.Write(data);
            data.WriteInt32(Quantity);
        }

        public ItemInstance Item;
        public int Quantity;
    }

    class PlayerChoiceResponseReward
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(TitleID);
            data.WriteInt32(PackageID);
            data.WriteInt32(SkillLineID);
            data.WriteUInt32(SkillPointCount);
            data.WriteUInt32(ArenaPointCount);
            data.WriteUInt32(HonorPointCount);
            data.WriteUInt64(Money);
            data.WriteUInt32(Xp);

            data.WriteUInt32(Items.Count);
            data.WriteUInt32(Currencies.Count);
            data.WriteUInt32(Factions.Count);
            data.WriteUInt32(ItemChoices.Count);

            foreach (PlayerChoiceResponseRewardEntry item in Items)
                item.Write(data);

            foreach (PlayerChoiceResponseRewardEntry currency in Currencies)
                currency.Write(data);

            foreach (PlayerChoiceResponseRewardEntry faction in Factions)
                faction.Write(data);

            foreach (PlayerChoiceResponseRewardEntry itemChoice in ItemChoices)
                itemChoice.Write(data);
        }

        public int TitleID;
        public int PackageID;
        public int SkillLineID;
        public uint SkillPointCount;
        public uint ArenaPointCount;
        public uint HonorPointCount;
        public ulong Money;
        public uint Xp;
        public List<PlayerChoiceResponseRewardEntry> Items = new List<PlayerChoiceResponseRewardEntry>();
        public List<PlayerChoiceResponseRewardEntry> Currencies = new List<PlayerChoiceResponseRewardEntry>();
        public List<PlayerChoiceResponseRewardEntry> Factions = new List<PlayerChoiceResponseRewardEntry>();
        public List<PlayerChoiceResponseRewardEntry> ItemChoices = new List<PlayerChoiceResponseRewardEntry>();
    }

    class PlayerChoiceResponse
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(ResponseID);
            data.WriteInt32(ChoiceArtFileID);
            data.WriteInt32(Flags);
            data.WriteUInt32(WidgetSetID);
            data.WriteUInt8(GroupID);

            data.WriteBits(Answer.GetByteCount(), 9);
            data.WriteBits(Header.GetByteCount(), 9);
            data.WriteBits(Description.GetByteCount(), 11);
            data.WriteBits(Confirmation.GetByteCount(), 7);

            data.WriteBit(Reward.HasValue);
            data.FlushBits();

            if (Reward.HasValue)
                Reward.Value.Write(data);

            data.WriteString(Answer);
            data.WriteString(Header);
            data.WriteString(Description);
            data.WriteString(Confirmation);
        }

        public int ResponseID;
        public int ChoiceArtFileID;
        public int Flags;
        public uint WidgetSetID;
        public byte GroupID;
        public string Answer;
        public string Header;
        public string Description;
        public string Confirmation;
        public Optional<PlayerChoiceResponseReward> Reward;
    }
}