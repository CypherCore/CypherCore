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
using Framework.IO;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using Game.PvP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.RequestAccountData, Status = SessionStatus.Authed)]
        void HandleRequestAccountData(RequestAccountData request)
        {
            if (request.DataType > AccountDataTypes.Max)
                return;

            AccountData adata = GetAccountData(request.DataType);
            if (adata.Data == null)
                return;

            UpdateAccountData data = new UpdateAccountData();
            data.Player = GetPlayer() ? GetPlayer().GetGUID() : ObjectGuid.Empty;
            data.Time = (uint)adata.Time;
            data.Size = (uint)adata.Data.Length;
            data.DataType = request.DataType;
            data.CompressedData = new ByteBuffer(ZLib.Compress(Encoding.UTF8.GetBytes(adata.Data)));

            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateAccountData, Status = SessionStatus.Authed)]
        void HandleUpdateAccountData(UserClientUpdateAccountData packet)
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

            byte[] data = ZLib.Decompress(packet.CompressedData.GetData(), packet.Size);
            SetAccountData(packet.DataType, packet.Time, Encoding.Default.GetString(data));
        }

        [WorldPacketHandler(ClientOpcodes.SetSelection)]
        void HandleSetSelection(SetSelection packet)
        {
            GetPlayer().SetSelection(packet.Selection);
        }

        [WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed)]
        void HandleObjectUpdateFailed(ObjectUpdateFailed objectUpdateFailed)
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
        void HandleObjectUpdateRescued(ObjectUpdateRescued objectUpdateRescued)
        {
            Log.outError(LogFilter.Network, "Object update rescued for {0} for player {1} ({2})", objectUpdateRescued.ObjectGUID.ToString(), GetPlayerName(), GetPlayer().GetGUID().ToString());

            // Client received values update after destroying object
            // re-register object in m_clientGUIDs to send DestroyObject on next visibility update
            GetPlayer().m_clientGUIDs.Add(objectUpdateRescued.ObjectGUID);
        }

        [WorldPacketHandler(ClientOpcodes.SetActionButton)]
        void HandleSetActionButton(SetActionButton packet)
        {
            uint action = packet.GetButtonAction();
            uint type = packet.GetButtonType();

            if (packet.Action == 0)
                GetPlayer().RemoveActionButton(packet.Index);
            else
                GetPlayer().AddActionButton(packet.Index, action, type);
        }

        [WorldPacketHandler(ClientOpcodes.SetActionBarToggles)]
        void HandleSetActionBarToggles(SetActionBarToggles packet)
        {
            if (!GetPlayer())                                        // ignore until not logged (check needed because STATUS_AUTHED)
            {
                if (packet.Mask != 0)
                    Log.outError(LogFilter.Network, "WorldSession.HandleSetActionBarToggles in not logged state with value: {0}, ignored", packet.Mask);
                return;
            }

            GetPlayer().SetByteValue(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetActionBarToggles, packet.Mask);
        }

        [WorldPacketHandler(ClientOpcodes.CompleteCinematic)]
        void HandleCompleteCinematic(CompleteCinematic packet)
        {    
            // If player has sight bound to visual waypoint NPC we should remove it
            GetPlayer().GetCinematicMgr().EndCinematic();
        }

        [WorldPacketHandler(ClientOpcodes.NextCinematicCamera)]
        void HandleNextCinematicCamera(NextCinematicCamera packet)
        {    
            // Sent by client when cinematic actually begun. So we begin the server side process
            GetPlayer().GetCinematicMgr().BeginCinematic();
        }

        [WorldPacketHandler(ClientOpcodes.CompleteMovie)]
        void HandleCompleteMovie(CompleteMovie packet)
        {
            uint movie = _player.GetMovie();
            if (movie == 0)
                return;

            _player.SetMovie(0);
            Global.ScriptMgr.OnMovieComplete(_player, movie);
        }

        [WorldPacketHandler(ClientOpcodes.ViolenceLevel, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
        void HandleViolenceLevel(ViolenceLevel violenceLevel)
        {
            // do something?
        }

        [WorldPacketHandler(ClientOpcodes.AreaTrigger)]
        void HandleAreaTrigger(AreaTriggerPkt packet)
        {
            Player player = GetPlayer();
            if (player.IsInFlight())
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTriggerOpcode: Player '{0}' (GUID: {1}) in flight, ignore Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            AreaTriggerRecord atEntry = CliDB.AreaTriggerStorage.LookupByKey(packet.AreaTriggerID);
            if (atEntry == null)
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTriggerOpcode: Player '{0}' (GUID: {1}) send unknown (by DBC) Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (packet.Entered && !player.IsInAreaTriggerRadius(atEntry))
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTriggerOpcode: Player '{0}' ({1}) too far, ignore Area Trigger ID: {2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (player.isDebugAreaTriggers)
                player.SendSysMessage(packet.Entered ? CypherStrings.DebugAreatriggerEntered : CypherStrings.DebugAreatriggerLeft, packet.AreaTriggerID);

            if (Global.ScriptMgr.OnAreaTrigger(player, atEntry, packet.Entered))
                return;

            if (player.IsAlive())
            {
                List<uint> quests = Global.ObjectMgr.GetQuestsForAreaTrigger(packet.AreaTriggerID);
                if (quests != null)
                {
                    foreach (uint questId in quests)
                    {
                        Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questId);
                        if (qInfo != null && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                        {
                            foreach (QuestObjective obj in qInfo.Objectives)
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
                    player.RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);

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

            bool teleported = false;
            if (player.GetMapId() != at.target_mapId)
            {
                EnterState denyReason = Global.MapMgr.PlayerCannotEnter(at.target_mapId, player, false);
                if (denyReason != 0)
                {
                    bool reviveAtTrigger = false; // should we revive the player if he is trying to enter the correct instance?
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
                                MapRecord entry = CliDB.MapStorage.LookupByKey(at.target_mapId);
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
                                MapRecord entry = CliDB.MapStorage.LookupByKey(at.target_mapId);
                                if (entry != null)
                                {
                                    string mapName = entry.MapName[player.GetSession().GetSessionDbcLocale()];
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

                Group group = player.GetGroup();
                if (group)
                    if (group.isLFGGroup() && player.GetMap().IsDungeon())
                        teleported = player.TeleportToBGEntryPoint();
            }

            if (!teleported)
            {
                WorldSafeLocsRecord entranceLocation = null;
                InstanceSave instanceSave = player.GetInstanceSave(at.target_mapId);
                if (instanceSave != null)
                {
                    // Check if we can contact the instancescript of the instance for an updated entrance location
                    Map map = Global.MapMgr.FindMap(at.target_mapId, player.GetInstanceSave(at.target_mapId).GetInstanceId());
                    if (map)
                    {
                        InstanceMap instanceMap = map.ToInstanceMap();
                        if (instanceMap != null)
                        {
                            InstanceScript instanceScript = instanceMap.GetInstanceScript();
                            if (instanceScript != null)
                                entranceLocation = CliDB.WorldSafeLocsStorage.LookupByKey(instanceScript.GetEntranceLocation());
                        }
                    }

                    // Finally check with the instancesave for an entrance location if we did not get a valid one from the instancescript
                    if (entranceLocation == null)
                        entranceLocation = CliDB.WorldSafeLocsStorage.LookupByKey(instanceSave.GetEntranceLocation());
                }

                if (entranceLocation != null)
                    player.TeleportTo(entranceLocation.MapID, entranceLocation.Loc.X, entranceLocation.Loc.Y, entranceLocation.Loc.Z, (float)(entranceLocation.Facing * Math.PI / 180), TeleportToOptions.NotLeaveTransport);
                else
                    player.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, at.target_Orientation, TeleportToOptions.NotLeaveTransport);
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestPlayedTime)]
        void HandlePlayedTime(RequestPlayedTime packet)
        {
            PlayedTime playedTime = new PlayedTime();
            playedTime.TotalTime = GetPlayer().GetTotalPlayedTime();
            playedTime.LevelTime = GetPlayer().GetLevelPlayedTime();
            playedTime.TriggerEvent = packet.TriggerScriptEvent;  // 0-1 - will not show in chat frame
            SendPacket(playedTime);
        }

        [WorldPacketHandler(ClientOpcodes.SaveCufProfiles, Processing = PacketProcessing.Inplace)]
        void HandleSaveCUFProfiles(SaveCUFProfiles packet)
        {
            if (packet.CUFProfiles.Count > PlayerConst.MaxCUFProfiles)
            {
                Log.outError(LogFilter.Player, "HandleSaveCUFProfiles - {0} tried to save more than {1} CUF profiles. Hacking attempt?", GetPlayerName(), PlayerConst.MaxCUFProfiles);
                return;
            }

            for (byte i = 0; i < packet.CUFProfiles.Count; ++i)
                GetPlayer().SaveCUFProfile(i, packet.CUFProfiles[i]);

            for (byte i = (byte)packet.CUFProfiles.Count; i < PlayerConst.MaxCUFProfiles; ++i)
                GetPlayer().SaveCUFProfile(i, null);
        }

        public void SendLoadCUFProfiles()
        {
            Player player = GetPlayer();

            LoadCUFProfiles loadCUFProfiles = new LoadCUFProfiles();

            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                CUFProfile cufProfile = player.GetCUFProfile(i);
                if (cufProfile != null)
                    loadCUFProfiles.CUFProfiles.Add(cufProfile);
            }

            SendPacket(loadCUFProfiles);
        }

        [WorldPacketHandler(ClientOpcodes.SetAdvancedCombatLogging, Processing = PacketProcessing.Inplace)]
        void HandleSetAdvancedCombatLogging(SetAdvancedCombatLogging setAdvancedCombatLogging)
        {
            GetPlayer().SetAdvancedCombatLogging(setAdvancedCombatLogging.Enable);
        }

        [WorldPacketHandler(ClientOpcodes.MountSpecialAnim)]
        void HandleMountSpecialAnim(MountSpecial mountSpecial)
        {
            SpecialMountAnim specialMountAnim = new SpecialMountAnim();
            specialMountAnim.UnitGUID = _player.GetGUID();
            GetPlayer().SendMessageToSet(specialMountAnim, false);
        }

        [WorldPacketHandler(ClientOpcodes.MountSetFavorite)]
        void HandleMountSetFavorite(MountSetFavorite mountSetFavorite)
        {
            _collectionMgr.MountSetFavorite(mountSetFavorite.MountSpellID, mountSetFavorite.IsFavorite);
        }

        [WorldPacketHandler(ClientOpcodes.CloseInteraction)]
        void HandleCloseInteraction(CloseInteraction closeInteraction)
        {
            if (_player.PlayerTalkClass.GetInteractionData().SourceGuid == closeInteraction.SourceGuid)
                _player.PlayerTalkClass.GetInteractionData().Reset();
        }

        [WorldPacketHandler(ClientOpcodes.ChatUnregisterAllAddonPrefixes)]
        void HandleUnregisterAllAddonPrefixes(ChatUnregisterAllAddonPrefixes packet)
        {
            _registeredAddonPrefixes.Clear();
        }

        [WorldPacketHandler(ClientOpcodes.ChatRegisterAddonPrefixes)]
        void HandleAddonRegisteredPrefixes(ChatRegisterAddonPrefixes packet)
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
        void HandleTogglePvP(TogglePvP packet)
        {
            Player player = GetPlayer();

            bool inPvP = player.HasFlag(PlayerFields.Flags, PlayerFlags.InPVP);
            player.ApplyModFlag(PlayerFields.Flags, PlayerFlags.InPVP, !inPvP);
            player.ApplyModFlag(PlayerFields.Flags, PlayerFlags.PVPTimer, inPvP);

            if (player.HasFlag(PlayerFields.Flags, PlayerFlags.InPVP))
            {
                if (!player.IsPvP() || player.pvpInfo.EndTimer != 0)
                    player.UpdatePvP(true, true);
            }
            else
            {
                if (!player.pvpInfo.IsHostile && player.IsPvP())
                    player.pvpInfo.EndTimer = Time.UnixTime;     // start toggle-off
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetPvp)]
        void HandleSetPvP(SetPvP packet)
        {
            Player player = GetPlayer();

            player.ApplyModFlag(PlayerFields.Flags, PlayerFlags.InPVP, packet.EnablePVP);
            player.ApplyModFlag(PlayerFields.Flags, PlayerFlags.PVPTimer, !packet.EnablePVP);

            if (player.HasFlag(PlayerFields.Flags, PlayerFlags.InPVP))
            {
                if (!player.IsPvP() || player.pvpInfo.EndTimer != 0)
                    player.UpdatePvP(true, true);
            }
            else
            {
                if (!player.pvpInfo.IsHostile && player.IsPvP())
                    player.pvpInfo.EndTimer = Time.UnixTime; // start set-off
            }
        }

        [WorldPacketHandler(ClientOpcodes.FarSight)]
        void HandleFarSight(FarSight farSight)
        {
            if (farSight.Enable)
            {
                Log.outDebug(LogFilter.Network, "Added FarSight {0} to player {1}", GetPlayer().GetUInt64Value(ActivePlayerFields.Farsight), GetPlayer().GetGUID().ToString());
                WorldObject target = GetPlayer().GetViewpoint();
                if (target)
                    GetPlayer().SetSeer(target);
                else
                    Log.outDebug(LogFilter.Network, "Player {0} (GUID: {1}) requests non-existing seer {2}", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetPlayer().GetUInt64Value(ActivePlayerFields.Farsight));
            }
            else
            {
                Log.outDebug(LogFilter.Network, "Player {0} set vision to self", GetPlayer().GetGUID().ToString());
                GetPlayer().SetSeer(GetPlayer());
            }

            GetPlayer().UpdateVisibilityForPlayer();
        }

        [WorldPacketHandler(ClientOpcodes.SetTitle)]
        void HandleSetTitle(SetTitle packet)
        {
            // -1 at none
            if (packet.TitleID > 0 && packet.TitleID < PlayerConst.MaxTitleIndex)
            {
                if (!GetPlayer().HasTitle((uint)packet.TitleID))
                    return;
            }
            else
                packet.TitleID = 0;

            GetPlayer().SetUInt32Value(PlayerFields.ChosenTitle, (uint)packet.TitleID);
        }

        [WorldPacketHandler(ClientOpcodes.ResetInstances)]
        void HandleResetInstances(ResetInstances packet)
        {
            Group group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                    group.ResetInstances(InstanceResetMethod.All, false, false, GetPlayer());
            }
            else
                GetPlayer().ResetInstances(InstanceResetMethod.All, false, false);
        }

        [WorldPacketHandler(ClientOpcodes.SetDungeonDifficulty)]
        void HandleSetDungeonDifficulty(SetDungeonDifficulty setDungeonDifficulty)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setDungeonDifficulty.DifficultyID);
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

            Difficulty difficultyID = (Difficulty)difficultyEntry.Id;
            if (difficultyID == GetPlayer().GetDungeonDifficultyID())
                return;

            // cannot reset while in an instance
            Map map = GetPlayer().GetMap();
            if (map && map.IsDungeon())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetDungeonDifficulty: player (Name: {0}, {1}) tried to reset the instance while player is inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            Group group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player groupGuy = refe.GetSource();
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
                    //_player->SendDungeonDifficulty(true);
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
        void HandleSetRaidDifficulty(SetRaidDifficulty setRaidDifficulty)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setRaidDifficulty.DifficultyID);
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

            Difficulty difficultyID = (Difficulty)difficultyEntry.Id;
            if (difficultyID == (setRaidDifficulty.Legacy != 0 ? GetPlayer().GetLegacyRaidDifficultyID() : GetPlayer().GetRaidDifficultyID()))
                return;

            // cannot reset while in an instance
            Map map = GetPlayer().GetMap();
            if (map && map.IsDungeon())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetRaidDifficulty: player (Name: {0}, {1} tried to reset the instance while inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            Group group = GetPlayer().GetGroup();
            if (group)
            {
                if (group.IsLeader(GetPlayer().GetGUID()))
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player groupGuy = refe.GetSource();
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
        void HandleSetTaxiBenchmark(SetTaxiBenchmarkMode packet)
        {
            _player.ApplyModFlag(PlayerFields.Flags, PlayerFlags.TaxiBenchmark, packet.Enable);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
        void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
        {
            Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId());
            if (guild)
                guild.GetAchievementMgr().SendAchievementInfo(GetPlayer(), setFocusedAchievement.AchievementID);
        }

        [WorldPacketHandler(ClientOpcodes.InstanceLockResponse)]
        void HandleInstanceLockResponse(InstanceLockResponse packet)
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

        [WorldPacketHandler(ClientOpcodes.WardenData)]
        void HandleWardenDataOpcode(WardenData packet)
        {
            if (_warden == null || packet.Data.GetSize() == 0)
                return;

            _warden.DecryptData(packet.Data.GetData());            
            WardenOpcodes opcode = (WardenOpcodes)packet.Data.ReadUInt8();

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
    }
}
