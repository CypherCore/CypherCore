/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Framework.IO;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using Game.PvP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.RequestAccountData, Status = SessionStatus.Authed)]
        private void HandleRequestAccountData(RequestAccountData request)
        {
            if (request.DataType > AccountDataTypes.Max)
                return;

            var adata = GetAccountData(request.DataType);

            var data = new UpdateAccountData();
            data.Player = GetPlayer() ? GetPlayer().GetGUID() : ObjectGuid.Empty;
            data.Time = (uint)adata.Time;
            data.DataType = request.DataType;

            if (!adata.Data.IsEmpty())
            {
                data.Size = (uint)adata.Data.Length;
                data.CompressedData = new ByteBuffer(ZLib.Compress(Encoding.UTF8.GetBytes(adata.Data)));
            }

            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateAccountData, Status = SessionStatus.Authed)]
        private void HandleUpdateAccountData(UserClientUpdateAccountData packet)
        {
            if (packet.DataType > AccountDataTypes.Max)
                return;

            if (packet.Size == 0)
            {
                SetAccountData(packet.DataType, 0, "");
                return;
            }

            if (packet.Size > 0xFFFF)
            {
                Log.outError(LogFilter.Network, "UpdateAccountData: Account data packet too big, size {0}", packet.Size);
                return;
            }

            var data = ZLib.Decompress(packet.CompressedData.GetData(), packet.Size);
            SetAccountData(packet.DataType, packet.Time, Encoding.Default.GetString(data));
        }

        [WorldPacketHandler(ClientOpcodes.SetSelection)]
        private void HandleSetSelection(SetSelection packet)
        {
            GetPlayer().SetSelection(packet.Selection);
        }

        [WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed)]
        private void HandleObjectUpdateFailed(ObjectUpdateFailed objectUpdateFailed)
        {
            Log.outError(LogFilter.Network, "Object update failed for {0} for player {1} ({2})", objectUpdateFailed.ObjectGUID.ToString(), GetPlayerName(), GetPlayer().GetGUID().ToString());

            // If create object failed for current player then client will be stuck on loading screen
            if (GetPlayer().GetGUID() == objectUpdateFailed.ObjectGUID)
            {
                LogoutPlayer(true);
                return;
            }

            // Pretend we've never seen this object
            GetPlayer().m_clientGUIDs.Remove(objectUpdateFailed.ObjectGUID);
        }

        [WorldPacketHandler(ClientOpcodes.ObjectUpdateRescued, Processing = PacketProcessing.Inplace)]
        private void HandleObjectUpdateRescued(ObjectUpdateRescued objectUpdateRescued)
        {
            Log.outError(LogFilter.Network, "Object update rescued for {0} for player {1} ({2})", objectUpdateRescued.ObjectGUID.ToString(), GetPlayerName(), GetPlayer().GetGUID().ToString());

            // Client received values update after destroying object
            // re-register object in m_clientGUIDs to send DestroyObject on next visibility update
            GetPlayer().m_clientGUIDs.Add(objectUpdateRescued.ObjectGUID);
        }

        [WorldPacketHandler(ClientOpcodes.SetActionButton)]
        private void HandleSetActionButton(SetActionButton packet)
        {
            var action = packet.GetButtonAction();
            var type = packet.GetButtonType();

            if (packet.Action == 0)
                GetPlayer().RemoveActionButton(packet.Index);
            else
                GetPlayer().AddActionButton(packet.Index, action, type);
        }

        [WorldPacketHandler(ClientOpcodes.SetActionBarToggles)]
        private void HandleSetActionBarToggles(SetActionBarToggles packet)
        {
            if (!GetPlayer())                                        // ignore until not logged (check needed because STATUS_AUTHED)
            {
                if (packet.Mask != 0)
                    Log.outError(LogFilter.Network, "WorldSession.HandleSetActionBarToggles in not logged state with value: {0}, ignored", packet.Mask);
                return;
            }

            GetPlayer().SetMultiActionBars(packet.Mask);
        }

        [WorldPacketHandler(ClientOpcodes.CompleteCinematic)]
        private void HandleCompleteCinematic(CompleteCinematic packet)
        {
            // If player has sight bound to visual waypoint NPC we should remove it
            GetPlayer().GetCinematicMgr().EndCinematic();
        }

        [WorldPacketHandler(ClientOpcodes.NextCinematicCamera)]
        private void HandleNextCinematicCamera(NextCinematicCamera packet)
        {
            // Sent by client when cinematic actually begun. So we begin the server side process
            GetPlayer().GetCinematicMgr().NextCinematicCamera();
        }

        [WorldPacketHandler(ClientOpcodes.CompleteMovie)]
        private void HandleCompleteMovie(CompleteMovie packet)
        {
            var movie = _player.GetMovie();
            if (movie == 0)
                return;

            _player.SetMovie(0);
            Global.ScriptMgr.OnMovieComplete(_player, movie);
        }

        [WorldPacketHandler(ClientOpcodes.ViolenceLevel, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
        private void HandleViolenceLevel(ViolenceLevel violenceLevel)
        {
            // do something?
        }

        [WorldPacketHandler(ClientOpcodes.AreaTrigger)]
        private void HandleAreaTrigger(AreaTriggerPkt packet)
        {
            var player = GetPlayer();
            if (player.IsInFlight())
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' (GUID: {1}) in flight, ignore Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            var atEntry = CliDB.AreaTriggerStorage.LookupByKey(packet.AreaTriggerID);
            if (atEntry == null)
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' (GUID: {1}) send unknown (by DBC) Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (packet.Entered && !player.IsInAreaTriggerRadius(atEntry))
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' ({1}) too far, ignore Area Trigger ID: {2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (player.IsDebugAreaTriggers)
                player.SendSysMessage(packet.Entered ? CypherStrings.DebugAreatriggerEntered : CypherStrings.DebugAreatriggerLeft, packet.AreaTriggerID);

            if (Global.ScriptMgr.OnAreaTrigger(player, atEntry, packet.Entered))
                return;

            if (player.IsAlive())
            {
                List<uint> quests = Global.ObjectMgr.GetQuestsForAreaTrigger(packet.AreaTriggerID);
                if (quests != null)
                {
                    foreach (var questId in quests)
                    {
                        Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questId);
                        if (qInfo != null && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                        {
                            foreach (var obj in qInfo.Objectives)
                            {
                                if (obj.Type == QuestObjectiveType.AreaTrigger && !player.IsQuestObjectiveComplete(obj))
                                {
                                    player.SetQuestObjectiveData(obj, 1);
                                    player.SendQuestUpdateAddCreditSimple(obj);
                                    break;
                                }
                            }

                            if (player.CanCompleteQuest(questId))
                                player.CompleteQuest(questId);
                        }
                    }
                }
            }

            if (Global.ObjectMgr.IsTavernAreaTrigger(packet.AreaTriggerID))
            {
                // set resting flag we are in the inn
                player.GetRestMgr().SetRestFlag(RestFlag.Tavern, atEntry.Id);

                if (Global.WorldMgr.IsFFAPvPRealm())
                    player.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);

                return;
            }
            Battleground bg = player.GetBattleground();
            if (bg)
                bg.HandleAreaTrigger(player, packet.AreaTriggerID, packet.Entered);

            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null)
            {
                if (pvp.HandleAreaTrigger(player, packet.AreaTriggerID, packet.Entered))
                    return;
            }

            AreaTriggerStruct at = Global.ObjectMgr.GetAreaTrigger(packet.AreaTriggerID);
            if (at == null)
                return;

            var teleported = false;
            if (player.GetMapId() != at.target_mapId)
            {
                var denyReason = Global.MapMgr.PlayerCannotEnter(at.target_mapId, player, false);
                if (denyReason != 0)
                {
                    var reviveAtTrigger = false; // should we revive the player if he is trying to enter the correct instance?
                    switch (denyReason)
                    {
                        case EnterState.CannotEnterNoEntry:
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' attempted to enter map with id {1} which has no entry", player.GetName(), at.target_mapId);
                            break;
                        case EnterState.CannotEnterUninstancedDungeon:
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' attempted to enter dungeon map {1} but no instance template was found", player.GetName(), at.target_mapId);
                            break;
                        case EnterState.CannotEnterDifficultyUnavailable:
                            {
                                Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' attempted to enter instance map {1} but the requested difficulty was not found", player.GetName(), at.target_mapId);
                                var entry = CliDB.MapStorage.LookupByKey(at.target_mapId);
                                if (entry != null)
                                    player.SendTransferAborted(entry.Id, TransferAbortReason.Difficulty, (byte)player.GetDifficultyID(entry));
                            }
                            break;
                        case EnterState.CannotEnterNotInRaid:
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' must be in a raid group to enter map {1}", player.GetName(), at.target_mapId);
                            player.SendRaidGroupOnlyMessage(RaidGroupReason.Only, 0);
                            reviveAtTrigger = true;
                            break;
                        case EnterState.CannotEnterCorpseInDifferentInstance:
                            player.SendPacket(new AreaTriggerNoCorpse());
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' does not have a corpse in instance map {1} and cannot enter", player.GetName(), at.target_mapId);
                            break;
                        case EnterState.CannotEnterInstanceBindMismatch:
                            {
                                var entry = CliDB.MapStorage.LookupByKey(at.target_mapId);
                                if (entry != null)
                                {
                                    var mapName = entry.MapName[player.GetSession().GetSessionDbcLocale()];
                                    Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' cannot enter instance map '{1}' because their permanent bind is incompatible with their group's", player.GetName(), mapName);
                                    // is there a special opcode for this?
                                    // @todo figure out how to get player localized difficulty string (e.g. "10 player", "Heroic" etc)
                                    player.SendSysMessage(CypherStrings.InstanceBindMismatch, mapName);
                                }
                                reviveAtTrigger = true;
                            }
                            break;
                        case EnterState.CannotEnterTooManyInstances:
                            player.SendTransferAborted(at.target_mapId, TransferAbortReason.TooManyInstances);
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' cannot enter instance map {1} because he has exceeded the maximum number of instances per hour.", player.GetName(), at.target_mapId);
                            reviveAtTrigger = true;
                            break;
                        case EnterState.CannotEnterMaxPlayers:
                            player.SendTransferAborted(at.target_mapId, TransferAbortReason.MaxPlayers);
                            reviveAtTrigger = true;
                            break;
                        case EnterState.CannotEnterZoneInCombat:
                            player.SendTransferAborted(at.target_mapId, TransferAbortReason.ZoneInCombat);
                            reviveAtTrigger = true;
                            break;
                        default:
                            break;
                    }

                    if (reviveAtTrigger) // check if the player is touching the areatrigger leading to the map his corpse is on
                    {
                        if (!player.IsAlive() && player.HasCorpse())
                        {
                            if (player.GetCorpseLocation().GetMapId() == at.target_mapId)
                            {
                                player.ResurrectPlayer(0.5f);
                                player.SpawnCorpseBones();
                            }
                        }
                    }

                    return;
                }

                var group = player.GetGroup();
                if (group)
                    if (group.IsLFGGroup() && player.GetMap().IsDungeon())
                        teleported = player.TeleportToBGEntryPoint();
            }

            if (!teleported)
            {
                WorldSafeLocsEntry entranceLocation = null;
                InstanceSave instanceSave = player.GetInstanceSave(at.target_mapId);
                if (instanceSave != null)
                {
                    // Check if we can contact the instancescript of the instance for an updated entrance location
                    var map = Global.MapMgr.FindMap(at.target_mapId, player.GetInstanceSave(at.target_mapId).GetInstanceId());
                    if (map)
                    {
                        var instanceMap = map.ToInstanceMap();
                        if (instanceMap != null)
                        {
                            var instanceScript = instanceMap.GetInstanceScript();
                            if (instanceScript != null)
                                entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceScript.GetEntranceLocation());
                        }
                    }

                    // Finally check with the instancesave for an entrance location if we did not get a valid one from the instancescript
                    if (entranceLocation == null)
                        entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceSave.GetEntranceLocation());
                }

                if (entranceLocation != null)
                    player.TeleportTo(entranceLocation.Loc, TeleportToOptions.NotLeaveTransport);
                else
                    player.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, at.target_Orientation, TeleportToOptions.NotLeaveTransport);
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestPlayedTime)]
        private void HandlePlayedTime(RequestPlayedTime packet)
        {
            var playedTime = new PlayedTime();
            playedTime.TotalTime = GetPlayer().GetTotalPlayedTime();
            playedTime.LevelTime = GetPlayer().GetLevelPlayedTime();
            playedTime.TriggerEvent = packet.TriggerScriptEvent;  // 0-1 - will not show in chat frame
            SendPacket(playedTime);
        }

        [WorldPacketHandler(ClientOpcodes.SaveCufProfiles, Processing = PacketProcessing.Inplace)]
        private void HandleSaveCUFProfiles(SaveCUFProfiles packet)
        {
            if (packet.CUFProfiles.Count > PlayerConst.MaxCUFProfiles)
            {
                Log.outError(LogFilter.Player, "HandleSaveCUFProfiles - {0} tried to save more than {1} CUF profiles. Hacking attempt?", GetPlayerName(), PlayerConst.MaxCUFProfiles);
                return;
            }

            for (byte i = 0; i < packet.CUFProfiles.Count; ++i)
                GetPlayer().SaveCUFProfile(i, packet.CUFProfiles[i]);

            for (var i = (byte)packet.CUFProfiles.Count; i < PlayerConst.MaxCUFProfiles; ++i)
                GetPlayer().SaveCUFProfile(i, null);
        }

        public void SendLoadCUFProfiles()
        {
            var player = GetPlayer();

            var loadCUFProfiles = new LoadCUFProfiles();

            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                var cufProfile = player.GetCUFProfile(i);
                if (cufProfile != null)
                    loadCUFProfiles.CUFProfiles.Add(cufProfile);
            }

            SendPacket(loadCUFProfiles);
        }

        [WorldPacketHandler(ClientOpcodes.SetAdvancedCombatLogging, Processing = PacketProcessing.Inplace)]
        private void HandleSetAdvancedCombatLogging(SetAdvancedCombatLogging setAdvancedCombatLogging)
        {
            GetPlayer().SetAdvancedCombatLogging(setAdvancedCombatLogging.Enable);
        }

        [WorldPacketHandler(ClientOpcodes.MountSpecialAnim)]
        private void HandleMountSpecialAnim(MountSpecial mountSpecial)
        {
            var specialMountAnim = new SpecialMountAnim();
            specialMountAnim.UnitGUID = _player.GetGUID();
            GetPlayer().SendMessageToSet(specialMountAnim, false);
        }

        [WorldPacketHandler(ClientOpcodes.MountSetFavorite)]
        private void HandleMountSetFavorite(MountSetFavorite mountSetFavorite)
        {
            _collectionMgr.MountSetFavorite(mountSetFavorite.MountSpellID, mountSetFavorite.IsFavorite);
        }

        [WorldPacketHandler(ClientOpcodes.CloseInteraction)]
        private void HandleCloseInteraction(CloseInteraction closeInteraction)
        {
            if (_player.PlayerTalkClass.GetInteractionData().SourceGuid == closeInteraction.SourceGuid)
                _player.PlayerTalkClass.GetInteractionData().Reset();
        }

        [WorldPacketHandler(ClientOpcodes.ChatUnregisterAllAddonPrefixes)]
        private void HandleUnregisterAllAddonPrefixes(ChatUnregisterAllAddonPrefixes packet)
        {
            _registeredAddonPrefixes.Clear();
        }

        [WorldPacketHandler(ClientOpcodes.ChatRegisterAddonPrefixes)]
        private void HandleAddonRegisteredPrefixes(ChatRegisterAddonPrefixes packet)
        {
            _registeredAddonPrefixes.AddRange(packet.Prefixes);

            if (_registeredAddonPrefixes.Count > 64) // shouldn't happen
            {
                _filterAddonMessages = false;
                return;
            }

            _filterAddonMessages = true;
        }

        [WorldPacketHandler(ClientOpcodes.TogglePvp)]
        private void HandleTogglePvP(TogglePvP packet)
        {
            if (GetPlayer().HasPlayerFlag(PlayerFlags.InPVP))
            {
                GetPlayer().RemovePlayerFlag(PlayerFlags.InPVP);
                GetPlayer().AddPlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().pvpInfo.IsHostile && GetPlayer().IsPvP())
                    GetPlayer().pvpInfo.EndTimer = Time.UnixTime; // start toggle-off
            }
            else
            {
                GetPlayer().AddPlayerFlag(PlayerFlags.InPVP);
                GetPlayer().RemovePlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().IsPvP() || GetPlayer().pvpInfo.EndTimer != 0)
                    GetPlayer().UpdatePvP(true, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetPvp)]
        private void HandleSetPvP(SetPvP packet)
        {
            if (!packet.EnablePVP)
            {
                GetPlayer().RemovePlayerFlag(PlayerFlags.InPVP);
                GetPlayer().AddPlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().pvpInfo.IsHostile && GetPlayer().IsPvP())
                    GetPlayer().pvpInfo.EndTimer = Time.UnixTime; // start toggle-off
            }
            else
            {
                GetPlayer().AddPlayerFlag(PlayerFlags.InPVP);
                GetPlayer().RemovePlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().IsPvP() || GetPlayer().pvpInfo.EndTimer != 0)
                    GetPlayer().UpdatePvP(true, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.FarSight)]
        private void HandleFarSight(FarSight farSight)
        {
            if (farSight.Enable)
            {
                Log.outDebug(LogFilter.Network, "Added FarSight {0} to player {1}", GetPlayer().m_activePlayerData.FarsightObject.ToString(), GetPlayer().GetGUID().ToString());
                var target = GetPlayer().GetViewpoint();
                if (target)
                    GetPlayer().SetSeer(target);
                else
                    Log.outDebug(LogFilter.Network, "Player {0} (GUID: {1}) requests non-existing seer {2}", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetPlayer().m_activePlayerData.FarsightObject.ToString());
            }
            else
            {
                Log.outDebug(LogFilter.Network, "Player {0} set vision to self", GetPlayer().GetGUID().ToString());
                GetPlayer().SetSeer(GetPlayer());
            }

            GetPlayer().UpdateVisibilityForPlayer();
        }

        [WorldPacketHandler(ClientOpcodes.SetTitle)]
        private void HandleSetTitle(SetTitle packet)
        {
            // -1 at none
            if (packet.TitleID > 0)
            {
                if (!GetPlayer().HasTitle((uint)packet.TitleID))
                    return;
            }
            else
                packet.TitleID = 0;

            GetPlayer().SetChosenTitle((uint)packet.TitleID);
        }

        [WorldPacketHandler(ClientOpcodes.ResetInstances)]
        private void HandleResetInstances(ResetInstances packet)
        {
            var group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                    group.ResetInstances(InstanceResetMethod.All, false, false, GetPlayer());
            }
            else
                GetPlayer().ResetInstances(InstanceResetMethod.All, false, false);
        }

        [WorldPacketHandler(ClientOpcodes.SetDungeonDifficulty)]
        private void HandleSetDungeonDifficulty(SetDungeonDifficulty setDungeonDifficulty)
        {
            var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setDungeonDifficulty.DifficultyID);
            if (difficultyEntry == null)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), setDungeonDifficulty.DifficultyID);
                return;
            }

            if (difficultyEntry.InstanceType != MapTypes.Instance)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            var difficultyID = (Difficulty)difficultyEntry.Id;
            if (difficultyID == GetPlayer().GetDungeonDifficultyID())
                return;

            // cannot reset while in an instance
            var map = GetPlayer().GetMap();
            if (map && map.IsDungeon())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetDungeonDifficulty: player (Name: {0}, {1}) tried to reset the instance while player is inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            var group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                    {
                        var groupGuy = refe.GetSource();
                        if (!groupGuy)
                            continue;

                        if (!groupGuy.IsInMap(groupGuy))
                            return;

                        if (groupGuy.GetMap().IsNonRaidDungeon())
                        {
                            Log.outDebug(LogFilter.Network, "WorldSession:HandleSetDungeonDifficulty: {0} tried to reset the instance while group member (Name: {1}, {2}) is inside!",
                                GetPlayer().GetGUID().ToString(), groupGuy.GetName(), groupGuy.GetGUID().ToString());
                            return;
                        }
                    }
                    // the difficulty is set even if the instances can't be reset
                    //_player.SendDungeonDifficulty(true);
                    group.ResetInstances(InstanceResetMethod.ChangeDifficulty, false, false, GetPlayer());
                    group.SetDungeonDifficultyID(difficultyID);
                }
            }
            else
            {
                GetPlayer().ResetInstances(InstanceResetMethod.ChangeDifficulty, false, false);
                GetPlayer().SetDungeonDifficultyID(difficultyID);
                GetPlayer().SendDungeonDifficulty();
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetRaidDifficulty)]
        private void HandleSetRaidDifficulty(SetRaidDifficulty setRaidDifficulty)
        {
            var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setRaidDifficulty.DifficultyID);
            if (difficultyEntry == null)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), setRaidDifficulty.DifficultyID);
                return;
            }

            if (difficultyEntry.InstanceType != MapTypes.Raid)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            if (((int)(difficultyEntry.Flags & DifficultyFlags.Legacy) >> 5) != setRaidDifficulty.Legacy)
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent not matching legacy difficulty {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            var difficultyID = (Difficulty)difficultyEntry.Id;
            if (difficultyID == (setRaidDifficulty.Legacy != 0 ? GetPlayer().GetLegacyRaidDifficultyID() : GetPlayer().GetRaidDifficultyID()))
                return;

            // cannot reset while in an instance
            var map = GetPlayer().GetMap();
            if (map && map.IsDungeon())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetRaidDifficulty: player (Name: {0}, {1} tried to reset the instance while inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            var group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                    {
                        var groupGuy = refe.GetSource();
                        if (!groupGuy)
                            continue;

                        if (!groupGuy.IsInMap(groupGuy))
                            return;

                        if (groupGuy.GetMap().IsRaid())
                        {
                            Log.outDebug(LogFilter.Network, "WorldSession:HandleSetRaidDifficulty: player {0} tried to reset the instance while inside!", GetPlayer().GetGUID().ToString());
                            return;
                        }
                    }
                    // the difficulty is set even if the instances can't be reset
                    group.ResetInstances(InstanceResetMethod.ChangeDifficulty, true, setRaidDifficulty.Legacy != 0, GetPlayer());
                    if (setRaidDifficulty.Legacy != 0)
                        group.SetLegacyRaidDifficultyID(difficultyID);
                    else
                        group.SetRaidDifficultyID(difficultyID);
                }
            }
            else
            {
                GetPlayer().ResetInstances(InstanceResetMethod.ChangeDifficulty, true, setRaidDifficulty.Legacy != 0);
                if (setRaidDifficulty.Legacy != 0)
                    GetPlayer().SetLegacyRaidDifficultyID(difficultyID);
                else
                    GetPlayer().SetRaidDifficultyID(difficultyID);

                GetPlayer().SendRaidDifficulty(setRaidDifficulty.Legacy != 0);
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetTaxiBenchmarkMode, Processing = PacketProcessing.Inplace)]
        private void HandleSetTaxiBenchmark(SetTaxiBenchmarkMode packet)
        {
            if (packet.Enable)
                _player.AddPlayerFlag(PlayerFlags.TaxiBenchmark);
            else
                _player.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
        private void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
        {
            var guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId());
            if (guild)
                guild.GetAchievementMgr().SendAchievementInfo(GetPlayer(), setFocusedAchievement.AchievementID);
        }

        [WorldPacketHandler(ClientOpcodes.InstanceLockResponse)]
        private void HandleInstanceLockResponse(InstanceLockResponse packet)
        {
            if (!GetPlayer().HasPendingBind())
            {
                Log.outInfo(LogFilter.Network, "InstanceLockResponse: Player {0} (guid {1}) tried to bind himself/teleport to graveyard without a pending bind!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            if (packet.AcceptLock)
                GetPlayer().BindToInstance();
            else
                GetPlayer().RepopAtGraveyard();

            GetPlayer().SetPendingBind(0, 0);
        }

        [WorldPacketHandler(ClientOpcodes.Warden3Data)]
        private void HandleWarden3Data(WardenData packet)
        {
            if (_warden == null || packet.Data.GetSize() == 0)
                return;

            _warden.DecryptData(packet.Data.GetData());
            var opcode = (WardenOpcodes)packet.Data.ReadUInt8();

            switch (opcode)
            {
                case WardenOpcodes.CMSG_ModuleMissing:
                    _warden.SendModuleToClient();
                    break;
                case WardenOpcodes.Cmsg_ModuleOk:
                    _warden.RequestHash();
                    break;
                case WardenOpcodes.Smsg_CheatChecksRequest:
                    _warden.HandleData(packet.Data);
                    break;
                case WardenOpcodes.Cmsg_MemChecksResult:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");
                    break;
                case WardenOpcodes.Cmsg_HashResult:
                    _warden.HandleHashResult(packet.Data);
                    _warden.InitializeModule();
                    break;
                case WardenOpcodes.Cmsg_ModuleFailed:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MODULE_FAILED received!");
                    break;
                default:
                    Log.outDebug(LogFilter.Warden, "Got unknown warden opcode {0} of size {1}.", opcode, packet.Data.GetSize() - 1);
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
        private void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest adventureJournalOpenQuest)
        {
            var adventureJournalEntry = CliDB.AdventureJournalStorage.LookupByKey(adventureJournalOpenQuest.AdventureJournalID);
            if (adventureJournalEntry == null)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(adventureJournalEntry.QuestID);
            if (quest == null)
                return;

            if (_player.CanTakeQuest(quest, true))
            {
                var menu = new PlayerMenu(_player.GetSession());
                menu.SendQuestGiverQuestDetails(quest, _player.GetGUID(), true, false);
            }
        }

        [WorldPacketHandler(ClientOpcodes.AdventureJournalStartQuest)]
        private void HandleAdventureJournalStartQuest(AdventureJournalStartQuest adventureJournalStartQuest)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(adventureJournalStartQuest.QuestID);
            if (quest == null)
                return;

            AdventureJournalRecord adventureJournalEntry = null;
            foreach (var adventureJournal in CliDB.AdventureJournalStorage.Values)
            {
                if (quest.Id == adventureJournal.QuestID)
                {
                    adventureJournalEntry = adventureJournal;
                    break;
                }
            }

            if (adventureJournalEntry == null)
                return;

            if (_player.MeetPlayerCondition(adventureJournalEntry.PlayerConditionID) && _player.CanTakeQuest(quest, true))
                _player.AddQuestAndCheckCompletion(quest, null);
        }

        [WorldPacketHandler(ClientOpcodes.AdventureJournalUpdateSuggestions)]
        private void HandleAdventureJournalUpdateSuggestions(AdventureJournalUpdateSuggestions adventureJournalUpdateSuggestions)
        {
            if (adventureJournalUpdateSuggestions.OnLevelUp && _player.GetLevel() < 10)
                return;

            var response = new AdventureJournalDataResponse();
            response.OnLevelUp = adventureJournalUpdateSuggestions.OnLevelUp;

            foreach (var adventureJournal in CliDB.AdventureJournalStorage.Values)
            {
                if (_player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
                {
                    AdventureJournalData adventureJournalData;
                    adventureJournalData.AdventureJournalID = (int)adventureJournal.Id;
                    adventureJournalData.Priority = adventureJournal.PriorityMax;
                    response.AdventureJournalDatas.Add(adventureJournalData);
                }
            }

            SendPacket(response);
        }
    }
}
