// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Miscellaneous;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
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
            _worldPacket.WriteUInt64((ulong)QuestGiver.Status);
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
                _worldPacket.WriteUInt64((ulong)questGiver.Status);
            }
        }

        public List<QuestGiverInfo> QuestGiver = new();
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
                _worldPacket.WriteUInt32(Info.QuestID);
                _worldPacket.WriteInt32(Info.QuestType);
                _worldPacket.WriteUInt32(Info.QuestPackageID);
                _worldPacket.WriteUInt32(Info.ContentTuningID);
                _worldPacket.WriteInt32(Info.QuestSortID);
                _worldPacket.WriteUInt32(Info.QuestInfoID);
                _worldPacket.WriteUInt32(Info.SuggestedGroupNum);
                _worldPacket.WriteUInt32(Info.RewardNextQuest);
                _worldPacket.WriteUInt32(Info.RewardXPDifficulty);
                _worldPacket.WriteFloat(Info.RewardXPMultiplier);
                _worldPacket.WriteInt32(Info.RewardMoney);
                _worldPacket.WriteUInt32(Info.RewardMoneyDifficulty);
                _worldPacket.WriteFloat(Info.RewardMoneyMultiplier);
                _worldPacket.WriteUInt32(Info.RewardBonusMoney);
                _worldPacket.WriteInt32(Info.RewardDisplaySpell.Count);
                _worldPacket.WriteUInt32(Info.RewardSpell);
                _worldPacket.WriteUInt32(Info.RewardHonor);
                _worldPacket.WriteFloat(Info.RewardKillHonor);
                _worldPacket.WriteInt32(Info.RewardArtifactXPDifficulty);
                _worldPacket.WriteFloat(Info.RewardArtifactXPMultiplier);
                _worldPacket.WriteInt32(Info.RewardArtifactCategoryID);
                _worldPacket.WriteUInt32(Info.StartItem);
                _worldPacket.WriteUInt32(Info.Flags);
                _worldPacket.WriteUInt32(Info.FlagsEx);
                _worldPacket.WriteUInt32(Info.FlagsEx2);

                for (uint i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    _worldPacket.WriteUInt32(Info.RewardItems[i]);
                    _worldPacket.WriteUInt32(Info.RewardAmount[i]);
                    _worldPacket.WriteInt32(Info.ItemDrop[i]);
                    _worldPacket.WriteInt32(Info.ItemDropQuantity[i]);
                }

                for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                {
                    _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].ItemID);
                    _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].Quantity);
                    _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].DisplayID);
                }

                _worldPacket.WriteUInt32(Info.POIContinent);
                _worldPacket.WriteFloat(Info.POIx);
                _worldPacket.WriteFloat(Info.POIy);
                _worldPacket.WriteUInt32(Info.POIPriority);

                _worldPacket.WriteUInt32(Info.RewardTitle);
                _worldPacket.WriteInt32(Info.RewardArenaPoints);
                _worldPacket.WriteUInt32(Info.RewardSkillLineID);
                _worldPacket.WriteUInt32(Info.RewardNumSkillUps);

                _worldPacket.WriteUInt32(Info.PortraitGiver);
                _worldPacket.WriteUInt32(Info.PortraitGiverMount);
                _worldPacket.WriteInt32(Info.PortraitGiverModelSceneID);
                _worldPacket.WriteUInt32(Info.PortraitTurnIn);

                for (uint i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
                {
                    _worldPacket.WriteUInt32(Info.RewardFactionID[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionValue[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionOverride[i]);
                    _worldPacket.WriteInt32(Info.RewardFactionCapIn[i]);
                }

                _worldPacket.WriteUInt32(Info.RewardFactionFlags);

                for (uint i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
                {
                    _worldPacket.WriteUInt32(Info.RewardCurrencyID[i]);
                    _worldPacket.WriteUInt32(Info.RewardCurrencyQty[i]);
                }

                _worldPacket.WriteUInt32(Info.AcceptedSoundKitID);
                _worldPacket.WriteUInt32(Info.CompleteSoundKitID);

                _worldPacket.WriteUInt32(Info.AreaGroupID);
                _worldPacket.WriteInt64(Info.TimeAllowed);

                _worldPacket.WriteInt32(Info.Objectives.Count);
                _worldPacket.WriteUInt64(Info.AllowableRaces.RawValue);
                _worldPacket.WriteInt32(Info.TreasurePickerID.Count);
                _worldPacket.WriteInt32(Info.TreasurePickerID2.Count);
                _worldPacket.WriteInt32(Info.Expansion);
                _worldPacket.WriteInt32(Info.ManagedWorldStateID);
                _worldPacket.WriteInt32(Info.QuestSessionBonus);
                _worldPacket.WriteInt32(Info.QuestGiverCreatureID);

                _worldPacket.WriteInt32(Info.ConditionalQuestDescription.Count);
                _worldPacket.WriteInt32(Info.ConditionalQuestCompletionLog.Count);

                foreach (QuestCompleteDisplaySpell rewardDisplaySpell in Info.RewardDisplaySpell)
                    rewardDisplaySpell.Write(_worldPacket);

                if (!Info.TreasurePickerID.Empty())
                    foreach (var id in Info.TreasurePickerID)
                        _worldPacket.WriteInt32(id);

                if (!Info.TreasurePickerID2.Empty())
                    foreach (var id in Info.TreasurePickerID2)
                        _worldPacket.WriteInt32(id);

                _worldPacket.WriteBits(Info.LogTitle.GetByteCount(), 9);
                _worldPacket.WriteBits(Info.LogDescription.GetByteCount(), 12);
                _worldPacket.WriteBits(Info.QuestDescription.GetByteCount(), 12);
                _worldPacket.WriteBits(Info.AreaDescription.GetByteCount(), 9);
                _worldPacket.WriteBits(Info.PortraitGiverText.GetByteCount(), 10);
                _worldPacket.WriteBits(Info.PortraitGiverName.GetByteCount(), 8);
                _worldPacket.WriteBits(Info.PortraitTurnInText.GetByteCount(), 10);
                _worldPacket.WriteBits(Info.PortraitTurnInName.GetByteCount(), 8);
                _worldPacket.WriteBits(Info.QuestCompletionLog.GetByteCount(), 11);
                _worldPacket.WriteBit(Info.ReadyForTranslation);
                _worldPacket.WriteBit(Info.ResetByScheduler);
                _worldPacket.FlushBits();

                foreach (QuestObjective questObjective in Info.Objectives)
                {
                    _worldPacket.WriteUInt32(questObjective.Id);
                    _worldPacket.WriteInt32((int)questObjective.Type);
                    _worldPacket.WriteInt8(questObjective.StorageIndex);
                    _worldPacket.WriteInt32(questObjective.ObjectID);
                    _worldPacket.WriteInt32(questObjective.Amount);
                    _worldPacket.WriteUInt32((uint)questObjective.Flags);
                    _worldPacket.WriteUInt32((uint)questObjective.Flags2);
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

                foreach (ConditionalQuestText conditionalQuestText in Info.ConditionalQuestDescription)
                    conditionalQuestText.Write(_worldPacket);

                foreach (ConditionalQuestText conditionalQuestText in Info.ConditionalQuestCompletionLog)
                    conditionalQuestText.Write(_worldPacket);
            }
        }

        public bool Allow;
        public QuestInfo Info = new();
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
            _worldPacket.WriteUInt8((byte)ObjectiveType);
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
            _worldPacket.WriteUInt32(QuestPackageID);
            _worldPacket.WriteUInt32(PortraitGiver);
            _worldPacket.WriteUInt32(PortraitGiverMount);
            _worldPacket.WriteInt32(PortraitGiverModelSceneID);
            _worldPacket.WriteUInt32(PortraitTurnIn);
            _worldPacket.WriteUInt32(QuestGiverCreatureID);
            _worldPacket.WriteInt32(ConditionalRewardText.Count);

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(RewardText.GetByteCount(), 12);
            _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
            _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
            _worldPacket.FlushBits();

            foreach (ConditionalQuestText conditionalQuestText in ConditionalRewardText)
                conditionalQuestText.Write(_worldPacket);

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
        public int PortraitGiverModelSceneID;
        public uint QuestGiverCreatureID;
        public string QuestTitle = "";
        public string RewardText = "";
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public List<ConditionalQuestText> ConditionalRewardText = new();
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
            Choice.Read(_worldPacket);
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestID;
        public QuestChoiceItem Choice;
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
        public ItemInstance ItemReward = new();
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

    public class QuestGiverCloseQuest : ClientPacket
    {
        public QuestGiverCloseQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
        }

        public uint QuestID;
    }

    public class QuestGiverQuestDetails : ServerPacket
    {
        public QuestGiverQuestDetails() : base(ServerOpcodes.QuestGiverQuestDetails) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(QuestGiverGUID);
            _worldPacket.WritePackedGuid(InformUnit);
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteInt32(QuestPackageID);
            _worldPacket.WriteUInt32(PortraitGiver);
            _worldPacket.WriteUInt32(PortraitGiverMount);
            _worldPacket.WriteInt32(PortraitGiverModelSceneID);
            _worldPacket.WriteUInt32(PortraitTurnIn);
            _worldPacket.WriteUInt32(QuestFlags[0]); // Flags
            _worldPacket.WriteUInt32(QuestFlags[1]); // FlagsEx
            _worldPacket.WriteUInt32(QuestFlags[2]); // FlagsEx
            _worldPacket.WriteUInt32(SuggestedPartyMembers);
            _worldPacket.WriteInt32(LearnSpells.Count);
            _worldPacket.WriteInt32(DescEmotes.Count);
            _worldPacket.WriteInt32(Objectives.Count);
            _worldPacket.WriteInt32(QuestStartItemID);
            _worldPacket.WriteInt32(QuestInfoID);
            _worldPacket.WriteInt32(QuestSessionBonus);
            _worldPacket.WriteInt32(QuestGiverCreatureID);
            _worldPacket.WriteInt32(ConditionalDescriptionText.Count);

            foreach (uint spell in LearnSpells)
                _worldPacket.WriteUInt32(spell);

            foreach (QuestDescEmote emote in DescEmotes)
            {
                _worldPacket.WriteInt32(emote.Type);
                _worldPacket.WriteUInt32(emote.Delay);
            }

            foreach (QuestObjectiveSimple obj in Objectives)
            {
                _worldPacket.WriteUInt32(obj.Id);
                _worldPacket.WriteUInt8(obj.Type);
                _worldPacket.WriteInt32(obj.ObjectID);
                _worldPacket.WriteInt32(obj.Amount);
            }

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(DescriptionText.GetByteCount(), 12);
            _worldPacket.WriteBits(LogDescription.GetByteCount(), 12);
            _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
            _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
            _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
            _worldPacket.WriteBit(AutoLaunched);
            _worldPacket.WriteBit(FromContentPush);
            _worldPacket.WriteBit(false);   // unused in client
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

            foreach (ConditionalQuestText conditionalQuestText in ConditionalDescriptionText)
                conditionalQuestText.Write(_worldPacket);
        }

        public ObjectGuid QuestGiverGUID;
        public ObjectGuid InformUnit;
        public uint QuestID;
        public int QuestPackageID;
        public uint[] QuestFlags = new uint[3];
        public uint SuggestedPartyMembers;
        public QuestRewards Rewards = new();
        public List<QuestObjectiveSimple> Objectives = new();
        public List<QuestDescEmote> DescEmotes = new();
        public List<uint> LearnSpells = new();
        public uint PortraitTurnIn;
        public uint PortraitGiver;
        public uint PortraitGiverMount;
        public int PortraitGiverModelSceneID;
        public int QuestStartItemID;
        public int QuestInfoID;
        public int QuestSessionBonus;
        public int QuestGiverCreatureID;
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public string QuestTitle = "";
        public string LogDescription = "";
        public string DescriptionText = "";
        public List<ConditionalQuestText> ConditionalDescriptionText = new();
        public bool DisplayPopup;
        public bool StartCheat;
        public bool AutoLaunched;
        public bool FromContentPush;
    }

    public class QuestGiverRequestItems : ServerPacket
    {
        public QuestGiverRequestItems() : base(ServerOpcodes.QuestGiverRequestItems) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Collect.Count);
            _worldPacket.WriteInt32(Currency.Count);
            _worldPacket.WritePackedGuid(QuestGiverGUID);
            _worldPacket.WriteUInt32(QuestFlags[0]);
            _worldPacket.WriteUInt32(QuestFlags[1]);
            _worldPacket.WriteUInt32(QuestFlags[2]);
            _worldPacket.WriteInt32(StatusFlags);
            _worldPacket.WriteUInt32(QuestGiverCreatureID);
            _worldPacket.WriteUInt32(QuestID);
            _worldPacket.WriteUInt32(CompEmoteDelay);
            _worldPacket.WriteUInt32(CompEmoteType);
            _worldPacket.WriteUInt32(SuggestPartyMembers);
            _worldPacket.WriteInt32(MoneyToGet);
            _worldPacket.WriteInt32(QuestInfoID);

            foreach (QuestObjectiveCollect obj in Collect)
            {
                _worldPacket.WriteUInt32(obj.ObjectID);
                _worldPacket.WriteInt32(obj.Amount);
                _worldPacket.WriteUInt32(obj.Flags);
            }
            foreach (QuestCurrency cur in Currency)
            {
                _worldPacket.WriteUInt32(cur.CurrencyID);
                _worldPacket.WriteInt32(cur.Amount);
            }

            _worldPacket.WriteBit(AutoLaunched);
            _worldPacket.WriteBit(ResetByScheduler);
            _worldPacket.FlushBits();

            _worldPacket.WriteUInt32(QuestGiverCreatureID);
            _worldPacket.WriteInt32(ConditionalCompletionText.Count);

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(CompletionText.GetByteCount(), 12);
            _worldPacket.FlushBits();

            foreach (ConditionalQuestText conditionalQuestText in ConditionalCompletionText)
                conditionalQuestText.Write(_worldPacket);

            _worldPacket.WriteString(QuestTitle);
            _worldPacket.WriteString(CompletionText);
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestGiverCreatureID;
        public uint QuestID;
        public uint CompEmoteDelay;
        public uint CompEmoteType;
        public bool AutoLaunched;
        public bool ResetByScheduler;
        public uint SuggestPartyMembers;
        public int MoneyToGet;
        public List<QuestObjectiveCollect> Collect = new();
        public List<QuestCurrency> Currency = new();
        public int StatusFlags;
        public int QuestInfoID;
        public uint[] QuestFlags = new uint[3];
        public string QuestTitle = "";
        public string CompletionText = "";
        public List<ConditionalQuestText> ConditionalCompletionText = new();
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
            _worldPacket.WriteInt32(QuestDataText.Count);
            _worldPacket.WriteBits(Greeting.GetByteCount(), 11);
            _worldPacket.FlushBits();

            foreach (ClientGossipText gossip in QuestDataText)
                gossip.Write(_worldPacket);

            _worldPacket.WriteString(Greeting);
        }

        public ObjectGuid QuestGiverGUID;
        public uint GreetEmoteDelay;
        public uint GreetEmoteType;
        public List<ClientGossipText> QuestDataText = new();
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
            _worldPacket.WriteUInt8((byte)Result);

            _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(QuestTitle);
        }

        public ObjectGuid SenderGUID;
        public QuestPushReason Result;
        public string QuestTitle;
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
            _worldPacket.WriteUInt32((uint)Reason);
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
            _worldPacket.WriteUInt32((uint)Reason);
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

    class QuestForceRemoved : ServerPacket
    {
        uint QuestID;

        public QuestForceRemoved(uint questId) : base(ServerOpcodes.QuestForceRemoved)
        {
            QuestID = questId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QuestID);
        }
    }

    class RequestWorldQuestUpdate : ClientPacket
    {
        public RequestWorldQuestUpdate(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class WorldQuestUpdateResponse : ServerPacket
    {
        public WorldQuestUpdateResponse() : base(ServerOpcodes.WorldQuestUpdateResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(WorldQuestUpdates.Count);

            foreach (WorldQuestUpdateInfo worldQuestUpdate in WorldQuestUpdates)
            {
                _worldPacket.WriteInt64(worldQuestUpdate.LastUpdate);
                _worldPacket.WriteUInt32(worldQuestUpdate.QuestID);
                _worldPacket.WriteUInt32(worldQuestUpdate.Timer);
                _worldPacket.WriteInt32(worldQuestUpdate.VariableID);
                _worldPacket.WriteInt32(worldQuestUpdate.Value);
            }
        }

        List<WorldQuestUpdateInfo> WorldQuestUpdates = new();
    }

    class DisplayPlayerChoice : ServerPacket
    {
        public DisplayPlayerChoice() : base(ServerOpcodes.DisplayPlayerChoice) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ChoiceID);
            _worldPacket.WriteInt32(Responses.Count);
            _worldPacket.WritePackedGuid(SenderGUID);
            _worldPacket.WriteInt32(UiTextureKitID);
            _worldPacket.WriteUInt32(SoundKitID);
            _worldPacket.WriteUInt32(CloseUISoundKitID);
            _worldPacket.WriteUInt8(NumRerolls);
            _worldPacket.WriteInt64(Duration);
            _worldPacket.WriteBits(Question.GetByteCount(), 8);
            _worldPacket.WriteBits(PendingChoiceText.GetByteCount(), 8);
            _worldPacket.WriteBit(CloseChoiceFrame);
            _worldPacket.WriteBit(HideWarboardHeader);
            _worldPacket.WriteBit(KeepOpenAfterChoice);
            _worldPacket.FlushBits();

            foreach (PlayerChoiceResponse response in Responses)
                response.Write(_worldPacket);

            _worldPacket.WriteString(Question);
            _worldPacket.WriteString(PendingChoiceText);
        }

        public ObjectGuid SenderGUID;
        public int ChoiceID;
        public int UiTextureKitID;
        public uint SoundKitID;
        public uint CloseUISoundKitID;
        public byte NumRerolls;
        public long Duration;
        public string Question;
        public string PendingChoiceText;
        public List<PlayerChoiceResponse> Responses = new();
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
            ResponseIdentifier = _worldPacket.ReadInt32();
            IsReroll = _worldPacket.HasBit();
        }

        public int ChoiceID;
        public int ResponseIdentifier;
        public bool IsReroll;
    }

    class UiMapQuestLinesResponse : ServerPacket
    {
        public uint UiMapID;
        public List<uint> QuestLineXQuestIDs = new();
        public List<uint> QuestIDs = new();

        public UiMapQuestLinesResponse() : base(ServerOpcodes.UiMapQuestLinesResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(UiMapID);
            _worldPacket.WriteInt32(QuestLineXQuestIDs.Count);
            _worldPacket.WriteInt32(QuestIDs.Count);

            foreach (var questLineQuestID in QuestLineXQuestIDs)
                _worldPacket.WriteUInt32(questLineQuestID);

            foreach (var questID in QuestIDs)
                _worldPacket.WriteUInt32(questID);
        }
    }

    class UiMapQuestLinesRequest : ClientPacket
    {
        public int UiMapID;

        public UiMapQuestLinesRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UiMapID = _worldPacket.ReadInt32();
        }
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

    public struct QuestCompleteDisplaySpell
    {
        public uint SpellID;
        public uint PlayerConditionID;
        public int Type;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(PlayerConditionID);
            data.WriteInt32(Type);
        }
    }

    public class ConditionalQuestText
    {
        public int PlayerConditionID;
        public int QuestGiverCreatureID;
        public string Text = "";

        public ConditionalQuestText(int playerConditionID, int questGiverCreatureID, string text)
        {
            PlayerConditionID = playerConditionID;
            QuestGiverCreatureID = questGiverCreatureID;
            Text = text;
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PlayerConditionID);
            data.WriteInt32(QuestGiverCreatureID);
            data.WriteBits(Text.GetByteCount(), 12);
            data.FlushBits();

            data.WriteString(Text);
        }
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
        public uint ContentTuningID;
        public uint QuestPackageID;
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
        public List<QuestCompleteDisplaySpell> RewardDisplaySpell = new(); // reward spell, this spell will be displayed (icon)
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
        public RaceMask<ulong> AllowableRaces = new(0xFFFFFFFFFFFFFFFF);
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
        public int PortraitGiverModelSceneID;
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
        public long TimeAllowed;
        public List<int> TreasurePickerID = new();
        public List<int> TreasurePickerID2 = new();   // unknown purpose, used only sometimes and only if TreasurePickerID is empty
        public int Expansion;
        public int ManagedWorldStateID;
        public int QuestSessionBonus;
        public int QuestGiverCreatureID; // used to select ConditionalQuestText
        public List<QuestObjective> Objectives = new();
        public List<ConditionalQuestText> ConditionalQuestDescription = new();
        public List<ConditionalQuestText> ConditionalQuestCompletionLog = new();
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
        public bool ReadyForTranslation;
        public bool ResetByScheduler;
    }

    public struct QuestRewardItem
    {
        public uint ItemID;
        public uint ItemQty;
        public QuestRewardContextFlags? ContextFlags;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(ItemQty);
            data.WriteBit(ContextFlags.HasValue);
            data.FlushBits();
            if (ContextFlags.HasValue)
                data.WriteInt32((int)ContextFlags);
        }
    }

    public struct QuestChoiceItem
    {
        public LootItemType LootItemType;
        public ItemInstance Item;
        public uint Quantity;
        public QuestRewardContextFlags? ContextFlags;

        public void Read(WorldPacket data)
        {
            data.ResetBitPos();
            LootItemType = (LootItemType)data.ReadBits<byte>(2);
            bool hasContextFlags = data.HasBit();
            Item = new ItemInstance();
            Item.Read(data);
            Quantity = data.ReadUInt32();
            if (hasContextFlags)
                ContextFlags = (QuestRewardContextFlags)data.ReadInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits((byte)LootItemType, 2);
            data.WriteBit(ContextFlags.HasValue);
            Item.Write(data);
            data.WriteUInt32(Quantity);
            if (ContextFlags.HasValue)
                data.WriteInt32((int)ContextFlags.Value);
        }
    }

    public struct QuestRewardCurrency
    {
        public uint CurrencyID;
        public uint CurrencyQty;
        public int BonusQty;
        public QuestRewardContextFlags? ContextFlags;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(CurrencyID);
            data.WriteUInt32(CurrencyQty);
            data.WriteInt32(BonusQty);
            data.WriteBit(ContextFlags.HasValue);
            data.FlushBits();
            if (ContextFlags.HasValue)
                data.WriteInt32((int)ContextFlags);
        }
    }

    public class QuestRewards
    {
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
        public List<int> TreasurePickerID = new();
        public QuestChoiceItem[] ChoiceItems = new QuestChoiceItem[SharedConst.QuestRewardChoicesCount];
        public QuestRewardItem[] Items = new QuestRewardItem[SharedConst.QuestRewardItemCount];
        public uint[] FactionID = new uint[SharedConst.QuestRewardReputationsCount];
        public int[] FactionValue = new int[SharedConst.QuestRewardReputationsCount];
        public int[] FactionOverride = new int[SharedConst.QuestRewardReputationsCount];
        public int[] FactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
        public QuestRewardCurrency[] Currencies = new QuestRewardCurrency[SharedConst.QuestRewardCurrencyCount];
        public bool IsBoostSpell;

        public void Write(WorldPacket data)
        {
            foreach (QuestRewardItem item in Items)
                item.Write(data);

            data.WriteUInt32(ChoiceItemCount);
            data.WriteUInt32(ItemCount);
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
            data.WriteUInt32(SkillLineID);
            data.WriteUInt32(NumSkillUps);
            data.WriteInt32(TreasurePickerID.Count);
            if (!TreasurePickerID.Empty())
                foreach (var id in TreasurePickerID)
                    data.WriteInt32(id);

            foreach (QuestRewardCurrency currency in Currencies)
                currency.Write(data);

            data.WriteBit(IsBoostSpell);
            data.FlushBits();

            foreach (QuestChoiceItem choiceItem in ChoiceItems)
                choiceItem.Write(data);
        }
    }

    public struct QuestDescEmote
    {
        public QuestDescEmote(int type = 0, uint delay = 0)
        {
            Type = type;
            Delay = delay;
        }

        public int Type;
        public uint Delay;
    }

    public class QuestGiverOfferReward
    {
        public void Write(WorldPacket data)
        {
            Rewards.Write(data); // QuestRewards
            data.WriteInt32(Emotes.Count);
            data.WritePackedGuid(QuestGiverGUID);
            data.WriteUInt32(QuestFlags[0]); // Flags
            data.WriteUInt32(QuestFlags[1]); // FlagsEx
            data.WriteUInt32(QuestFlags[2]); // FlagsEx2
            data.WriteUInt32(QuestGiverCreatureID);
            data.WriteUInt32(QuestID);
            data.WriteUInt32(SuggestedPartyMembers);
            data.WriteInt32(QuestInfoID);

            foreach (QuestDescEmote emote in Emotes)
            {
                data.WriteInt32(emote.Type);
                data.WriteUInt32(emote.Delay);
            }

            data.WriteBit(AutoLaunched);
            data.WriteBit(false);   // Unused
            data.WriteBit(ResetByScheduler);
            data.FlushBits();
        }

        public ObjectGuid QuestGiverGUID;
        public uint QuestGiverCreatureID;
        public uint QuestID;
        public bool AutoLaunched;
        public bool ResetByScheduler;
        public uint SuggestedPartyMembers;
        public QuestRewards Rewards = new();
        public List<QuestDescEmote> Emotes = new();
        public uint[] QuestFlags = new uint[3]; // Flags and FlagsEx
        public int QuestInfoID;
    }

    public struct QuestObjectiveSimple
    {
        public uint Id;
        public byte Type;
        public int ObjectID;
        public int Amount;
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

    struct WorldQuestUpdateInfo
    {
        public WorldQuestUpdateInfo(long lastUpdate, uint questID, uint timer, int variableID, int value)
        {
            LastUpdate = lastUpdate;
            QuestID = questID;
            Timer = timer;
            VariableID = variableID;
            Value = value;
        }

        public long LastUpdate;
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

            data.WriteInt32(Items.Count);
            data.WriteInt32(Currencies.Count);
            data.WriteInt32(Factions.Count);
            data.WriteInt32(ItemChoices.Count);

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
        public List<PlayerChoiceResponseRewardEntry> Items = new();
        public List<PlayerChoiceResponseRewardEntry> Currencies = new();
        public List<PlayerChoiceResponseRewardEntry> Factions = new();
        public List<PlayerChoiceResponseRewardEntry> ItemChoices = new();
    }

    struct PlayerChoiceResponseMawPower
    {
        public int Unused901_1;
        public int TypeArtFileID;
        public int? Rarity;
        public uint? RarityColor;
        public int Unused901_2;
        public int SpellID;
        public int MaxStacks;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Unused901_1);
            data.WriteInt32(TypeArtFileID);
            data.WriteInt32(Unused901_2);
            data.WriteInt32(SpellID);
            data.WriteInt32(MaxStacks);
            data.WriteBit(Rarity.HasValue);
            data.WriteBit(RarityColor.HasValue);
            data.FlushBits();

            if (Rarity.HasValue)
                data.WriteInt32(Rarity.Value);

            if (RarityColor.HasValue)
                data.WriteUInt32(RarityColor.Value);
        }
    }

    class PlayerChoiceResponse
    {
        public int ResponseID;
        public ushort ResponseIdentifier;
        public int ChoiceArtFileID;
        public int Flags;
        public uint WidgetSetID;
        public uint UiTextureAtlasElementID;
        public uint SoundKitID;
        public byte GroupID;
        public int UiTextureKitID;
        public string Answer;
        public string Header;
        public string SubHeader;
        public string ButtonTooltip;
        public string Description;
        public string Confirmation;
        public PlayerChoiceResponseReward Reward;
        public uint? RewardQuestID;
        public PlayerChoiceResponseMawPower? MawPower;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ResponseID);
            data.WriteUInt16(ResponseIdentifier);
            data.WriteInt32(ChoiceArtFileID);
            data.WriteInt32(Flags);
            data.WriteUInt32(WidgetSetID);
            data.WriteUInt32(UiTextureAtlasElementID);
            data.WriteUInt32(SoundKitID);
            data.WriteUInt8(GroupID);
            data.WriteInt32(UiTextureKitID);

            data.WriteBits(Answer.GetByteCount(), 9);
            data.WriteBits(Header.GetByteCount(), 9);
            data.WriteBits(SubHeader.GetByteCount(), 7);
            data.WriteBits(ButtonTooltip.GetByteCount(), 9);
            data.WriteBits(Description.GetByteCount(), 11);
            data.WriteBits(Confirmation.GetByteCount(), 7);

            data.WriteBit(RewardQuestID.HasValue);
            data.WriteBit(Reward != null);
            data.WriteBit(MawPower.HasValue);
            data.FlushBits();

            if (Reward != null)
                Reward.Write(data);

            data.WriteString(Answer);
            data.WriteString(Header);
            data.WriteString(SubHeader);
            data.WriteString(ButtonTooltip);
            data.WriteString(Description);
            data.WriteString(Confirmation);

            if (RewardQuestID.HasValue)
                data.WriteUInt32(RewardQuestID.Value);

            if (MawPower.HasValue)
                MawPower.Value.Write(data);
        }
    }
}