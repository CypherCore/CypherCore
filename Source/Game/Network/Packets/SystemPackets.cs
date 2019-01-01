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
using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Network.Packets
{
    public class FeatureSystemStatus : ServerPacket
    {
        public FeatureSystemStatus() : base(ServerOpcodes.FeatureSystemStatus)
        {
            SessionAlert = new Optional<SessionAlertConfig>();
            EuropaTicketSystemStatus = new Optional<EuropaTicketConfig>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(ComplaintStatus);

            _worldPacket.WriteUInt32(ScrollOfResurrectionRequestsRemaining);
            _worldPacket.WriteUInt32(ScrollOfResurrectionMaxRequestsPerDay);

            _worldPacket.WriteUInt32(CfgRealmID);
            _worldPacket.WriteInt32(CfgRealmRecID);

            _worldPacket.WriteUInt32(TwitterPostThrottleLimit);
            _worldPacket.WriteUInt32(TwitterPostThrottleCooldown);

            _worldPacket.WriteUInt32(TokenPollTimeSeconds);
            _worldPacket.WriteUInt32(TokenRedeemIndex);
            _worldPacket.WriteInt64(TokenBalanceAmount);

            _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
            _worldPacket.WriteUInt32(ClubsPresenceUpdateTimer);

            _worldPacket.WriteBit(VoiceEnabled);
            _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
            _worldPacket.WriteBit(ScrollOfResurrectionEnabled);
            _worldPacket.WriteBit(BpayStoreEnabled);
            _worldPacket.WriteBit(BpayStoreAvailable);
            _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
            _worldPacket.WriteBit(ItemRestorationButtonEnabled);
            _worldPacket.WriteBit(BrowserEnabled);
            _worldPacket.WriteBit(SessionAlert.HasValue);
            _worldPacket.WriteBit(RecruitAFriendSendingEnabled);
            _worldPacket.WriteBit(CharUndeleteEnabled);
            _worldPacket.WriteBit(RestrictedAccount);
            _worldPacket.WriteBit(CommerceSystemEnabled);
            _worldPacket.WriteBit(TutorialsEnabled);
            _worldPacket.WriteBit(NPETutorialsEnabled);
            _worldPacket.WriteBit(TwitterEnabled);
            _worldPacket.WriteBit(Unk67);
            _worldPacket.WriteBit(WillKickFromWorld);
            _worldPacket.WriteBit(KioskModeEnabled);
            _worldPacket.WriteBit(CompetitiveModeEnabled);
            _worldPacket.WriteBit(RaceClassExpansionLevels.HasValue);
            _worldPacket.WriteBit(TokenBalanceEnabled);
            _worldPacket.WriteBit(WarModeFeatureEnabled);
            _worldPacket.WriteBit(ClubsEnabled);
            _worldPacket.WriteBit(ClubsBattleNetClubTypeAllowed);
            _worldPacket.WriteBit(ClubsCharacterClubTypeAllowed);
            _worldPacket.WriteBit(VoiceChatDisabledByParentalControl);
            _worldPacket.WriteBit(VoiceChatMutedByParentalControl);
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

            if (RaceClassExpansionLevels.HasValue)
            {
                _worldPacket.WriteUInt32(RaceClassExpansionLevels.Value.Count);
                foreach (var level in RaceClassExpansionLevels.Value)
                    _worldPacket.WriteUInt8(level);
            }

            _worldPacket.WriteBit(VoiceChatManagerSettings.Enabled);
            _worldPacket.WritePackedGuid(VoiceChatManagerSettings.BnetAccountGuid);
            _worldPacket.WritePackedGuid(VoiceChatManagerSettings.GuildGuid);

            if (EuropaTicketSystemStatus.HasValue)
            {
                _worldPacket.WriteBit(EuropaTicketSystemStatus.Value.TicketsEnabled);
                _worldPacket.WriteBit(EuropaTicketSystemStatus.Value.BugsEnabled);
                _worldPacket.WriteBit(EuropaTicketSystemStatus.Value.ComplaintsEnabled);
                _worldPacket.WriteBit(EuropaTicketSystemStatus.Value.SuggestionsEnabled);

                _worldPacket.WriteUInt32(EuropaTicketSystemStatus.Value.ThrottleState.MaxTries);
                _worldPacket.WriteUInt32(EuropaTicketSystemStatus.Value.ThrottleState.PerMilliseconds);
                _worldPacket.WriteUInt32(EuropaTicketSystemStatus.Value.ThrottleState.TryCount);
                _worldPacket.WriteUInt32(EuropaTicketSystemStatus.Value.ThrottleState.LastResetTimeBeforeNow);
            }
        }

        public bool VoiceEnabled;
        public bool BrowserEnabled;
        public bool BpayStoreAvailable;
        public bool RecruitAFriendSendingEnabled;
        public bool BpayStoreEnabled;
        public Optional<SessionAlertConfig> SessionAlert;
        public uint ScrollOfResurrectionMaxRequestsPerDay;
        public bool ScrollOfResurrectionEnabled;
        public Optional<EuropaTicketConfig> EuropaTicketSystemStatus;
        public uint ScrollOfResurrectionRequestsRemaining;
        public uint CfgRealmID;
        public byte ComplaintStatus;
        public int CfgRealmRecID;
        public uint TwitterPostThrottleLimit;
        public uint TwitterPostThrottleCooldown;
        public uint TokenPollTimeSeconds;
        public uint TokenRedeemIndex;
        public long TokenBalanceAmount;
        public uint BpayStoreProductDeliveryDelay;
        public uint ClubsPresenceUpdateTimer;
        public bool ItemRestorationButtonEnabled;
        public bool CharUndeleteEnabled; // Implemented
        public bool BpayStoreDisabledByParentalControls;
        public bool TwitterEnabled;
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
        public bool VoiceChatDisabledByParentalControl;
        public bool VoiceChatMutedByParentalControl;

        public Optional<List<byte>> RaceClassExpansionLevels;
        public SocialQueueConfig QuickJoinConfig;
        public VoiceChatProxySettings VoiceChatManagerSettings;

        public struct SavedThrottleObjectState
        {
            public uint MaxTries;
            public uint PerMilliseconds;
            public uint TryCount;
            public uint LastResetTimeBeforeNow;
        }

        public struct EuropaTicketConfig
        {
            public bool TicketsEnabled;
            public bool BugsEnabled;
            public bool ComplaintsEnabled;
            public bool SuggestionsEnabled;

            public SavedThrottleObjectState ThrottleState;
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

        public struct VoiceChatProxySettings
        {
            public bool Enabled;
            public ObjectGuid BnetAccountGuid;
            public ObjectGuid GuildGuid;
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
            _worldPacket.WriteBit(false); // not accessed in handler
            _worldPacket.WriteBit(TrialBoostEnabled);
            _worldPacket.WriteBit(TokenBalanceEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterListEnabled);
            _worldPacket.WriteBit(LiveRegionCharacterCopyEnabled);
            _worldPacket.WriteBit(LiveRegionAccountCopyEnabled);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(TokenPollTimeSeconds);
            _worldPacket.WriteInt32(TokenRedeemIndex);
            _worldPacket.WriteInt64(TokenBalanceAmount);
            _worldPacket.WriteInt32(MaxCharactersPerRealm);
            _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
            _worldPacket.WriteInt32(ActiveCharacterUpgradeBoostType);
            _worldPacket.WriteInt32(ActiveClassTrialBoostType);
            _worldPacket.WriteInt32(MinimumExpansionLevel);
            _worldPacket.WriteInt32(MaximumExpansionLevel);
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
        public int TokenPollTimeSeconds;     // NYI
        public int TokenRedeemIndex;     // NYI
        public long TokenBalanceAmount;     // NYI 
        public int MaxCharactersPerRealm;
        public uint BpayStoreProductDeliveryDelay;     // NYI
        public int ActiveCharacterUpgradeBoostType;     // NYI
        public int ActiveClassTrialBoostType;     // NYI
        public int MinimumExpansionLevel;
        public int MaximumExpansionLevel;
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
            _worldPacket.WriteString(ServerTimeTZ);
            _worldPacket.WriteString(GameTimeTZ);
        }

        public string ServerTimeTZ;
        public string GameTimeTZ;
    }
}
