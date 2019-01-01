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
using Game.Groups;
using Game.Maps;
using Game.Scripting;

namespace Game.DungeonFinding
{
    class LFGPlayerScript : PlayerScript
    {
        public LFGPlayerScript() : base("LFGPlayerScript") { }

        // Player Hooks
        public override void OnLogout(Player player)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            if (!player.GetGroup())
                Global.LFGMgr.LeaveLfg(player.GetGUID());
            else if (player.GetSession().PlayerDisconnected())
                Global.LFGMgr.LeaveLfg(player.GetGUID(), true);
        }

        public override void OnLogin(Player player)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            // Temporal: Trying to determine when group data and LFG data gets desynched
            ObjectGuid guid = player.GetGUID();
            ObjectGuid gguid = Global.LFGMgr.GetGroup(guid);

            Group group = player.GetGroup();
            if (group)
            {
                ObjectGuid gguid2 = group.GetGUID();
                if (gguid != gguid2)
                {
                    Log.outError(LogFilter.Lfg, "{0} on group {1} but LFG has group {2} saved... Fixing.", player.GetSession().GetPlayerInfo(), gguid2.ToString(), gguid.ToString());
                    Global.LFGMgr.SetupGroupMember(guid, group.GetGUID());
                }
            }

            Global.LFGMgr.SetTeam(player.GetGUID(), player.GetTeam());
            // @todo - Restore LfgPlayerData and send proper status to player if it was in a group
        }

        public override void OnMapChanged(Player player)
        {
            Map map = player.GetMap();

            if (Global.LFGMgr.inLfgDungeonMap(player.GetGUID(), map.GetId(), map.GetDifficultyID()))
            {
                Group group = player.GetGroup();
                // This function is also called when players log in
                // if for some reason the LFG system recognises the player as being in a LFG dungeon,
                // but the player was loaded without a valid group, we'll teleport to homebind to prevent
                // crashes or other undefined behaviour
                if (!group)
                {
                    Global.LFGMgr.LeaveLfg(player.GetGUID());
                    player.RemoveAurasDueToSpell(SharedConst.LFGSpellLuckOfTheDraw);
                    player.TeleportTo(player.GetHomebind());
                    Log.outError(LogFilter.Lfg, "LFGPlayerScript.OnMapChanged, Player {0} ({1}) is in LFG dungeon map but does not have a valid group! Teleporting to homebind.",
                        player.GetName(), player.GetGUID().ToString());
                    return;
                }

                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player member = refe.GetSource();
                    if (member)
                        player.GetSession().SendNameQuery(member.GetGUID());
                }

                if (Global.LFGMgr.selectedRandomLfgDungeon(player.GetGUID()))
                    player.CastSpell(player, SharedConst.LFGSpellLuckOfTheDraw, true);
            }
            else
            {
                Group group = player.GetGroup();
                if (group && group.GetMembersCount() == 1)
                {
                    Global.LFGMgr.LeaveLfg(group.GetGUID());
                    group.Disband();
                    Log.outDebug(LogFilter.Lfg, "LFGPlayerScript::OnMapChanged, Player {0}({1}) is last in the lfggroup so we disband the group.",
                        player.GetName(), player.GetGUID().ToString());
                }

                player.RemoveAurasDueToSpell(SharedConst.LFGSpellLuckOfTheDraw);
            }
        }
    }

    class LFGGroupScript : GroupScript
    {
        public LFGGroupScript() : base("LFGGroupScript") { }

        // Group Hooks
        public override void OnAddMember(Group group, ObjectGuid guid)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            ObjectGuid gguid = group.GetGUID();
            ObjectGuid leader = group.GetLeaderGUID();

            if (leader == guid)
            {
                Log.outDebug(LogFilter.Lfg, "LFGScripts.OnAddMember [{0}]: added [{1} leader {2}]", gguid, guid, leader);
                Global.LFGMgr.SetLeader(gguid, guid);
            }
            else
            {
                LfgState gstate = Global.LFGMgr.GetState(gguid);
                LfgState state = Global.LFGMgr.GetState(guid);
                Log.outDebug(LogFilter.Lfg, "LFGScripts.OnAddMember [{0}]: added [{1} leader {2}] gstate: {3}, state: {4}", gguid, guid, leader, gstate, state);

                if (state == LfgState.Queued)
                    Global.LFGMgr.LeaveLfg(guid);

                if (gstate == LfgState.Queued)
                    Global.LFGMgr.LeaveLfg(gguid);
            }

            Global.LFGMgr.SetGroup(guid, gguid);
            Global.LFGMgr.AddPlayerToGroup(gguid, guid);
        }

        public override void OnRemoveMember(Group group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            ObjectGuid gguid = group.GetGUID();
            Log.outDebug(LogFilter.Lfg, "LFGScripts.OnRemoveMember [{0}]: remove [{1}] Method: {2} Kicker: {3} Reason: {4}", gguid, guid, method, kicker, reason);

            bool isLFG = group.isLFGGroup();

            if (isLFG && method == RemoveMethod.Kick)        // Player have been kicked
            {
                // @todo - Update internal kick cooldown of kicker
                string str_reason = "";
                if (!string.IsNullOrEmpty(reason))
                    str_reason = reason;
                Global.LFGMgr.InitBoot(gguid, kicker, guid, str_reason);
                return;
            }

            LfgState state = Global.LFGMgr.GetState(gguid);

            // If group is being formed after proposal success do nothing more
            if (state == LfgState.Proposal && method == RemoveMethod.Default)
            {
                // LfgData: Remove player from group
                Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
                Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);
                return;
            }

            Global.LFGMgr.LeaveLfg(guid);
            Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
            byte players = Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);

            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
            {
                if (method == RemoveMethod.Leave && state == LfgState.Dungeon &&
                    players >= SharedConst.LFGKickVotesNeeded)
                    player.CastSpell(player, SharedConst.LFGSpellDungeonDeserter, true);
                //else if (state == LFG_STATE_BOOT)
                // Update internal kick cooldown of kicked

                player.GetSession().SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.LeaderUnk1), true);
                if (isLFG && player.GetMap().IsDungeon())            // Teleport player out the dungeon
                    Global.LFGMgr.TeleportPlayer(player, true);
            }

            if (isLFG && state != LfgState.FinishedDungeon) // Need more players to finish the dungeon
            {
                Player leader = Global.ObjAccessor.FindPlayer(Global.LFGMgr.GetLeader(gguid));
                if (leader)
                    leader.GetSession().SendLfgOfferContinue(Global.LFGMgr.GetDungeon(gguid, false));
            }
        }

        public override void OnDisband(Group group)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            ObjectGuid gguid = group.GetGUID();
            Log.outDebug(LogFilter.Lfg, "LFGScripts.OnDisband {0}", gguid);

            Global.LFGMgr.RemoveGroupData(gguid);
        }

        public override void OnChangeLeader(Group group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            ObjectGuid gguid = group.GetGUID();

            Log.outDebug(LogFilter.Lfg, "LFGScripts.OnChangeLeader {0}: old {0} new {0}", gguid, newLeaderGuid, oldLeaderGuid);
            Global.LFGMgr.SetLeader(gguid, newLeaderGuid);
        }

        public override void OnInviteMember(Group group, ObjectGuid guid)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            ObjectGuid gguid = group.GetGUID();
            ObjectGuid leader = group.GetLeaderGUID();
            Log.outDebug(LogFilter.Lfg, "LFGScripts.OnInviteMember {0}: invite {0} leader {0}", gguid, guid, leader);
            // No gguid ==  new group being formed
            // No leader == after group creation first invite is new leader
            // leader and no gguid == first invite after leader is added to new group (this is the real invite)
            if (!leader.IsEmpty() && gguid.IsEmpty())
                Global.LFGMgr.LeaveLfg(leader);
        }
    }
}
