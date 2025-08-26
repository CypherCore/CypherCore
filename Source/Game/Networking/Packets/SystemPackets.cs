// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

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
            _worldPacket.WriteUInt32(RAFSystem.RewardsVersion);

            _worldPacket.WriteUInt32(CommercePricePollTimeSeconds);
            _worldPacket.WriteUInt32(KioskSessionDurationMinutes);
            _worldPacket.WriteInt64(RedeemForBalanceAmount);

            _worldPacket.WriteUInt32(ClubsPresenceDelay);
            _worldPacket.WriteUInt32(ClubPresenceUnsubscribeDelay);

            _worldPacket.WriteInt32(ContentSetID);
            _worldPacket.WriteInt32(GameRules.Count);
            _worldPacket.WriteInt32(ActiveTimerunningSeasonID);
            _worldPacket.WriteInt32(RemainingTimerunningSeasonSeconds);

            _worldPacket.WriteInt16(MaxPlayerGuidLookupsPerRequest);
            _worldPacket.WriteInt16(NameLookupTelemetryInterval);
            _worldPacket.WriteUInt32((uint)NotFoundCacheTimeSeconds.TotalSeconds);

            _worldPacket.WriteUInt32(RealmPvpTypeOverride);

            _worldPacket.WriteInt32(AddonChatThrottle.MaxTries);
            _worldPacket.WriteInt32(AddonChatThrottle.TriesRestoredPerSecond);
            _worldPacket.WriteInt32(AddonChatThrottle.UsedTriesPerMessage);

            _worldPacket.WriteFloat(AddonPerformanceMsgWarning);
            _worldPacket.WriteFloat(AddonPerformanceMsgError);
            _worldPacket.WriteFloat(AddonPerformanceMsgOverall);

            foreach (GameRuleValuePair gameRuleValue in GameRules)
                gameRuleValue.Write(_worldPacket);

            _worldPacket.WriteBit(VoiceEnabled);
            _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
            _worldPacket.WriteBit(BpayStoreAvailable);
            _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
            _worldPacket.WriteBit(ItemRestorationButtonEnabled);
            _worldPacket.WriteBit(SessionAlert.HasValue);
            _worldPacket.WriteBit(RAFSystem.Enabled);
            _worldPacket.WriteBit(RAFSystem.RecruitingEnabled);

            _worldPacket.WriteBit(CharUndeleteEnabled);
            _worldPacket.WriteBit(RestrictedAccount);
            _worldPacket.WriteBit(CommerceServerEnabled);
            _worldPacket.WriteBit(TutorialEnabled);
            _worldPacket.WriteBit(VeteranTokenRedeemWillKick);
            _worldPacket.WriteBit(WorldTokenRedeemWillKick);
            _worldPacket.WriteBit(KioskModeEnabled);
            _worldPacket.WriteBit(CompetitiveModeEnabled);

            _worldPacket.WriteBit(RedeemForBalanceAvailable);
            _worldPacket.WriteBit(WarModeEnabled);
            _worldPacket.WriteBit(CommunitiesEnabled);
            _worldPacket.WriteBit(BnetGroupsEnabled);
            _worldPacket.WriteBit(CharacterCommunitiesEnabled);
            _worldPacket.WriteBit(ClubPresenceAllowSubscribeAll);
            _worldPacket.WriteBit(VoiceChatParentalDisabled);
            _worldPacket.WriteBit(VoiceChatParentalMuted);

            _worldPacket.WriteBit(QuestSessionEnabled);
            _worldPacket.WriteBit(IsChatMuted);
            _worldPacket.WriteBit(ClubFinderEnabled);
            _worldPacket.WriteBit(CommunityFinderEnabled);
            _worldPacket.WriteBit(BrowserCrashReporterEnabled);
            _worldPacket.WriteBit(SpeakForMeAllowed);
            _worldPacket.WriteBit(DoesAccountNeedAADCPrompt);
            _worldPacket.WriteBit(IsAccountOptedInToAADC);

            _worldPacket.WriteBit(LfgRequireAuthenticatorEnabled);
            _worldPacket.WriteBit(ScriptsDisallowedForBeta);
            _worldPacket.WriteBit(TimerunningEnabled);
            _worldPacket.WriteBit(WarGamesEnabled);
            _worldPacket.WriteBit(IsPlayerContentTrackingEnabled);
            _worldPacket.WriteBit(SellAllJunkEnabled);
            _worldPacket.WriteBit(GroupFinderEnabled);
            _worldPacket.WriteBit(IsPremadeGroupEnabled);

            _worldPacket.WriteBit(false); // unused 10.2.7
            _worldPacket.WriteBit(GuildEventsEditsEnabled);
            _worldPacket.WriteBit(GuildTradeSkillsEnabled);
            _worldPacket.WriteBits(Unknown1027.GetByteCount(), 10);
            _worldPacket.WriteBit(BNSendWhisperUseV2Services);
            _worldPacket.WriteBit(BNSendGameDataUseV2Services);
            _worldPacket.WriteBit(IsAccountCurrencyTransferEnabled);

            _worldPacket.WriteBit(false); // unused 11.0.7
            _worldPacket.WriteBit(LobbyMatchmakerQueueFromMainlineEnabled);
            _worldPacket.WriteBit(CanSendLobbyMatchmakerPartyCustomizations);
            _worldPacket.WriteBit(AddonProfilerEnabled);
            _worldPacket.WriteBit(false); // unused 11.1.7
            _worldPacket.WriteBit(false); // unused 11.1.7

            _worldPacket.FlushBits();

            QuickJoinConfig.Write(_worldPacket);

            if (SessionAlert.HasValue)
            {
                _worldPacket.WriteInt32(SessionAlert.Value.Delay);
                _worldPacket.WriteInt32(SessionAlert.Value.Period);
                _worldPacket.WriteInt32(SessionAlert.Value.DisplayTime);
            }

            _worldPacket.WriteString(Unknown1027);

            _worldPacket.WriteBit(Squelch.IsSquelched);
            _worldPacket.WritePackedGuid(Squelch.BnetAccountGuid);
            _worldPacket.WritePackedGuid(Squelch.GuildGuid);

            if (EuropaTicketSystemStatus.HasValue)
                EuropaTicketSystemStatus.Value.Write(_worldPacket);
        }

        public bool VoiceEnabled;
        public bool BpayStoreAvailable;
        public SessionAlertConfig? SessionAlert;
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public uint CfgRealmID;
        public byte ComplaintStatus;
        public int CfgRealmRecID;
        public uint CommercePricePollTimeSeconds;
        public long RedeemForBalanceAmount;
        public uint ClubsPresenceDelay;
        public uint ClubPresenceUnsubscribeDelay; // Timer for updating club presence when communities ui frame is hidden
        public uint KioskSessionDurationMinutes;
        public int ContentSetID; // Currently active Classic season
        public short MaxPlayerGuidLookupsPerRequest = 50;
        public short NameLookupTelemetryInterval = 600;
        public TimeSpan NotFoundCacheTimeSeconds = TimeSpan.FromSeconds(10);
        public bool ItemRestorationButtonEnabled;
        public bool CharUndeleteEnabled; // Implemented
        public bool BpayStoreDisabledByParentalControls;
        public bool CommerceServerEnabled;
        public bool VeteranTokenRedeemWillKick;
        public bool WorldTokenRedeemWillKick;
        public bool RestrictedAccount;
        public bool TutorialEnabled;
        public bool NPETutorialsEnabled;
        public bool KioskModeEnabled;
        public bool CompetitiveModeEnabled;
        public bool RedeemForBalanceAvailable;
        public bool WarModeEnabled;
        public bool CommunitiesEnabled;
        public bool BnetGroupsEnabled;
        public bool CharacterCommunitiesEnabled;
        public bool ClubPresenceAllowSubscribeAll;
        public bool VoiceChatParentalDisabled;
        public bool VoiceChatParentalMuted;
        public bool QuestSessionEnabled;
        public bool IsChatMuted;
        public bool ClubFinderEnabled;
        public bool CommunityFinderEnabled;
        public bool BrowserCrashReporterEnabled;
        public bool SpeakForMeAllowed;
        public bool DoesAccountNeedAADCPrompt;
        public bool IsAccountOptedInToAADC;
        public bool LfgRequireAuthenticatorEnabled;
        public bool ScriptsDisallowedForBeta;
        public bool TimerunningEnabled;
        public bool WarGamesEnabled; // classic only
        public bool IsPlayerContentTrackingEnabled;
        public bool SellAllJunkEnabled;
        public bool GroupFinderEnabled = true;  // classic only
        public bool IsPremadeGroupEnabled = true;  // classic only
        public bool GuildEventsEditsEnabled = true;
        public bool GuildTradeSkillsEnabled = true;
        public bool BNSendWhisperUseV2Services = true; ///< BNSendWhisper will send to v2.WhisperService instead of v1.NotificationService
        public bool BNSendGameDataUseV2Services = true; ///< BNSendGameData will send to v2.NotificationService instead of v1.NotificationService
        public bool IsAccountCurrencyTransferEnabled;
        public bool LobbyMatchmakerQueueFromMainlineEnabled;
        public bool CanSendLobbyMatchmakerPartyCustomizations;
        public bool AddonProfilerEnabled;

        public SocialQueueConfig QuickJoinConfig;
        public SquelchInfo Squelch;
        public RafSystemFeatureInfo RAFSystem;
        public List<GameRuleValuePair> GameRules = new();
        public int ActiveTimerunningSeasonID;
        public int RemainingTimerunningSeasonSeconds;
        public string Unknown1027;                          // related to movement lua functions used by keybinds
        public AddonChatThrottleParams AddonChatThrottle;
        public uint RealmPvpTypeOverride;       ///< Use Cfg_Configs value = 0, ForceEnabled = 1, ForceDisabled = 2
        public float AddonPerformanceMsgWarning;
        public float AddonPerformanceMsgError;
        public float AddonPerformanceMsgOverall;

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

            public void Write(WorldPacket data)
            {
                data.WriteFloat(ToastDuration);
                data.WriteFloat(DelayDuration);
                data.WriteFloat(QueueMultiplier);
                data.WriteFloat(PlayerMultiplier);
                data.WriteFloat(PlayerFriendValue);
                data.WriteFloat(PlayerGuildValue);
                data.WriteFloat(ThrottleInitialThreshold);
                data.WriteFloat(ThrottleDecayTime);
                data.WriteFloat(ThrottlePrioritySpike);
                data.WriteFloat(ThrottleMinThreshold);
                data.WriteFloat(ThrottlePvPPriorityNormal);
                data.WriteFloat(ThrottlePvPPriorityLow);
                data.WriteFloat(ThrottlePvPHonorThreshold);
                data.WriteFloat(ThrottleLfgListPriorityDefault);
                data.WriteFloat(ThrottleLfgListPriorityAbove);
                data.WriteFloat(ThrottleLfgListPriorityBelow);
                data.WriteFloat(ThrottleLfgListIlvlScalingAbove);
                data.WriteFloat(ThrottleLfgListIlvlScalingBelow);
                data.WriteFloat(ThrottleRfPriorityAbove);
                data.WriteFloat(ThrottleRfIlvlScalingAbove);
                data.WriteFloat(ThrottleDfMaxItemLevel);
                data.WriteFloat(ThrottleDfBestPriority);
                data.WriteBit(ToastsDisabled);
                data.FlushBits();
            }
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
            public uint RewardsVersion;
        }

        public struct AddonChatThrottleParams
        {
            public int MaxTries;
            public int TriesRestoredPerSecond;
            public int UsedTriesPerMessage;
        }
    }

    public class FeatureSystemStatusGlueScreen : ServerPacket
    {
        public FeatureSystemStatusGlueScreen() : base(ServerOpcodes.FeatureSystemStatusGlueScreen) { }

        public override void Write()
        {
            _worldPacket.WriteBit(BpayStoreAvailable);
            _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
            _worldPacket.WriteBit(CharUndeleteEnabled);
            _worldPacket.WriteBit(CommerceServerEnabled);
            _worldPacket.WriteBit(VeteranTokenRedeemWillKick);
            _worldPacket.WriteBit(WorldTokenRedeemWillKick);
            _worldPacket.WriteBit(ExpansionPreorderInStore);
            _worldPacket.WriteBit(KioskModeEnabled);

            _worldPacket.WriteBit(CompetitiveModeEnabled);
            _worldPacket.WriteBit(BoostEnabled);
            _worldPacket.WriteBit(TrialBoostEnabled);
            _worldPacket.WriteBit(RedeemForBalanceAvailable);
            _worldPacket.WriteBit(PaidCharacterTransfersBetweenBnetAccountsEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterListEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterCopyEnabled);
            _worldPacket.WriteBit(LiveRegionAccountCopyEnabled);

            _worldPacket.WriteBit(LiveRegionKeyBindingsCopyEnabled);
            _worldPacket.WriteBit(BrowserCrashReporterEnabled);
            _worldPacket.WriteBit(IsEmployeeAccount);
            _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
            _worldPacket.WriteBit(NameReservationOnly);
            _worldPacket.WriteBit(LaunchDurationETA.HasValue);
            _worldPacket.WriteBit(TimerunningEnabled);
            _worldPacket.WriteBit(ScriptsDisallowedForBeta);

            _worldPacket.WriteBit(PlayerIdentityOptionsEnabled);
            _worldPacket.WriteBit(AccountExportEnabled);
            _worldPacket.WriteBit(AccountLockedPostExport);
            _worldPacket.WriteBits(RealmHiddenAlert.GetByteCount() + 1, 11);

            _worldPacket.WriteBit(BNSendWhisperUseV2Services);
            _worldPacket.WriteBit(BNSendGameDataUseV2Services);

            _worldPacket.WriteBit(CharacterSelectListModeRealmless);
            _worldPacket.WriteBit(WowTokenLimitedMode);
            _worldPacket.WriteBit(false); // unused 11.1.7
            _worldPacket.WriteBit(false); // unused 11.1.7
            _worldPacket.WriteBit(PandarenLevelBoostAllowed);

            _worldPacket.FlushBits();

            if (EuropaTicketSystemStatus.HasValue)
                EuropaTicketSystemStatus.Value.Write(_worldPacket);

            _worldPacket.WriteUInt32(CommercePricePollTimeSeconds);
            _worldPacket.WriteUInt32(KioskSessionDurationMinutes);
            _worldPacket.WriteInt64(RedeemForBalanceAmount);
            _worldPacket.WriteInt32(MaxCharactersOnThisRealm);
            _worldPacket.WriteInt32(LiveRegionCharacterCopySourceRegions.Count);
            _worldPacket.WriteInt32(ActiveBoostType);
            _worldPacket.WriteInt32(TrialBoostType);
            _worldPacket.WriteInt32(MinimumExpansionLevel);
            _worldPacket.WriteInt32(MaximumExpansionLevel);
            _worldPacket.WriteInt32(ContentSetID);
            _worldPacket.WriteInt32(GameRules.Count);
            _worldPacket.WriteInt32(ActiveTimerunningSeasonID);
            _worldPacket.WriteInt32(RemainingTimerunningSeasonSeconds);
            _worldPacket.WriteInt16(MaxPlayerGuidLookupsPerRequest);
            _worldPacket.WriteInt16(NameLookupTelemetryInterval);
            _worldPacket.WriteUInt32((uint)NotFoundCacheTimeSeconds.TotalSeconds);
            _worldPacket.WriteInt32(DebugTimeEvents.Count);
            _worldPacket.WriteInt32(MostRecentTimeEventID);
            _worldPacket.WriteUInt32(EventRealmQueues);

            if (LaunchDurationETA.HasValue)
                _worldPacket.WriteInt32(LaunchDurationETA.Value);

            if (!RealmHiddenAlert.IsEmpty())
                _worldPacket.WriteString(RealmHiddenAlert);

            foreach (var sourceRegion in LiveRegionCharacterCopySourceRegions)
                _worldPacket.WriteInt32(sourceRegion);

            foreach (GameRuleValuePair gameRuleValue in GameRules)
                gameRuleValue.Write(_worldPacket);

            foreach (DebugTimeEventInfo debugTimeEventInfo in DebugTimeEvents)
                debugTimeEventInfo.Write(_worldPacket);
        }

        public bool BpayStoreAvailable; // NYI
        public bool BpayStoreDisabledByParentalControls; // NYI
        public bool CharUndeleteEnabled;
        public bool CommerceServerEnabled; // NYI
        public bool VeteranTokenRedeemWillKick; // NYI
        public bool WorldTokenRedeemWillKick; // NYI
        public bool ExpansionPreorderInStore; // NYI
        public bool KioskModeEnabled; // NYI
        public bool CompetitiveModeEnabled; // NYI
        public bool BoostEnabled; // classic only
        public bool TrialBoostEnabled; // NYI
        public bool RedeemForBalanceAvailable; // NYI
        public bool PaidCharacterTransfersBetweenBnetAccountsEnabled;
        public bool LiveRegionCharacterListEnabled; // NYI
        public bool LiveRegionCharacterCopyEnabled; // NYI
        public bool LiveRegionAccountCopyEnabled; // NYI
        public bool LiveRegionKeyBindingsCopyEnabled;
        public bool BrowserCrashReporterEnabled; // NYI
        public bool IsEmployeeAccount; // NYI
        public bool NameReservationOnly; // classic only
        public bool TimerunningEnabled; // NYI
        public bool ScriptsDisallowedForBeta;
        public bool PlayerIdentityOptionsEnabled;
        public bool AccountExportEnabled;
        public bool AccountLockedPostExport;
        public bool BNSendWhisperUseV2Services = true; ///< BNSendWhisper will send to v2.WhisperService instead of v1.NotificationService
        public bool BNSendGameDataUseV2Services = true; ///< BNSendGameData will send to v2.NotificationService instead of v1.NotificationService
        public bool CharacterSelectListModeRealmless;
        public bool WowTokenLimitedMode; // classic only
        public bool PandarenLevelBoostAllowed; // classic only
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public List<int> LiveRegionCharacterCopySourceRegions = new();
        public uint CommercePricePollTimeSeconds;     // NYI
        public long RedeemForBalanceAmount;     // NYI 
        public int MaxCharactersOnThisRealm;
        public int ActiveBoostType;     // NYI
        public int TrialBoostType;     // NYI
        public int MinimumExpansionLevel;
        public int MaximumExpansionLevel;
        public uint KioskSessionDurationMinutes;
        public int ContentSetID; // Currently active Classic season
        public List<GameRuleValuePair> GameRules = new();
        public int ActiveTimerunningSeasonID;
        public int RemainingTimerunningSeasonSeconds;
        public short MaxPlayerGuidLookupsPerRequest = 50;
        public short NameLookupTelemetryInterval = 600;
        public TimeSpan NotFoundCacheTimeSeconds = TimeSpan.FromSeconds(10);
        public int? LaunchDurationETA;
        public List<DebugTimeEventInfo> DebugTimeEvents = new();
        public int MostRecentTimeEventID;
        public uint EventRealmQueues;
        public string RealmHiddenAlert;
    }

    public class SetTimeZoneInformation : ServerPacket
    {
        public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation) { }

        public override void Write()
        {
            _worldPacket.WriteBits(ServerTimeTZ.GetByteCount(), 7);
            _worldPacket.WriteBits(GameTimeTZ.GetByteCount(), 7);
            _worldPacket.WriteBits(ServerRegionalTimeTZ.GetByteCount(), 7);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(ServerTimeTZ);
            _worldPacket.WriteString(GameTimeTZ);
            _worldPacket.WriteString(ServerRegionalTimeTZ);
        }

        public string ServerTimeTZ;
        public string GameTimeTZ;
        public string ServerRegionalTimeTZ;
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
        public float ValueF;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Rule);
            data.WriteInt32(Value);
            data.WriteFloat(ValueF);
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
