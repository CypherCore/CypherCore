// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class QuestGiverStatusQuery : ClientPacket
	{
		public ObjectGuid QuestGiverGUID;

		public QuestGiverStatusQuery(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
		}
	}

	public class QuestGiverStatusMultipleQuery : ClientPacket
	{
		public QuestGiverStatusMultipleQuery(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class QuestGiverStatusPkt : ServerPacket
	{
		public QuestGiverInfo QuestGiver;

		public QuestGiverStatusPkt() : base(ServerOpcodes.QuestGiverStatus, ConnectionType.Instance)
		{
			QuestGiver = new QuestGiverInfo();
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(QuestGiver.Guid);
			_worldPacket.WriteUInt32((uint)QuestGiver.Status);
		}
	}

	public class QuestGiverStatusMultiple : ServerPacket
	{
		public List<QuestGiverInfo> QuestGiver = new();

		public QuestGiverStatusMultiple() : base(ServerOpcodes.QuestGiverStatusMultiple, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(QuestGiver.Count);

			foreach (QuestGiverInfo questGiver in QuestGiver)
			{
				_worldPacket.WritePackedGuid(questGiver.Guid);
				_worldPacket.WriteUInt32((uint)questGiver.Status);
			}
		}
	}

	public class QuestGiverHello : ClientPacket
	{
		public ObjectGuid QuestGiverGUID;

		public QuestGiverHello(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
		}
	}

	public class QueryQuestInfo : ClientPacket
	{
		public ObjectGuid QuestGiver;
		public uint QuestID;

		public QueryQuestInfo(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestID    = _worldPacket.ReadUInt32();
			QuestGiver = _worldPacket.ReadPackedGuid();
		}
	}

	public class QueryQuestInfoResponse : ServerPacket
	{
		public bool Allow;
		public QuestInfo Info = new();
		public uint QuestID;

		public QueryQuestInfoResponse() : base(ServerOpcodes.QueryQuestInfoResponse, ConnectionType.Instance)
		{
		}

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
				_worldPacket.WriteUInt32(Info.TimeAllowed);

				_worldPacket.WriteInt32(Info.Objectives.Count);
				_worldPacket.WriteInt64(Info.AllowableRaces);
				_worldPacket.WriteInt32(Info.TreasurePickerID);
				_worldPacket.WriteInt32(Info.Expansion);
				_worldPacket.WriteInt32(Info.ManagedWorldStateID);
				_worldPacket.WriteInt32(Info.QuestSessionBonus);
				_worldPacket.WriteInt32(Info.QuestGiverCreatureID);

				_worldPacket.WriteInt32(Info.ConditionalQuestDescription.Count);
				_worldPacket.WriteInt32(Info.ConditionalQuestCompletionLog.Count);

				foreach (QuestCompleteDisplaySpell rewardDisplaySpell in Info.RewardDisplaySpell)
					rewardDisplaySpell.Write(_worldPacket);

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
				_worldPacket.FlushBits();

				foreach (QuestObjective questObjective in Info.Objectives)
				{
					_worldPacket.WriteUInt32(questObjective.Id);
					_worldPacket.WriteUInt8((byte)questObjective.Type);
					_worldPacket.WriteInt8(questObjective.StorageIndex);
					_worldPacket.WriteInt32(questObjective.ObjectID);
					_worldPacket.WriteInt32(questObjective.Amount);
					_worldPacket.WriteUInt32((uint)questObjective.Flags);
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

				foreach (ConditionalQuestText conditionalQuestText in Info.ConditionalQuestDescription)
					conditionalQuestText.Write(_worldPacket);

				foreach (ConditionalQuestText conditionalQuestText in Info.ConditionalQuestCompletionLog)
					conditionalQuestText.Write(_worldPacket);
			}
		}
	}

	public class QuestUpdateAddCredit : ServerPacket
	{
		public ushort Count;
		public int ObjectID;
		public byte ObjectiveType;
		public uint QuestID;
		public ushort Required;

		public ObjectGuid VictimGUID;

		public QuestUpdateAddCredit() : base(ServerOpcodes.QuestUpdateAddCredit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(VictimGUID);
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WriteInt32(ObjectID);
			_worldPacket.WriteUInt16(Count);
			_worldPacket.WriteUInt16(Required);
			_worldPacket.WriteUInt8(ObjectiveType);
		}
	}

	internal class QuestUpdateAddCreditSimple : ServerPacket
	{
		public int ObjectID;
		public QuestObjectiveType ObjectiveType;

		public uint QuestID;

		public QuestUpdateAddCreditSimple() : base(ServerOpcodes.QuestUpdateAddCreditSimple, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WriteInt32(ObjectID);
			_worldPacket.WriteUInt8((byte)ObjectiveType);
		}
	}

	internal class QuestUpdateAddPvPCredit : ServerPacket
	{
		public ushort Count;

		public uint QuestID;

		public QuestUpdateAddPvPCredit() : base(ServerOpcodes.QuestUpdateAddPvpCredit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WriteUInt16(Count);
		}
	}

	public class QuestGiverOfferRewardMessage : ServerPacket
	{
		public List<ConditionalQuestText> ConditionalRewardText = new();
		public uint PortraitGiver;
		public int PortraitGiverModelSceneID;
		public uint PortraitGiverMount;
		public string PortraitGiverName = "";
		public string PortraitGiverText = "";

		public uint PortraitTurnIn;
		public string PortraitTurnInName = "";
		public string PortraitTurnInText = "";
		public QuestGiverOfferReward QuestData;
		public uint QuestGiverCreatureID;
		public uint QuestPackageID;
		public string QuestTitle = "";
		public string RewardText = "";

		public QuestGiverOfferRewardMessage() : base(ServerOpcodes.QuestGiverOfferRewardMessage)
		{
		}

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
	}

	public class QuestGiverChooseReward : ClientPacket
	{
		public QuestChoiceItem Choice;

		public ObjectGuid QuestGiverGUID;
		public uint QuestID;

		public QuestGiverChooseReward(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
			QuestID        = _worldPacket.ReadUInt32();
			Choice.Read(_worldPacket);
		}
	}

	public class QuestGiverQuestComplete : ServerPacket
	{
		public bool HideChatMessage;
		public ItemInstance ItemReward = new();
		public bool LaunchGossip;
		public bool LaunchQuest;
		public long MoneyReward;
		public uint NumSkillUpsReward;

		public uint QuestID;
		public uint SkillLineIDReward;
		public bool UseQuestReward;
		public uint XPReward;

		public QuestGiverQuestComplete() : base(ServerOpcodes.QuestGiverQuestComplete)
		{
		}

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
	}

	public class QuestGiverCompleteQuest : ClientPacket
	{
		public bool FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode

		public ObjectGuid QuestGiverGUID; // NPC / GameObject Guid for normal quest completion. Player Guid for self-completed quests
		public uint QuestID;

		public QuestGiverCompleteQuest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
			QuestID        = _worldPacket.ReadUInt32();
			FromScript     = _worldPacket.HasBit();
		}
	}

	public class QuestGiverCloseQuest : ClientPacket
	{
		public uint QuestID;

		public QuestGiverCloseQuest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestID = _worldPacket.ReadUInt32();
		}
	}

	public class QuestGiverQuestDetails : ServerPacket
	{
		public bool AutoLaunched;
		public List<ConditionalQuestText> ConditionalDescriptionText = new();
		public List<QuestDescEmote> DescEmotes = new();
		public string DescriptionText = "";
		public bool DisplayPopup;
		public ObjectGuid InformUnit;
		public List<uint> LearnSpells = new();
		public string LogDescription = "";
		public List<QuestObjectiveSimple> Objectives = new();
		public uint PortraitGiver;
		public int PortraitGiverModelSceneID;
		public uint PortraitGiverMount;
		public string PortraitGiverName = "";
		public string PortraitGiverText = "";
		public uint PortraitTurnIn;
		public string PortraitTurnInName = "";
		public string PortraitTurnInText = "";
		public uint[] QuestFlags = new uint[3];
		public int QuestGiverCreatureID;

		public ObjectGuid QuestGiverGUID;
		public uint QuestID;
		public int QuestPackageID;
		public int QuestSessionBonus;
		public int QuestStartItemID;
		public string QuestTitle = "";
		public QuestRewards Rewards = new();
		public bool StartCheat;
		public uint SuggestedPartyMembers;

		public QuestGiverQuestDetails() : base(ServerOpcodes.QuestGiverQuestDetails)
		{
		}

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
				_worldPacket.WriteInt32(obj.ObjectID);
				_worldPacket.WriteInt32(obj.Amount);
				_worldPacket.WriteUInt8(obj.Type);
			}

			_worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
			_worldPacket.WriteBits(DescriptionText.GetByteCount(), 12);
			_worldPacket.WriteBits(LogDescription.GetByteCount(), 12);
			_worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
			_worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
			_worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
			_worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
			_worldPacket.WriteBit(AutoLaunched);
			_worldPacket.WriteBit(false); // unused in client
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
	}

	public class QuestGiverRequestItems : ServerPacket
	{
		public bool AutoLaunched;
		public List<QuestObjectiveCollect> Collect = new();
		public uint CompEmoteDelay;
		public uint CompEmoteType;
		public string CompletionText = "";
		public List<ConditionalQuestText> ConditionalCompletionText = new();
		public List<QuestCurrency> Currency = new();
		public int MoneyToGet;
		public uint[] QuestFlags = new uint[3];
		public uint QuestGiverCreatureID;

		public ObjectGuid QuestGiverGUID;
		public uint QuestID;
		public string QuestTitle = "";
		public int StatusFlags;
		public uint SuggestPartyMembers;

		public QuestGiverRequestItems() : base(ServerOpcodes.QuestGiverRequestItems)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(QuestGiverGUID);
			_worldPacket.WriteUInt32(QuestGiverCreatureID);
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WriteUInt32(CompEmoteDelay);
			_worldPacket.WriteUInt32(CompEmoteType);
			_worldPacket.WriteUInt32(QuestFlags[0]);
			_worldPacket.WriteUInt32(QuestFlags[1]);
			_worldPacket.WriteUInt32(QuestFlags[2]);
			_worldPacket.WriteUInt32(SuggestPartyMembers);
			_worldPacket.WriteInt32(MoneyToGet);
			_worldPacket.WriteInt32(Collect.Count);
			_worldPacket.WriteInt32(Currency.Count);
			_worldPacket.WriteInt32(StatusFlags);

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
	}

	public class QuestGiverRequestReward : ClientPacket
	{
		public ObjectGuid QuestGiverGUID;
		public uint QuestID;

		public QuestGiverRequestReward(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
			QuestID        = _worldPacket.ReadUInt32();
		}
	}

	public class QuestGiverQueryQuest : ClientPacket
	{
		public ObjectGuid QuestGiverGUID;
		public uint QuestID;
		public bool RespondToGiver;

		public QuestGiverQueryQuest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
			QuestID        = _worldPacket.ReadUInt32();
			RespondToGiver = _worldPacket.HasBit();
		}
	}

	public class QuestGiverAcceptQuest : ClientPacket
	{
		public ObjectGuid QuestGiverGUID;
		public uint QuestID;
		public bool StartCheat;

		public QuestGiverAcceptQuest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestGiverGUID = _worldPacket.ReadPackedGuid();
			QuestID        = _worldPacket.ReadUInt32();
			StartCheat     = _worldPacket.HasBit();
		}
	}

	public class QuestLogRemoveQuest : ClientPacket
	{
		public byte Entry;

		public QuestLogRemoveQuest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Entry = _worldPacket.ReadUInt8();
		}
	}

	public class QuestGiverQuestListMessage : ServerPacket
	{
		public uint GreetEmoteDelay;
		public uint GreetEmoteType;
		public string Greeting = "";
		public List<ClientGossipText> QuestDataText = new();

		public ObjectGuid QuestGiverGUID;

		public QuestGiverQuestListMessage() : base(ServerOpcodes.QuestGiverQuestListMessage)
		{
		}

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
	}

	internal class QuestUpdateComplete : ServerPacket
	{
		public uint QuestID;

		public QuestUpdateComplete() : base(ServerOpcodes.QuestUpdateComplete)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
		}
	}

	internal class QuestConfirmAcceptResponse : ServerPacket
	{
		public ObjectGuid InitiatedBy;
		public uint QuestID;
		public string QuestTitle;

		public QuestConfirmAcceptResponse() : base(ServerOpcodes.QuestConfirmAccept)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WritePackedGuid(InitiatedBy);

			_worldPacket.WriteBits(QuestTitle.GetByteCount(), 10);
			_worldPacket.WriteString(QuestTitle);
		}
	}

	internal class QuestConfirmAccept : ClientPacket
	{
		public uint QuestID;

		public QuestConfirmAccept(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestID = _worldPacket.ReadUInt32();
		}
	}

	internal class QuestPushResultResponse : ServerPacket
	{
		public string QuestTitle;
		public QuestPushReason Result;

		public ObjectGuid SenderGUID;

		public QuestPushResultResponse() : base(ServerOpcodes.QuestPushResult)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(SenderGUID);
			_worldPacket.WriteUInt8((byte)Result);

			_worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
			_worldPacket.FlushBits();

			_worldPacket.WriteString(QuestTitle);
		}
	}

	internal class QuestPushResult : ClientPacket
	{
		public uint QuestID;
		public QuestPushReason Result;

		public ObjectGuid SenderGUID;

		public QuestPushResult(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			SenderGUID = _worldPacket.ReadPackedGuid();
			QuestID    = _worldPacket.ReadUInt32();
			Result     = (QuestPushReason)_worldPacket.ReadUInt8();
		}
	}

	internal class QuestGiverInvalidQuest : ServerPacket
	{
		public int ContributionRewardID;

		public QuestFailedReasons Reason;
		public string ReasonText = "";
		public bool SendErrorMessage;

		public QuestGiverInvalidQuest() : base(ServerOpcodes.QuestGiverInvalidQuest)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32((uint)Reason);
			_worldPacket.WriteInt32(ContributionRewardID);

			_worldPacket.WriteBit(SendErrorMessage);
			_worldPacket.WriteBits(ReasonText.GetByteCount(), 9);
			_worldPacket.FlushBits();

			_worldPacket.WriteString(ReasonText);
		}
	}

	internal class QuestUpdateFailedTimer : ServerPacket
	{
		public uint QuestID;

		public QuestUpdateFailedTimer() : base(ServerOpcodes.QuestUpdateFailedTimer)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
		}
	}

	internal class QuestGiverQuestFailed : ServerPacket
	{
		public uint QuestID;
		public InventoryResult Reason;

		public QuestGiverQuestFailed() : base(ServerOpcodes.QuestGiverQuestFailed)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(QuestID);
			_worldPacket.WriteUInt32((uint)Reason);
		}
	}

	internal class PushQuestToParty : ClientPacket
	{
		public uint QuestID;

		public PushQuestToParty(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			QuestID = _worldPacket.ReadUInt32();
		}
	}

	internal class DailyQuestsReset : ServerPacket
	{
		public int Count;

		public DailyQuestsReset() : base(ServerOpcodes.DailyQuestsReset)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Count);
		}
	}

	internal class QuestLogFull : ServerPacket
	{
		public QuestLogFull() : base(ServerOpcodes.QuestLogFull)
		{
		}

		public override void Write()
		{
		}
	}

	internal class RequestWorldQuestUpdate : ClientPacket
	{
		public RequestWorldQuestUpdate(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class WorldQuestUpdateResponse : ServerPacket
	{
		private List<WorldQuestUpdateInfo> WorldQuestUpdates = new();

		public WorldQuestUpdateResponse() : base(ServerOpcodes.WorldQuestUpdateResponse, ConnectionType.Instance)
		{
		}

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
	}

	internal class DisplayPlayerChoice : ServerPacket
	{
		public int ChoiceID;
		public bool CloseChoiceFrame;
		public uint CloseUISoundKitID;
		public long Duration;
		public bool HideWarboardHeader;
		public bool KeepOpenAfterChoice;
		public byte NumRerolls;
		public string PendingChoiceText;
		public string Question;
		public List<PlayerChoiceResponse> Responses = new();

		public ObjectGuid SenderGUID;
		public uint SoundKitID;
		public int UiTextureKitID;

		public DisplayPlayerChoice() : base(ServerOpcodes.DisplayPlayerChoice)
		{
		}

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
	}

	internal class ChoiceResponse : ClientPacket
	{
		public int ChoiceID;
		public bool IsReroll;
		public int ResponseIdentifier;

		public ChoiceResponse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ChoiceID           = _worldPacket.ReadInt32();
			ResponseIdentifier = _worldPacket.ReadInt32();
			IsReroll           = _worldPacket.HasBit();
		}
	}

	//Structs
	public class QuestGiverInfo
	{
		public ObjectGuid Guid;
		public QuestGiverStatus Status = QuestGiverStatus.None;

		public QuestGiverInfo()
		{
		}

		public QuestGiverInfo(ObjectGuid guid, QuestGiverStatus status)
		{
			Guid   = guid;
			Status = status;
		}
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

		public QuestCompleteDisplaySpell(uint spellID, uint playerConditionID)
		{
			SpellID           = spellID;
			PlayerConditionID = playerConditionID;
		}

		public void Write(WorldPacket data)
		{
			data.WriteUInt32(SpellID);
			data.WriteUInt32(PlayerConditionID);
		}
	}

	public class ConditionalQuestText
	{
		public int PlayerConditionID;
		public int QuestGiverCreatureID;
		public string Text = "";

		public ConditionalQuestText(int playerConditionID, int questGiverCreatureID, string text)
		{
			PlayerConditionID    = playerConditionID;
			QuestGiverCreatureID = questGiverCreatureID;
			Text                 = text;
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
		public uint AcceptedSoundKitID;
		public long AllowableRaces = -1;
		public string AreaDescription;
		public uint AreaGroupID;
		public uint CompleteSoundKitID;
		public List<ConditionalQuestText> ConditionalQuestCompletionLog = new();
		public List<ConditionalQuestText> ConditionalQuestDescription = new();
		public uint ContentTuningID;
		public int Expansion;
		public uint Flags;
		public uint FlagsEx;
		public uint FlagsEx2;
		public int[] ItemDrop = new int[SharedConst.QuestItemDropCount];
		public int[] ItemDropQuantity = new int[SharedConst.QuestItemDropCount];
		public string LogDescription;
		public string LogTitle;
		public int ManagedWorldStateID;
		public List<QuestObjective> Objectives = new();
		public uint POIContinent;
		public uint POIPriority;
		public float POIx;
		public float POIy;
		public uint PortraitGiver; // quest giver entry ?
		public int PortraitGiverModelSceneID;
		public uint PortraitGiverMount;
		public string PortraitGiverName;
		public string PortraitGiverText;
		public uint PortraitTurnIn; // quest turn in entry ?
		public string PortraitTurnInName;
		public string PortraitTurnInText;
		public string QuestCompletionLog;
		public string QuestDescription;
		public int QuestGiverCreatureID; // used to select ConditionalQuestText

		public uint QuestID;
		public uint QuestInfoID;
		public uint QuestPackageID;
		public int QuestSessionBonus;
		public int QuestSortID; // zone or sort to display in quest log
		public int QuestType;   // Accepted values: 0, 1 or 2. 0 == IsAutoComplete() (skip objectives/details)
		public bool ReadyForTranslation;
		public uint[] RewardAmount = new uint[SharedConst.QuestRewardItemCount];
		public int RewardArenaPoints;
		public int RewardArtifactCategoryID;
		public int RewardArtifactXPDifficulty;
		public float RewardArtifactXPMultiplier;
		public uint RewardBonusMoney;
		public uint[] RewardCurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
		public uint[] RewardCurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
		public List<QuestCompleteDisplaySpell> RewardDisplaySpell = new(); // reward spell, this spell will be displayed (icon)
		public int[] RewardFactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
		public uint RewardFactionFlags; // rep mask (unsure on what it does)
		public uint[] RewardFactionID = new uint[SharedConst.QuestRewardReputationsCount];
		public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];
		public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];
		public uint RewardHonor;
		public uint[] RewardItems = new uint[SharedConst.QuestRewardItemCount];
		public float RewardKillHonor;
		public int RewardMoney; // reward money (below max lvl)
		public uint RewardMoneyDifficulty;
		public float RewardMoneyMultiplier = 1.0f;
		public uint RewardNextQuest;   // client will request this quest from NPC, if not 0
		public uint RewardNumSkillUps; // reward skill points
		public uint RewardSkillLineID; // reward skill Id
		public uint RewardSpell;
		public uint RewardTitle;        // new 2.4.0, player gets this title (Id from CharTitles)
		public uint RewardXPDifficulty; // used for calculating rewarded experience
		public float RewardXPMultiplier = 1.0f;
		public uint StartItem;
		public uint SuggestedGroupNum;
		public uint TimeAllowed;
		public int TreasurePickerID;
		public QuestInfoChoiceItem[] UnfilteredChoiceItems = new QuestInfoChoiceItem[SharedConst.QuestRewardChoicesCount];

		public QuestInfo()
		{
			LogTitle           = "";
			LogDescription     = "";
			QuestDescription   = "";
			AreaDescription    = "";
			PortraitGiverText  = "";
			PortraitGiverName  = "";
			PortraitTurnInText = "";
			PortraitTurnInName = "";
			QuestCompletionLog = "";
		}
	}

	public struct QuestChoiceItem
	{
		public LootItemType LootItemType;
		public ItemInstance Item;
		public uint Quantity;

		public void Read(WorldPacket data)
		{
			data.ResetBitPos();
			LootItemType = (LootItemType)data.ReadBits<byte>(2);
			Item         = new ItemInstance();
			Item.Read(data);
			Quantity = data.ReadUInt32();
		}

		public void Write(WorldPacket data)
		{
			data.WriteBits((byte)LootItemType, 2);
			Item.Write(data);
			data.WriteUInt32(Quantity);
		}
	}

	public class QuestRewards
	{
		public uint ArtifactCategoryID;
		public uint ArtifactXP;
		public uint ChoiceItemCount;
		public QuestChoiceItem[] ChoiceItems = new QuestChoiceItem[SharedConst.QuestRewardChoicesCount];
		public uint[] CurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
		public uint[] CurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
		public int[] FactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
		public uint FactionFlags;
		public uint[] FactionID = new uint[SharedConst.QuestRewardReputationsCount];
		public int[] FactionOverride = new int[SharedConst.QuestRewardReputationsCount];
		public int[] FactionValue = new int[SharedConst.QuestRewardReputationsCount];
		public uint Honor;
		public bool IsBoostSpell;
		public uint ItemCount;
		public uint[] ItemID = new uint[SharedConst.QuestRewardItemCount];
		public uint[] ItemQty = new uint[SharedConst.QuestRewardItemCount];
		public uint Money;
		public uint NumSkillUps;
		public uint SkillLineID;
		public int[] SpellCompletionDisplayID = new int[SharedConst.QuestRewardDisplaySpellCount];
		public uint SpellCompletionID;
		public uint Title;
		public uint TreasurePickerID;
		public uint XP;

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
			data.WriteUInt32(TreasurePickerID);

			foreach (var choice in ChoiceItems)
				choice.Write(data);

			data.WriteBit(IsBoostSpell);
			data.FlushBits();
		}
	}

	public struct QuestDescEmote
	{
		public QuestDescEmote(int type = 0, uint delay = 0)
		{
			Type  = type;
			Delay = delay;
		}

		public int Type;
		public uint Delay;
	}

	public class QuestGiverOfferReward
	{
		public bool AutoLaunched = false;
		public List<QuestDescEmote> Emotes = new();
		public uint[] QuestFlags = new uint[3]; // Flags and FlagsEx
		public uint QuestGiverCreatureID = 0;

		public ObjectGuid QuestGiverGUID;
		public uint QuestID = 0;
		public QuestRewards Rewards = new();
		public uint SuggestedPartyMembers = 0;

		public void Write(WorldPacket data)
		{
			data.WritePackedGuid(QuestGiverGUID);
			data.WriteUInt32(QuestGiverCreatureID);
			data.WriteUInt32(QuestID);
			data.WriteUInt32(QuestFlags[0]); // Flags
			data.WriteUInt32(QuestFlags[1]); // FlagsEx
			data.WriteUInt32(QuestFlags[2]); // FlagsEx2
			data.WriteUInt32(SuggestedPartyMembers);

			data.WriteInt32(Emotes.Count);

			foreach (QuestDescEmote emote in Emotes)
			{
				data.WriteInt32(emote.Type);
				data.WriteUInt32(emote.Delay);
			}

			data.WriteBit(AutoLaunched);
			data.WriteBit(false); // Unused
			data.FlushBits();

			Rewards.Write(data);
		}
	}

	public struct QuestObjectiveSimple
	{
		public uint Id;
		public int ObjectID;
		public int Amount;
		public byte Type;
	}

	public struct QuestObjectiveCollect
	{
		public QuestObjectiveCollect(uint objectID = 0, int amount = 0, uint flags = 0)
		{
			ObjectID = objectID;
			Amount   = amount;
			Flags    = flags;
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
			Amount     = amount;
		}

		public uint CurrencyID;
		public int Amount;
	}

	internal struct WorldQuestUpdateInfo
	{
		public WorldQuestUpdateInfo(long lastUpdate, uint questID, uint timer, int variableID, int value)
		{
			LastUpdate = lastUpdate;
			QuestID    = questID;
			Timer      = timer;
			VariableID = variableID;
			Value      = value;
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
		public ItemInstance Item;
		public int Quantity;

		public void Write(WorldPacket data)
		{
			Item = new ItemInstance();
			Item.Write(data);
			data.WriteInt32(Quantity);
		}
	}

	internal class PlayerChoiceResponseReward
	{
		public uint ArenaPointCount;
		public List<PlayerChoiceResponseRewardEntry> Currencies = new();
		public List<PlayerChoiceResponseRewardEntry> Factions = new();
		public uint HonorPointCount;
		public List<PlayerChoiceResponseRewardEntry> ItemChoices = new();
		public List<PlayerChoiceResponseRewardEntry> Items = new();
		public ulong Money;
		public int PackageID;
		public int SkillLineID;
		public uint SkillPointCount;

		public int TitleID;
		public uint Xp;

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
	}

	internal struct PlayerChoiceResponseMawPower
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

	internal class PlayerChoiceResponse
	{
		public string Answer;
		public string ButtonTooltip;
		public int ChoiceArtFileID;
		public string Confirmation;
		public string Description;
		public int Flags;
		public byte GroupID;
		public string Header;
		public PlayerChoiceResponseMawPower? MawPower;
		public int ResponseID;
		public ushort ResponseIdentifier;
		public PlayerChoiceResponseReward Reward;
		public uint? RewardQuestID;
		public uint SoundKitID;
		public string SubHeader;
		public uint UiTextureAtlasElementID;
		public int UiTextureKitID;
		public uint WidgetSetID;

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