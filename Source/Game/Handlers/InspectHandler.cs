// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.Inspect, Processing = PacketProcessing.Inplace)]
        void HandleInspect(Inspect inspect)
        {
            Player player = Global.ObjAccessor.GetPlayer(_player, inspect.Target);
            if (!player)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleInspectOpcode: Target {0} not found.", inspect.Target.ToString());
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(player, SharedConst.InspectDistance, false))
                return;

            if (GetPlayer().IsValidAttackTarget(player))
                return;

            InspectResult inspectResult = new();
            inspectResult.DisplayInfo.Initialize(player);

            if (GetPlayer().CanBeGameMaster() || WorldConfig.GetIntValue(WorldCfg.TalentsInspecting) + (GetPlayer().GetEffectiveTeam() == player.GetEffectiveTeam() ? 1 : 0) > 1)
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
                InspectGuildData guildData;
                guildData.GuildGUID = guild.GetGUID();
                guildData.NumGuildMembers = guild.GetMembersCount();
                guildData.AchievementPoints = (int)guild.GetAchievementMgr().GetAchievementPoints();

                inspectResult.GuildData = guildData;
            }

            Item heartOfAzeroth = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (heartOfAzeroth != null)
            {
                AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                if (azeriteItem != null)
                    inspectResult.AzeriteLevel = azeriteItem.GetEffectiveLevel();
            }

            inspectResult.ItemLevel = (int)player.GetAverageItemLevel();
            inspectResult.LifetimeMaxRank = player.m_activePlayerData.LifetimeMaxRank;
            inspectResult.TodayHK = player.m_activePlayerData.TodayHonorableKills;
            inspectResult.YesterdayHK = player.m_activePlayerData.YesterdayHonorableKills;
            inspectResult.LifetimeHK = player.m_activePlayerData.LifetimeHonorableKills;
            inspectResult.HonorLevel = player.m_playerData.HonorLevel;

            SendPacket(inspectResult);
        }

        [WorldPacketHandler(ClientOpcodes.QueryInspectAchievements, Processing = PacketProcessing.Inplace)]
        void HandleQueryInspectAchievements(QueryInspectAchievements inspect)
        {
            Player player = Global.ObjAccessor.GetPlayer(_player, inspect.Guid);
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
