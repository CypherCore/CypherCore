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
using Game.Entities;
using Game.Guilds;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.Inspect)]
        void HandleInspect(Inspect inspect)
        {
            Player player = Global.ObjAccessor.FindPlayer(inspect.Target);
            if (!player)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleInspectOpcode: Target {0} not found.", inspect.Target.ToString());
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(player, SharedConst.InspectDistance, false))
                return;

            if (GetPlayer().IsValidAttackTarget(player))
                return;

            InspectResult inspectResult = new InspectResult();
            inspectResult.InspecteeGUID = inspect.Target;

            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                Item item = player.GetItemByPos(InventorySlots.Bag0, i);
                if (item)
                    inspectResult.Items.Add(new InspectItemData(item, i));
            }

            inspectResult.ClassID = player.GetClass();
            inspectResult.GenderID = (Gender)player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender);

            if (GetPlayer().CanBeGameMaster() || WorldConfig.GetIntValue(WorldCfg.TalentsInspecting) + (GetPlayer().GetTeamId() == player.GetTeamId() ? 1 : 0) > 1)
            {
                var talents = player.GetTalentMap(player.GetActiveTalentGroup());
                foreach (var v in talents)
                {
                    if (v.Value != PlayerSpellState.Removed)
                        inspectResult.Talents.Add((ushort)v.Key);
                }
            }

            Guild guild = Global.GuildMgr.GetGuildById(player.GetGuildId());
            if (guild)
            {
                inspectResult.GuildData.HasValue = true;

                InspectGuildData guildData;
                guildData.GuildGUID = guild.GetGUID();
                guildData.NumGuildMembers = guild.GetMembersCount();
                guildData.AchievementPoints = (int)guild.GetAchievementMgr().GetAchievementPoints();
                inspectResult.GuildData.Set(guildData);
            }

            inspectResult.InspecteeGUID = inspect.Target;
            inspectResult.SpecializationID = (int)player.GetUInt32Value(PlayerFields.CurrentSpecId);

            SendPacket(inspectResult);
        }

        [WorldPacketHandler(ClientOpcodes.RequestHonorStats)]
        void HandleRequestHonorStatsOpcode(RequestHonorStats request)
        {
            Player player = Global.ObjAccessor.FindPlayer(request.TargetGUID);
            if (!player)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleRequestHonorStatsOpcode: Target {0} not found.", request.TargetGUID.ToString());
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(player, SharedConst.InspectDistance, false))
                return;

            if (GetPlayer().IsValidAttackTarget(player))
                return;

            InspectHonorStats honorStats = new InspectHonorStats();
            honorStats.PlayerGUID = request.TargetGUID;
            honorStats.LifetimeHK = player.GetUInt32Value(ActivePlayerFields.LifetimeHonorableKills);
            honorStats.YesterdayHK = player.GetUInt16Value(ActivePlayerFields.Kills, 1);
            honorStats.TodayHK = player.GetUInt16Value(ActivePlayerFields.Kills, 0);
            honorStats.LifetimeMaxRank = 0; // @todo

            SendPacket(honorStats);
        }

        [WorldPacketHandler(ClientOpcodes.InspectPvp)]
        void HandleInspectPVP(InspectPVPRequest request)
        {
            // @todo: deal with request.InspectRealmAddress

            Player player = Global.ObjAccessor.FindPlayer(request.InspectTarget);
            if (!player)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleInspectPVP: Target {0} not found.", request.InspectTarget.ToString());
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(player, SharedConst.InspectDistance, false))
                return;

            if (GetPlayer().IsValidAttackTarget(player))
                return;

            InspectPVPResponse response = new InspectPVPResponse();
            response.ClientGUID = request.InspectTarget;
            // @todo: fill brackets

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryInspectAchievements)]
        void HandleQueryInspectAchievements(QueryInspectAchievements inspect)
        {
            Player player = Global.ObjAccessor.FindPlayer(inspect.Guid);
            if (!player)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleQueryInspectAchievements: [{0}] inspected unknown Player [{1}]", GetPlayer().GetGUID().ToString(), inspect.Guid.ToString());
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(player, SharedConst.InspectDistance, false))
                return;

            if (GetPlayer().IsValidAttackTarget(player))
                return;

            player.SendRespondInspectAchievements(GetPlayer());
        }
    }
}
