// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class FeatureSystemStatus : ServerPacket
    {
        public int ActiveSeason; // Currently active Classic season
        public bool AddonsDisabled;
        public bool BpayStoreAvailable;
        public bool BpayStoreDisabledByParentalControls;
        public bool BpayStoreEnabled;
        public uint BpayStoreProductDeliveryDelay;
        public bool BrowserEnabled;
        public uint CfgRealmID;
        public int CfgRealmRecID;
        public bool CharUndeleteEnabled; // Implemented
        public bool ChatDisabledByDefault;
        public bool ChatDisabledByPlayer;
        public bool ClubFinderEnabled;
        public bool ClubsBattleNetClubTypeAllowed;
        public bool ClubsCharacterClubTypeAllowed;
        public bool ClubsEnabled;
        public bool ClubsPresenceUpdateEnabled;
        public uint ClubsPresenceUpdateTimer;
        public bool CommerceSystemEnabled;
        public bool CompetitiveModeEnabled;
        public byte ComplaintStatus;
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public List<GameRuleValuePair> GameRuleValues = new();
        public uint HiddenUIClubsPresenceUpdateTimer; // Timer for updating club presence when communities ui frame is hidden
        public bool IsMuted;
        public bool ItemRestorationButtonEnabled;
        public bool KioskModeEnabled;
        public uint KioskSessionMinutes;
        public bool LFGListCustomRequiresAuthenticator;
        public short MaxPlayerNameQueriesPerPacket = 50;
        public bool NPETutorialsEnabled;
        public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
        public short PlayerNameQueryTelemetryInterval = 600;
        public bool QuestSessionEnabled;

        public SocialQueueConfig QuickJoinConfig;
        public RafSystemFeatureInfo RAFSystem;
        public bool RestrictedAccount;
        public bool ScrollOfResurrectionEnabled;
        public uint ScrollOfResurrectionMaxRequestsPerDay;
        public uint ScrollOfResurrectionRequestsRemaining;
        public SessionAlertConfig? SessionAlert;
        public SquelchInfo Squelch;
        public bool TextToSpeechFeatureEnabled;
        public long TokenBalanceAmount;
        public bool TokenBalanceEnabled;
        public uint TokenPollTimeSeconds;
        public bool TutorialsEnabled;
        public bool TwitterEnabled;
        public uint TwitterPostThrottleCooldown;
        public uint TwitterPostThrottleLimit;
        public bool Unk67;
        public bool Unknown901CheckoutRelated;
        public bool Unused1000;
        public bool VoiceChatDisabledByParentalControl;
        public bool VoiceChatMutedByParentalControl;

        public bool VoiceEnabled;
        public bool WarModeFeatureEnabled;
        public bool WillKickFromWorld;

        public FeatureSystemStatus() : base(ServerOpcodes.FeatureSystemStatus)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(ComplaintStatus);

            _worldPacket.WriteUInt32(ScrollOfResurrectionRequestsRemaining);
            _worldPacket.WriteUInt32(ScrollOfResurrectionMaxRequestsPerDay);

            _worldPacket.WriteUInt32(CfgRealmID);
            _worldPacket.WriteInt32(CfgRealmRecID);

            _worldPacket.WriteUInt32(RAFSystem.MaxRecruits);
            _worldPacket.WriteUInt32(RAFSystem.MaxRecruitMonths);
            _worldPacket.WriteUInt32(RAFSystem.MaxRecruitmentUses);
            _worldPacket.WriteUInt32(RAFSystem.DaysInCycle);

            _worldPacket.WriteUInt32(TwitterPostThrottleLimit);
            _worldPacket.WriteUInt32(TwitterPostThrottleCooldown);

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
            _worldPacket.WriteBit(ScrollOfResurrectionEnabled);
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
            _worldPacket.WriteBit(TwitterEnabled);

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
        }
    }

    public class FeatureSystemStatusGlueScreen : ServerPacket
    {
        public int ActiveCharacterUpgradeBoostType; // NYI
        public int ActiveClassTrialBoostType;       // NYI
        public int ActiveSeason;                    // Currently active Classic season
        public bool AddonsDisabled;

        public bool BpayStoreAvailable;                  // NYI
        public bool BpayStoreDisabledByParentalControls; // NYI
        public bool BpayStoreEnabled;                    // NYI
        public uint BpayStoreProductDeliveryDelay;       // NYI
        public bool CharUndeleteEnabled;
        public bool CommerceSystemEnabled;  // NYI
        public bool CompetitiveModeEnabled; // NYI
        public EuropaTicketConfig? EuropaTicketSystemStatus;
        public List<GameRuleValuePair> GameRuleValues = new();
        public bool IsExpansionPreorderInStore; // NYI
        public bool KioskModeEnabled;           // NYI
        public uint KioskSessionMinutes;
        public int? LaunchETA;
        public bool LiveRegionAccountCopyEnabled;   // NYI
        public bool LiveRegionCharacterCopyEnabled; // NYI
        public List<int> LiveRegionCharacterCopySourceRegions = new();
        public bool LiveRegionCharacterListEnabled; // NYI
        public bool LiveRegionKeyBindingsCopyEnabled;
        public int MaxCharactersPerRealm;
        public int MaximumExpansionLevel;
        public short MaxPlayerNameQueriesPerPacket = 50;
        public int MinimumExpansionLevel;
        public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
        public short PlayerNameQueryTelemetryInterval = 600;
        public long TokenBalanceAmount;        // NYI 
        public bool TokenBalanceEnabled;       // NYI
        public uint TokenPollTimeSeconds;      // NYI
        public bool TrialBoostEnabled;         // NYI
        public bool Unk14;                     // NYI
        public bool Unknown901CheckoutRelated; // NYI
        public bool Unused1000;
        public bool WillKickFromWorld; // NYI

        public FeatureSystemStatusGlueScreen() : base(ServerOpcodes.FeatureSystemStatusGlueScreen)
        {
        }

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

            if (LaunchETA.HasValue)
                _worldPacket.WriteInt32(LaunchETA.Value);

            foreach (var sourceRegion in LiveRegionCharacterCopySourceRegions)
                _worldPacket.WriteInt32(sourceRegion);

            foreach (GameRuleValuePair gameRuleValue in GameRuleValues)
                gameRuleValue.Write(_worldPacket);
        }
    }

    public class MOTD : ServerPacket
    {
        public List<string> Text;

        public MOTD() : base(ServerOpcodes.Motd)
        {
        }

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
    }

    public class SetTimeZoneInformation : ServerPacket
    {
        public string GameTimeTZ;
        public string ServerRegionalTZ;

        public string ServerTimeTZ;

        public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation)
        {
        }

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
}