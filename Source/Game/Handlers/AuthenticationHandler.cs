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
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
        {
            AuthResponse response = new AuthResponse();
            response.Result = code;

            if (code == BattlenetRpcErrorCode.Ok)
            {
                response.SuccessInfo.HasValue = true;

                response.SuccessInfo.Value = new AuthResponse.AuthSuccessInfo();
                response.SuccessInfo.Value.AccountExpansionLevel = (byte)GetAccountExpansion();
                response.SuccessInfo.Value.ActiveExpansionLevel = (byte)GetExpansion();
                response.SuccessInfo.Value.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                response.SuccessInfo.Value.Time = (uint)Time.UnixTime;

                var realm = Global.WorldMgr.GetRealm();

                // Send current home realm. Also there is no need to send it later in realm queries.
                response.SuccessInfo.Value.VirtualRealms.Add(new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName));

                if (HasPermission(RBACPermissions.UseCharacterTemplates))
                    foreach (var templ in Global.CharacterTemplateDataStorage.GetCharacterTemplates().Values)
                        response.SuccessInfo.Value.Templates.Add(templ);

                response.SuccessInfo.Value.AvailableClasses = Global.ObjectMgr.GetClassExpansionRequirements();
            }

            if (queued)
            {
                response.WaitInfo.HasValue = true;
                response.WaitInfo.Value.WaitCount = queuePos;
            }

            SendPacket(response);
        }

        public void SendAuthWaitQue(uint position)
        {
            if (position != 0)
            {
                WaitQueueUpdate waitQueueUpdate = new WaitQueueUpdate();
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
            ClientCacheVersion cache = new ClientCacheVersion();
            cache.CacheVersion = version;
            SendPacket(cache);//enabled it
        }

        public void SendSetTimeZoneInformation()
        {
            // @todo: replace dummy values
            SetTimeZoneInformation packet = new SetTimeZoneInformation();
            packet.ServerTimeTZ = "Europe/Paris";
            packet.GameTimeTZ = "Europe/Paris";

            SendPacket(packet);//enabled it
        }

        public void SendFeatureSystemStatusGlueScreen()
        {
            FeatureSystemStatusGlueScreen features = new FeatureSystemStatusGlueScreen();
            features.BpayStoreAvailable = false;
            features.BpayStoreDisabledByParentalControls = false;
            features.CharUndeleteEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
            features.BpayStoreEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
            features.MaxCharactersPerRealm = WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm);
            features.MinimumExpansionLevel = (int)Expansion.Classic;
            features.MaximumExpansionLevel = WorldConfig.GetIntValue(WorldCfg.Expansion);

            SendPacket(features);
        }
    }
}
