// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            if (player == null)
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

                var pvpTalents = player.GetPvpTalentMap(player.GetActiveTalentGroup());
                for (int i = 0; i < pvpTalents.Length; ++i)
                    inspectResult.PvpTalents[i] = (ushort)pvpTalents[i];

                inspectResult.TraitsInfo.PlayerLevel = (int)player.GetLevel();
                inspectResult.TraitsInfo.SpecID = (int)player.GetPrimarySpecialization();
                TraitConfig traitConfig = player.GetTraitConfig((int)(uint)player.m_activePlayerData.ActiveCombatTraitConfigID);
                if (traitConfig != null)
                    inspectResult.TraitsInfo.ActiveCombatTraits = new TraitConfigPacket(traitConfig);
            }

            Guild guild = Global.GuildMgr.GetGuildById(player.GetGuildId());
            if (guild != null)
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
            if (player == null)
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
