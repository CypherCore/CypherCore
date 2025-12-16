// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework;
using Framework.Constants;
using Game.Networking.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
        {
            AuthResponse response = new();
            response.Result = code;

            if (code == BattlenetRpcErrorCode.Ok)
            {
                response.SuccessInfo = new();

                response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
                response.SuccessInfo.ActiveExpansionLevel = (byte)GetExpansion();
                response.SuccessInfo.AccountExpansionLevel = (byte)GetAccountExpansion();
                response.SuccessInfo.Time = (uint)GameTime.GetGameTime();

                // Send current home realm. Also there is no need to send it later in realm queries.
                var currentRealm = Global.RealmMgr.GetCurrentRealm();
                if (currentRealm != null)
                {
                    response.SuccessInfo.VirtualRealmAddress = currentRealm.Id.GetAddress();
                    response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(currentRealm.Id.GetAddress(), true, false, currentRealm.Name, currentRealm.NormalizedName));
                }

                if (HasPermission(RBACPermissions.UseCharacterTemplates))
                    foreach (var templ in Global.CharacterTemplateDataStorage.GetCharacterTemplates().Values)
                        response.SuccessInfo.Templates.Add(templ);

                response.SuccessInfo.AvailableClasses = Global.ObjectMgr.GetClassExpansionRequirements();
            }

            if (queued)
            {
                AuthWaitInfo waitInfo = new();
                waitInfo.WaitCount = queuePos;
                response.WaitInfo = waitInfo;
            }

            SendPacket(response);
        }

        public void SendAuthWaitQueue(uint position)
        {
            if (position != 0)
            {
                WaitQueueUpdate waitQueueUpdate = new();
                waitQueueUpdate.WaitInfo.WaitCount = position;
                waitQueueUpdate.WaitInfo.WaitTime = 0;
                waitQueueUpdate.WaitInfo.HasFCM = false;
                SendPacket(waitQueueUpdate);
            }
            else
                SendPacket(new WaitQueueFinish());
        }

        public void SendClientCacheVersion(uint version)
        {
            ClientCacheVersion cache = new();
            cache.CacheVersion = version;
            SendPacket(cache);//enabled it
        }

        public void SendSetTimeZoneInformation()
        {
            TimeSpan timezoneOffset = Timezone.GetSystemZoneOffset();
            string realTimezone = Timezone.GetSystemZoneName();
            string clientSupportedTZ = Timezone.FindClosestClientSupportedTimezone(realTimezone, timezoneOffset);

            SetTimeZoneInformation packet = new();
            packet.ServerTimeTZ = clientSupportedTZ;
            packet.GameTimeTZ = clientSupportedTZ;
            packet.ServerRegionalTimeTZ = clientSupportedTZ;

            SendPacket(packet);//enabled it
        }

        public void SendFeatureSystemStatusGlueScreen()
        {
            FeatureSystemStatusGlueScreen features = new();
            features.BpayStoreAvailable = false;
            features.BpayStoreDisabledByParentalControls = false;
            features.CharUndeleteEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
            features.MaxCharactersOnThisRealm = WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm);
            features.MinimumExpansionLevel = (int)Expansion.Classic;
            features.MaximumExpansionLevel = WorldConfig.GetIntValue(WorldCfg.Expansion);

            var europaTicketConfig = new EuropaTicketConfig();
            europaTicketConfig.ThrottleState.MaxTries = 10;
            europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
            europaTicketConfig.ThrottleState.TryCount = 1;
            europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
            europaTicketConfig.TicketsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
            europaTicketConfig.BugsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
            europaTicketConfig.ComplaintsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
            europaTicketConfig.SuggestionsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

            foreach (var (gameRule, value) in Global.WorldMgr.GetGameRules())
            {
                GameRuleValuePair rule = new();
                rule.Rule = (int)gameRule;

                if (value is float)
                    rule.ValueF = (float)value;
                else
                    rule.Value = Convert.ToInt32(value);

                features.GameRules.Add(rule);
            }

            features.EuropaTicketSystemStatus = europaTicketConfig;

            SendPacket(features);

            MirrorVarSingle[] vars =
            {
                new MirrorVarSingle("raidLockoutExtendEnabled", "1"),
                new MirrorVarSingle("bypassItemLevelScalingCode", "0"),
                new MirrorVarSingle("shop2Enabled", "0"),
                new MirrorVarSingle("bpayStoreEnable", "0"),
                new MirrorVarSingle("recentAlliesEnabledClient", "0"),
                new MirrorVarSingle("browserEnabled", "0"),
            };

            MirrorVars variables = new();
            variables.Variables = vars;
            SendPacket(variables);
        }
    }
}
