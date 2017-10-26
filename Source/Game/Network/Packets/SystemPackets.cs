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

using Framework.Constants;
using Framework.Dynamic;
using System.Collections.Generic;

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
            _worldPacket.WriteBit(TutorialsEnabled);
            _worldPacket.WriteBit(NPETutorialsEnabled);
            _worldPacket.WriteBit(TwitterEnabled);
            _worldPacket.WriteBit(CommerceSystemEnabled);
            _worldPacket.WriteBit(Unk67);
            _worldPacket.WriteBit(WillKickFromWorld);
            _worldPacket.WriteBit(KioskModeEnabled);
            _worldPacket.WriteBit(CompetitiveModeEnabled);
            _worldPacket.WriteBit(RaceClassExpansionLevels.HasValue);
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

        public bool VoiceEnabled { get; set; }
        public bool BrowserEnabled { get; set; }
        public bool BpayStoreAvailable { get; set; }
        public bool RecruitAFriendSendingEnabled { get; set; }
        public bool BpayStoreEnabled { get; set; }
        public Optional<SessionAlertConfig> SessionAlert { get; set; }
        public uint ScrollOfResurrectionMaxRequestsPerDay { get; set; }
        public bool ScrollOfResurrectionEnabled { get; set; }
        public Optional<EuropaTicketConfig> EuropaTicketSystemStatus;
        public uint ScrollOfResurrectionRequestsRemaining { get; set; }
        public uint CfgRealmID { get; set; }
        public byte ComplaintStatus { get; set; }
        public int CfgRealmRecID { get; set; }
        public uint TwitterPostThrottleLimit { get; set; }
        public uint TwitterPostThrottleCooldown { get; set; }
        public uint TokenPollTimeSeconds { get; set; }
        public uint TokenRedeemIndex { get; set; }
        public long TokenBalanceAmount { get; set; }
        public bool ItemRestorationButtonEnabled { get; set; }
        public bool CharUndeleteEnabled; // Implemented
        public bool BpayStoreDisabledByParentalControls { get; set; }
        public bool TwitterEnabled { get; set; }
        public bool CommerceSystemEnabled { get; set; }
        public bool Unk67 { get; set; }
        public bool WillKickFromWorld { get; set; }

        public bool RestrictedAccount { get; set; }
        public bool TutorialsEnabled { get; set; }
        public bool NPETutorialsEnabled { get; set; }
        public bool KioskModeEnabled { get; set; }
        public bool CompetitiveModeEnabled { get; set; }
        public bool TokenBalanceEnabled { get; set; }

        public Optional<List<byte>> RaceClassExpansionLevels { get; set; }
        public SocialQueueConfig QuickJoinConfig { get; set; }

        public struct SavedThrottleObjectState
        {
            public uint MaxTries { get; set; }
            public uint PerMilliseconds { get; set; }
            public uint TryCount { get; set; }
            public uint LastResetTimeBeforeNow { get; set; }
        }

        public struct EuropaTicketConfig
        {
            public bool TicketsEnabled { get; set; }
            public bool BugsEnabled { get; set; }
            public bool ComplaintsEnabled { get; set; }
            public bool SuggestionsEnabled { get; set; }

            public SavedThrottleObjectState ThrottleState;
        }

        public struct SessionAlertConfig
        {
            public int Delay { get; set; }
            public int Period { get; set; }
            public int DisplayTime { get; set; }
        }

        public struct SocialQueueConfig
        {
            public bool ToastsDisabled { get; set; }
            public float ToastDuration { get; set; }
            public float DelayDuration { get; set; }
            public float QueueMultiplier { get; set; }
            public float PlayerMultiplier { get; set; }
            public float PlayerFriendValue { get; set; }
            public float PlayerGuildValue { get; set; }
            public float ThrottleInitialThreshold { get; set; }
            public float ThrottleDecayTime { get; set; }
            public float ThrottlePrioritySpike { get; set; }
            public float ThrottleMinThreshold { get; set; }
            public float ThrottlePvPPriorityNormal { get; set; }
            public float ThrottlePvPPriorityLow { get; set; }
            public float ThrottlePvPHonorThreshold { get; set; }
            public float ThrottleLfgListPriorityDefault { get; set; }
            public float ThrottleLfgListPriorityAbove { get; set; }
            public float ThrottleLfgListPriorityBelow { get; set; }
            public float ThrottleLfgListIlvlScalingAbove { get; set; }
            public float ThrottleLfgListIlvlScalingBelow { get; set; }
            public float ThrottleRfPriorityAbove { get; set; }
            public float ThrottleRfIlvlScalingAbove { get; set; }
            public float ThrottleDfMaxItemLevel { get; set; }
            public float ThrottleDfBestPriority { get; set; }
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
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(TokenPollTimeSeconds);
            _worldPacket.WriteInt32(TokenRedeemIndex);
            _worldPacket.WriteInt64(TokenBalanceAmount);
        }

        public bool BpayStoreAvailable; // NYI
        public bool BpayStoreDisabledByParentalControls; // NYI
        public bool CharUndeleteEnabled { get; set; }
        public bool BpayStoreEnabled; // NYI
        public bool CommerceSystemEnabled; // NYI
        public bool Unk14; // NYI
        public bool WillKickFromWorld; // NYI
        public bool IsExpansionPreorderInStore; // NYI
        public bool KioskModeEnabled; // NYI
        public bool CompetitiveModeEnabled; // NYI
        public bool TrialBoostEnabled; // NYI
        public bool TokenBalanceEnabled; // NYI
        public int TokenPollTimeSeconds;     // NYI
        public int TokenRedeemIndex;     // NYI
        public long TokenBalanceAmount;     // NYI
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
                _worldPacket.WriteBits(line.Length, 7);
                _worldPacket.FlushBits();
                _worldPacket.WriteString(line);
            }
        }

        public List<string> Text { get; set; }
    }

    public class SetTimeZoneInformation : ServerPacket
    {
        public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation) { }

        public override void Write()
        {
            _worldPacket.WriteBits(ServerTimeTZ.Length, 7);
            _worldPacket.WriteBits(GameTimeTZ.Length, 7);
            _worldPacket.WriteString(ServerTimeTZ);
            _worldPacket.WriteString(GameTimeTZ);
        }

        public string ServerTimeTZ { get; set; }
        public string GameTimeTZ { get; set; }
    }
}
