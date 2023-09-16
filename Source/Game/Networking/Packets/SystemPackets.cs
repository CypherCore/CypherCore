// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class FeatureSystemStatus : ServerPacket
    {
        public FeatureSystemStatus() : base(ServerOpcodes.FeatureSystemStatus) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(ComplaintStatus);

            _worldPacket.WriteUInt32(CfgRealmID);
            _worldPacket.WriteInt32(CfgRealmRecID);

            _worldPacket.WriteUInt32(RAFSystem.MaxRecruits);
            _worldPacket.WriteUInt32(RAFSystem.MaxRecruitMonths);
            _worldPacket.WriteUInt32(RAFSystem.MaxRecruitmentUses);
            _worldPacket.WriteUInt32(RAFSystem.DaysInCycle);
            _worldPacket.WriteUInt32(RAFSystem.Unknown1007);

            _worldPacket.WriteUInt32(TokenPollTimeSeconds);
            _worldPacket.WriteUInt32(KioskSessionMinutes);
            _worldPacket.WriteInt64(TokenBalanceAmount);

            _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
            _worldPacket.WriteUInt32(ClubsPresenceUpdateTimer);
            _worldPacket.WriteUInt32(HiddenUIClubsPresenceUpdateTimer);

            _worldPacket.WriteInt32(ActiveSeason);
            _worldPacket.WriteInt32(GameRuleValues.Count);

            _worldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
            _worldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
            _worldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);

            foreach (GameRuleValuePair gameRuleValue in GameRuleValues)
                gameRuleValue.Write(_worldPacket);

            _worldPacket.WriteBit(VoiceEnabled);
            _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
            _worldPacket.WriteBit(BpayStoreEnabled);
            _worldPacket.WriteBit(BpayStoreAvailable);
            _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
            _worldPacket.WriteBit(ItemRestorationButtonEnabled);
            _worldPacket.WriteBit(BrowserEnabled);

            _worldPacket.WriteBit(SessionAlert.HasValue);
            _worldPacket.WriteBit(RAFSystem.Enabled);
            _worldPacket.WriteBit(RAFSystem.RecruitingEnabled);
            _worldPacket.WriteBit(CharUndeleteEnabled);
            _worldPacket.WriteBit(RestrictedAccount);
            _worldPacket.WriteBit(CommerceSystemEnabled);
            _worldPacket.WriteBit(TutorialsEnabled);

            _worldPacket.WriteBit(Unk67);
            _worldPacket.WriteBit(WillKickFromWorld);
            _worldPacket.WriteBit(KioskModeEnabled);
            _worldPacket.WriteBit(CompetitiveModeEnabled);
            _worldPacket.WriteBit(TokenBalanceEnabled);
            _worldPacket.WriteBit(WarModeFeatureEnabled);
            _worldPacket.WriteBit(ClubsEnabled);
            _worldPacket.WriteBit(ClubsBattleNetClubTypeAllowed);

            _worldPacket.WriteBit(ClubsCharacterClubTypeAllowed);
            _worldPacket.WriteBit(ClubsPresenceUpdateEnabled);
            _worldPacket.WriteBit(VoiceChatDisabledByParentalControl);
            _worldPacket.WriteBit(VoiceChatMutedByParentalControl);
            _worldPacket.WriteBit(QuestSessionEnabled);
            _worldPacket.WriteBit(IsMuted);
            _worldPacket.WriteBit(ClubFinderEnabled);
            _worldPacket.WriteBit(Unknown901CheckoutRelated);

            _worldPacket.WriteBit(TextToSpeechFeatureEnabled);
            _worldPacket.WriteBit(ChatDisabledByDefault);
            _worldPacket.WriteBit(ChatDisabledByPlayer);
            _worldPacket.WriteBit(LFGListCustomRequiresAuthenticator);
            _worldPacket.WriteBit(AddonsDisabled);
            _worldPacket.WriteBit(Unused1000);
            _worldPacket.WriteBit(ContentTrackingEnabled);
            _worldPacket.WriteBit(IsSellAllJunkEnabled);

            _worldPacket.FlushBits();

            {
                _worldPacket.WriteBit(QuickJoinConfig.ToastsDisabled);
                _worldPacket.WriteFloat(QuickJoinConfig.ToastDuration);
                _worldPacket.WriteFloat(QuickJoinConfig.DelayDuration);
                _worldPacket.WriteFloat(QuickJoinConfig.QueueMultiplier);
                _worldPacket.WriteFloat(QuickJoinConfig.PlayerMultiplier);
                _worldPacket.WriteFloat(QuickJoinConfig.PlayerFriendValue);
                _worldPacket.WriteFloat(QuickJoinConfig.PlayerGuildValue);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleInitialThreshold);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDecayTime);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePrioritySpike);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleMinThreshold);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityNormal);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityLow);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPHonorThreshold);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityDefault);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityAbove);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityBelow);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingAbove);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingBelow);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleRfPriorityAbove);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleRfIlvlScalingAbove);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDfMaxItemLevel);
                _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDfBestPriority);
            }

            if (SessionAlert.HasValue)
            {
                _worldPacket.WriteInt32(SessionAlert.Value.Delay);
                _worldPacket.WriteInt32(SessionAlert.Value.Period);
                _worldPacket.WriteInt32(SessionAlert.Value.DisplayTime);
            }

            _worldPacket.WriteBit(Squelch.IsSquelched);
            _worldPacket.WritePackedGuid(Squelch.BnetAccountGuid);
            _worldPacket.WritePackedGuid(Squelch.GuildGuid);

            if (EuropaTicketSystemStatus.HasValue)
                EuropaTicketSystemStatus.Value.Write(_worldPacket);
        }

        public bool VoiceEnabled;
        public bool BrowserEnabled;
        public bool BpayStoreAvailable;
        public bool BpayStoreEnabled;
        public SessionAlertConfig? SessionAlert;
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public uint CfgRealmID;
        public byte ComplaintStatus;
        public int CfgRealmRecID;
        public uint TokenPollTimeSeconds;
        public long TokenBalanceAmount;
        public uint BpayStoreProductDeliveryDelay;
        public uint ClubsPresenceUpdateTimer;
        public uint HiddenUIClubsPresenceUpdateTimer; // Timer for updating club presence when communities ui frame is hidden
        public uint KioskSessionMinutes;
        public int ActiveSeason; // Currently active Classic season
        public short MaxPlayerNameQueriesPerPacket = 50;
        public short PlayerNameQueryTelemetryInterval = 600;
        public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
        public bool ItemRestorationButtonEnabled;
        public bool CharUndeleteEnabled; // Implemented
        public bool BpayStoreDisabledByParentalControls;
        public bool CommerceSystemEnabled;
        public bool Unk67;
        public bool WillKickFromWorld;
        public bool RestrictedAccount;
        public bool TutorialsEnabled;
        public bool NPETutorialsEnabled;
        public bool KioskModeEnabled;
        public bool CompetitiveModeEnabled;
        public bool TokenBalanceEnabled;
        public bool WarModeFeatureEnabled;
        public bool ClubsEnabled;
        public bool ClubsBattleNetClubTypeAllowed;
        public bool ClubsCharacterClubTypeAllowed;
        public bool ClubsPresenceUpdateEnabled;
        public bool VoiceChatDisabledByParentalControl;
        public bool VoiceChatMutedByParentalControl;
        public bool QuestSessionEnabled;
        public bool IsMuted;
        public bool ClubFinderEnabled;
        public bool Unknown901CheckoutRelated;
        public bool TextToSpeechFeatureEnabled;
        public bool ChatDisabledByDefault;
        public bool ChatDisabledByPlayer;
        public bool LFGListCustomRequiresAuthenticator;
        public bool AddonsDisabled;
        public bool Unused1000;
        public bool ContentTrackingEnabled;
        public bool IsSellAllJunkEnabled;

        public SocialQueueConfig QuickJoinConfig;
        public SquelchInfo Squelch;
        public RafSystemFeatureInfo RAFSystem;
        public List<GameRuleValuePair> GameRuleValues = new();

        public struct SessionAlertConfig
        {
            public int Delay;
            public int Period;
            public int DisplayTime;
        }

        public struct SocialQueueConfig
        {
            public bool ToastsDisabled;
            public float ToastDuration;
            public float DelayDuration;
            public float QueueMultiplier;
            public float PlayerMultiplier;
            public float PlayerFriendValue;
            public float PlayerGuildValue;
            public float ThrottleInitialThreshold;
            public float ThrottleDecayTime;
            public float ThrottlePrioritySpike;
            public float ThrottleMinThreshold;
            public float ThrottlePvPPriorityNormal;
            public float ThrottlePvPPriorityLow;
            public float ThrottlePvPHonorThreshold;
            public float ThrottleLfgListPriorityDefault;
            public float ThrottleLfgListPriorityAbove;
            public float ThrottleLfgListPriorityBelow;
            public float ThrottleLfgListIlvlScalingAbove;
            public float ThrottleLfgListIlvlScalingBelow;
            public float ThrottleRfPriorityAbove;
            public float ThrottleRfIlvlScalingAbove;
            public float ThrottleDfMaxItemLevel;
            public float ThrottleDfBestPriority;
        }

        public struct SquelchInfo
        {
            public bool IsSquelched;
            public ObjectGuid BnetAccountGuid;
            public ObjectGuid GuildGuid;
        }

        public struct RafSystemFeatureInfo
        {
            public bool Enabled;
            public bool RecruitingEnabled;
            public uint MaxRecruits;
            public uint MaxRecruitMonths;
            public uint MaxRecruitmentUses;
            public uint DaysInCycle;
            public uint Unknown1007;
        }
    }

    public class FeatureSystemStatusGlueScreen : ServerPacket
    {
        public FeatureSystemStatusGlueScreen() : base(ServerOpcodes.FeatureSystemStatusGlueScreen) { }

        public override void Write()
        {
            _worldPacket.WriteBit(BpayStoreEnabled);
            _worldPacket.WriteBit(BpayStoreAvailable);
            _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
            _worldPacket.WriteBit(CharUndeleteEnabled);
            _worldPacket.WriteBit(CommerceSystemEnabled);
            _worldPacket.WriteBit(Unk14);
            _worldPacket.WriteBit(WillKickFromWorld);
            _worldPacket.WriteBit(IsExpansionPreorderInStore);

            _worldPacket.WriteBit(KioskModeEnabled);
            _worldPacket.WriteBit(CompetitiveModeEnabled);
            _worldPacket.WriteBit(false); // unused, 10.0.2
            _worldPacket.WriteBit(TrialBoostEnabled);
            _worldPacket.WriteBit(TokenBalanceEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterListEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterCopyEnabled);
            _worldPacket.WriteBit(LiveRegionAccountCopyEnabled);

            _worldPacket.WriteBit(LiveRegionKeyBindingsCopyEnabled);
            _worldPacket.WriteBit(Unknown901CheckoutRelated);
            _worldPacket.WriteBit(false); // unused, 10.0.2
            _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
            _worldPacket.WriteBit(false); // unused, 10.0.2
            _worldPacket.WriteBit(LaunchETA.HasValue);
            _worldPacket.WriteBit(AddonsDisabled);
            _worldPacket.WriteBit(Unused1000);

            _worldPacket.WriteBit(AccountSaveDataExportEnabled);
            _worldPacket.WriteBit(AccountLockedByExport);
            _worldPacket.WriteBit(!RealmHiddenAlert.IsEmpty());

            if (!RealmHiddenAlert.IsEmpty())
                _worldPacket.WriteBits(RealmHiddenAlert.GetByteCount() + 1, 11);

            _worldPacket.FlushBits();

            if (EuropaTicketSystemStatus.HasValue)
                EuropaTicketSystemStatus.Value.Write(_worldPacket);

            _worldPacket.WriteUInt32(TokenPollTimeSeconds);
            _worldPacket.WriteUInt32(KioskSessionMinutes);
            _worldPacket.WriteInt64(TokenBalanceAmount);
            _worldPacket.WriteInt32(MaxCharactersPerRealm);
            _worldPacket.WriteInt32(LiveRegionCharacterCopySourceRegions.Count);
            _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
            _worldPacket.WriteInt32(ActiveCharacterUpgradeBoostType);
            _worldPacket.WriteInt32(ActiveClassTrialBoostType);
            _worldPacket.WriteInt32(MinimumExpansionLevel);
            _worldPacket.WriteInt32(MaximumExpansionLevel);
            _worldPacket.WriteInt32(ActiveSeason);
            _worldPacket.WriteInt32(GameRuleValues.Count);
            _worldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
            _worldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
            _worldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);
            _worldPacket.WriteInt32(DebugTimeEvents.Count);
            _worldPacket.WriteInt32(Unused1007);

            if (LaunchETA.HasValue)
                _worldPacket.WriteInt32(LaunchETA.Value);

            if (!RealmHiddenAlert.IsEmpty())
                _worldPacket.WriteString(RealmHiddenAlert);

            foreach (var sourceRegion in LiveRegionCharacterCopySourceRegions)
                _worldPacket.WriteInt32(sourceRegion);

            foreach (GameRuleValuePair gameRuleValue in GameRuleValues)
                gameRuleValue.Write(_worldPacket);

            foreach (DebugTimeEventInfo debugTimeEventInfo in DebugTimeEvents)
                debugTimeEventInfo.Write(_worldPacket);
        }

        public bool BpayStoreAvailable; // NYI
        public bool BpayStoreDisabledByParentalControls; // NYI
        public bool CharUndeleteEnabled;
        public bool BpayStoreEnabled; // NYI
        public bool CommerceSystemEnabled; // NYI
        public bool Unk14; // NYI
        public bool WillKickFromWorld; // NYI
        public bool IsExpansionPreorderInStore; // NYI
        public bool KioskModeEnabled; // NYI
        public bool CompetitiveModeEnabled; // NYI
        public bool TrialBoostEnabled; // NYI
        public bool TokenBalanceEnabled; // NYI
        public bool LiveRegionCharacterListEnabled; // NYI
        public bool LiveRegionCharacterCopyEnabled; // NYI
        public bool LiveRegionAccountCopyEnabled; // NYI
        public bool LiveRegionKeyBindingsCopyEnabled;
        public bool Unknown901CheckoutRelated; // NYI
        public bool AddonsDisabled;
        public bool Unused1000;
        public bool AccountSaveDataExportEnabled;
        public bool AccountLockedByExport;
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public List<int> LiveRegionCharacterCopySourceRegions = new();
        public uint TokenPollTimeSeconds;     // NYI
        public long TokenBalanceAmount;     // NYI 
        public int MaxCharactersPerRealm;
        public uint BpayStoreProductDeliveryDelay;     // NYI
        public int ActiveCharacterUpgradeBoostType;     // NYI
        public int ActiveClassTrialBoostType;     // NYI
        public int MinimumExpansionLevel;
        public int MaximumExpansionLevel;
        public uint KioskSessionMinutes;
        public int ActiveSeason; // Currently active Classic season
        public List<GameRuleValuePair> GameRuleValues = new();
        public short MaxPlayerNameQueriesPerPacket = 50;
        public short PlayerNameQueryTelemetryInterval = 600;
        public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
        public int? LaunchETA;
        public List<DebugTimeEventInfo> DebugTimeEvents = new();
        public int Unused1007;
        public string RealmHiddenAlert;
    }

    public class MOTD : ServerPacket
    {
        public MOTD() : base(ServerOpcodes.Motd) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Text.Count, 4);
            _worldPacket.FlushBits();

            foreach (var line in Text)
            {
                _worldPacket.WriteBits(line.GetByteCount(), 7);
                _worldPacket.FlushBits();
                _worldPacket.WriteString(line);
            }
        }

        public List<string> Text;
    }

    public class SetTimeZoneInformation : ServerPacket
    {
        public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation) { }

        public override void Write()
        {
            _worldPacket.WriteBits(ServerTimeTZ.GetByteCount(), 7);
            _worldPacket.WriteBits(GameTimeTZ.GetByteCount(), 7);
            _worldPacket.WriteBits(ServerRegionalTZ.GetByteCount(), 7);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(ServerTimeTZ);
            _worldPacket.WriteString(GameTimeTZ);
            _worldPacket.WriteString(ServerRegionalTZ);
        }

        public string ServerTimeTZ;
        public string GameTimeTZ;
        public string ServerRegionalTZ;
    }

    public struct SavedThrottleObjectState
    {
        public uint MaxTries;
        public uint PerMilliseconds;
        public uint TryCount;
        public uint LastResetTimeBeforeNow;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(MaxTries);
            data.WriteUInt32(PerMilliseconds);
            data.WriteUInt32(TryCount);
            data.WriteUInt32(LastResetTimeBeforeNow);
        }
    }

    public struct EuropaTicketConfig
    {
        public bool TicketsEnabled;
        public bool BugsEnabled;
        public bool ComplaintsEnabled;
        public bool SuggestionsEnabled;

        public SavedThrottleObjectState ThrottleState;

        public void Write(WorldPacket data)
        {
            data.WriteBit(TicketsEnabled);
            data.WriteBit(BugsEnabled);
            data.WriteBit(ComplaintsEnabled);
            data.WriteBit(SuggestionsEnabled);

            ThrottleState.Write(data);
        }
    }

    public struct GameRuleValuePair
    {
        public int Rule;
        public int Value;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Rule);
            data.WriteInt32(Value);
        }
    }

    public struct DebugTimeEventInfo
    {
        public uint TimeEvent;
        public string Text;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TimeEvent);
            data.WriteBits(Text.GetByteCount(), 7);
            data.FlushBits();

            data.WriteString(Text);
        }
    }
}
