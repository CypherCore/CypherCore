// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
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
        void HandleRequestAccountData(RequestAccountData request)
        {
            if (request.DataType > AccountDataTypes.Max)
                return;

            AccountData adata = GetAccountData(request.DataType);

            UpdateAccountData data = new();
            data.Player = GetPlayer() != null ? GetPlayer().GetGUID() : ObjectGuid.Empty;
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
        void HandleUpdateAccountData(UserClientUpdateAccountData packet)
        {
            if (packet.DataType >= AccountDataTypes.Max)
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

        [WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed, Processing = PacketProcessing.Inplace)]
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
            ulong action = packet.GetButtonAction();
            uint type = packet.GetButtonType();

            if (packet.Action == 0)
                GetPlayer().RemoveActionButton(packet.Index);
            else
                GetPlayer().AddActionButton(packet.Index, action, type);
        }

        [WorldPacketHandler(ClientOpcodes.SetActionBarToggles)]
        void HandleSetActionBarToggles(SetActionBarToggles packet)
        {
            if (GetPlayer() == null)                                        // ignore until not logged (check needed because STATUS_AUTHED)
            {
                if (packet.Mask != 0)
                    Log.outError(LogFilter.Network, "WorldSession.HandleSetActionBarToggles in not logged state with value: {0}, ignored", packet.Mask);
                return;
            }

            GetPlayer().SetMultiActionBars(packet.Mask);
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
            GetPlayer().GetCinematicMgr().NextCinematicCamera();
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

        [WorldPacketHandler(ClientOpcodes.AreaTrigger, Processing = PacketProcessing.Inplace)]
        void HandleAreaTrigger(AreaTriggerPkt packet)
        {
            Player player = GetPlayer();
            if (player.IsInFlight())
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' (GUID: {1}) in flight, ignore Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            AreaTriggerRecord atEntry = CliDB.AreaTriggerStorage.LookupByKey(packet.AreaTriggerID);
            if (atEntry == null)
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' (GUID: {1}) send unknown (by DBC) Area Trigger ID:{2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (packet.Entered != player.IsInAreaTrigger(atEntry))
            {
                Log.outDebug(LogFilter.Network, "HandleAreaTrigger: Player '{0}' ({1}) too far, ignore Area Trigger ID: {2}",
                    player.GetName(), player.GetGUID().ToString(), packet.AreaTriggerID);
                return;
            }

            if (player.IsDebugAreaTriggers)
                player.SendSysMessage(packet.Entered ? CypherStrings.DebugAreatriggerEntered : CypherStrings.DebugAreatriggerLeft, packet.AreaTriggerID);

            if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.AreatriggerClientTriggered, atEntry.Id, player))
                return;

            if (Global.ScriptMgr.OnAreaTrigger(player, atEntry, packet.Entered))
                return;

            if (atEntry.AreaTriggerActionSetID != 0)
            {
                if (packet.Entered)
                    player.UpdateCriteria(CriteriaType.EnterAreaTriggerWithActionSet, atEntry.AreaTriggerActionSetID);
                else
                    player.UpdateCriteria(CriteriaType.LeaveAreaTriggerWithActionSet, atEntry.AreaTriggerActionSetID);
            }

            if (player.IsAlive() && packet.Entered)
            {
                // not using Player.UpdateQuestObjectiveProgress, ObjectID in quest_objectives can be set to -1, areatrigger_involvedrelation then holds correct id
                List<uint> quests = Global.ObjectMgr.GetQuestsForAreaTrigger(packet.AreaTriggerID);
                if (quests != null)
                {
                    bool anyObjectiveChangedCompletionState = false;
                    foreach (uint questId in quests)
                    {
                        Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questId);
                        ushort slot = player.FindQuestSlot(questId);
                        if (qInfo != null && slot < SharedConst.MaxQuestLogSize && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                        {
                            foreach (QuestObjective obj in qInfo.Objectives)
                            {
                                if (obj.Type != QuestObjectiveType.AreaTrigger)
                                    continue;

                                if (!player.IsQuestObjectiveCompletable(slot, qInfo, obj))
                                    continue;

                                if (player.IsQuestObjectiveComplete(slot, qInfo, obj))
                                    continue;

                                if (obj.ObjectID != -1 && obj.ObjectID != packet.AreaTriggerID)
                                    continue;

                                player.SetQuestObjectiveData(obj, 1);
                                player.SendQuestUpdateAddCreditSimple(obj);
                                anyObjectiveChangedCompletionState = true;
                                break;
                            }

                            if (qInfo.HasFlag(QuestFlags.CompletionAreaTrigger))
                                player.AreaExploredOrEventHappens(questId);

                            if (player.CanCompleteQuest(questId))
                                player.CompleteQuest(questId);
                        }
                    }

                    if (anyObjectiveChangedCompletionState)
                        player.UpdateVisibleObjectInteractions(true, false, false, true);
                }
            }

            if (Global.ObjectMgr.IsTavernAreaTrigger(packet.AreaTriggerID))
            {
                // set resting flag we are in the inn
                if (packet.Entered)
                    player.GetRestMgr().SetInnTriggerID(atEntry.Id);
                else
                    player.GetRestMgr().RemoveRestFlag(RestFlag.Tavern);

                if (Global.WorldMgr.IsFFAPvPRealm())
                {
                    if (packet.Entered)
                        player.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
                    else
                        player.SetPvpFlag(UnitPVPStateFlags.FFAPvp);
                }

                return;
            }

            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null)
            {
                if (pvp.HandleAreaTrigger(player, packet.AreaTriggerID, packet.Entered))
                    return;
            }

            if (!packet.Entered)
                return;

            AreaTriggerStruct at = Global.ObjectMgr.GetAreaTrigger(packet.AreaTriggerID);
            if (at == null)
                return;

            bool teleported = false;
            if (player.GetMapId() != at.target_mapId)
            {
                if (!player.IsAlive())
                {
                    if (player.HasCorpse())
                    {
                        // let enter in ghost mode in instance that connected to inner instance with corpse
                        uint corpseMap = player.GetCorpseLocation().GetMapId();
                        do
                        {
                            if (corpseMap == at.target_mapId)
                                break;

                            InstanceTemplate corpseInstance = Global.ObjectMgr.GetInstanceTemplate(corpseMap);
                            corpseMap = corpseInstance != null ? corpseInstance.Parent : 0;
                        } while (corpseMap != 0);

                        if (corpseMap == 0)
                        {
                            SendPacket(new AreaTriggerNoCorpse());
                            return;
                        }

                        Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' has corpse in instance {at.target_mapId} and can enter.");
                    }
                    else
                        Log.outDebug(LogFilter.Maps, $"Map::CanPlayerEnter - player '{player.GetName()}' is dead but does not have a corpse!");
                }

                TransferAbortParams denyReason = Map.PlayerCannotEnter(at.target_mapId, player);
                if (denyReason != null)
                {
                    switch (denyReason.Reason)
                    {
                        case TransferAbortReason.MapNotAllowed:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' attempted to enter map with id {at.target_mapId} which has no entry");
                            break;
                        case TransferAbortReason.Difficulty:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' attempted to enter instance map {at.target_mapId} but the requested difficulty was not found");
                            break;
                        case TransferAbortReason.NeedGroup:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' must be in a raid group to enter map {at.target_mapId}");
                            player.SendRaidGroupOnlyMessage(RaidGroupReason.Only, 0);
                            break;
                        case TransferAbortReason.LockedToDifferentInstance:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because their permanent bind is incompatible with their group's");
                            break;
                        case TransferAbortReason.AlreadyCompletedEncounter:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because their permanent bind is incompatible with their group's");
                            break;
                        case TransferAbortReason.TooManyInstances:
                            Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' cannot enter instance map {1} because he has exceeded the maximum number of instances per hour.", player.GetName(), at.target_mapId);
                            break;
                        case TransferAbortReason.MaxPlayers:
                        case TransferAbortReason.ZoneInCombat:
                            break;
                        case TransferAbortReason.NotFound:
                            Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because instance is resetting.");
                            break;
                        default:
                            break;
                    }

                    if (denyReason.Reason != TransferAbortReason.NeedGroup)
                        player.SendTransferAborted(at.target_mapId, denyReason.Reason, denyReason.Arg, denyReason.MapDifficultyXConditionId);

                    if (!player.IsAlive() && player.HasCorpse())
                    {
                        if (player.GetCorpseLocation().GetMapId() == at.target_mapId)
                        {
                            player.ResurrectPlayer(0.5f);
                            player.SpawnCorpseBones();
                        }
                    }

                    return;
                }

                Group group = player.GetGroup();
                if (group != null)
                    if (group.IsLFGGroup() && player.GetMap().IsDungeon())
                        teleported = player.TeleportToBGEntryPoint();
            }

            if (!teleported)
            {
                WorldSafeLocsEntry entranceLocation = player.GetInstanceEntrance(at.target_mapId);
                if (entranceLocation != null && player.GetMapId() != at.target_mapId)
                    player.TeleportTo(entranceLocation.Loc, TeleportToOptions.NotLeaveTransport);
                else
                    player.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, at.target_Orientation, TeleportToOptions.NotLeaveTransport);
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestPlayedTime, Processing = PacketProcessing.Inplace)]
        void HandlePlayedTime(RequestPlayedTime packet)
        {
            PlayedTime playedTime = new();
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

            LoadCUFProfiles loadCUFProfiles = new();

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
            SpecialMountAnim specialMountAnim = new();
            specialMountAnim.UnitGUID = _player.GetGUID();
            specialMountAnim.SpellVisualKitIDs.AddRange(mountSpecial.SpellVisualKitIDs);
            specialMountAnim.SequenceVariation = mountSpecial.SequenceVariation;
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

            if (_player.GetStableMaster() == closeInteraction.SourceGuid)
                _player.SetStableMaster(ObjectGuid.Empty);
        }

        [WorldPacketHandler(ClientOpcodes.ConversationLineStarted)]
        void HandleConversationLineStarted(ConversationLineStarted conversationLineStarted)
        {
            Conversation conversation = ObjectAccessor.GetConversation(_player, conversationLineStarted.ConversationGUID);
            if (conversation != null)
                conversation.GetAI().OnLineStarted(conversationLineStarted.LineID, _player);
        }

        [WorldPacketHandler(ClientOpcodes.RequestLatestSplashScreen)]
        void HandleRequestLatestSplashScreen(RequestLatestSplashScreen requestLatestSplashScreen)
        {
            UISplashScreenRecord splashScreen = null;
            foreach (var itr in CliDB.UISplashScreenStorage.Values)
            {
                if (!ConditionManager.IsPlayerMeetingCondition(_player, (uint)itr.CharLevelConditionID))
                    continue;

                splashScreen = itr;
            }

            SplashScreenShowLatest splashScreenShowLatest = new();
            splashScreenShowLatest.UISplashScreenID = splashScreen != null ? splashScreen.Id : 0;
            SendPacket(splashScreenShowLatest);
        }

        [WorldPacketHandler(ClientOpcodes.QueryCountdownTimer, Processing = PacketProcessing.Inplace)]
        void HandleQueryCountdownTimer(QueryCountdownTimer queryCountdownTimer)
        {
            Group group = _player.GetGroup();
            if (group == null)
                return;

            CountdownInfo info = group.GetCountdownInfo(queryCountdownTimer.TimerType);
            if (info == null)
                return;

            StartTimer startTimer = new();
            startTimer.Type = queryCountdownTimer.TimerType;
            startTimer.TimeLeft = info.GetTimeLeft();
            startTimer.TotalTime = info.GetTotalTime();

            _player.SendPacket(startTimer);
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
            if (!GetPlayer().HasPlayerFlag(PlayerFlags.InPVP))
            {
                GetPlayer().SetPlayerFlag(PlayerFlags.InPVP);
                GetPlayer().RemovePlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().IsPvP() || GetPlayer().pvpInfo.EndTimer != 0)
                    GetPlayer().UpdatePvP(true, true);
            }
            else if (!GetPlayer().IsWarModeLocalActive())
            {
                GetPlayer().RemovePlayerFlag(PlayerFlags.InPVP);
                GetPlayer().SetPlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().pvpInfo.IsHostile && GetPlayer().IsPvP())
                    GetPlayer().pvpInfo.EndTimer = GameTime.GetGameTime(); // start toggle-off
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetPvp)]
        void HandleSetPvP(SetPvP packet)
        {
            if (packet.EnablePVP)
            {
                GetPlayer().SetPlayerFlag(PlayerFlags.InPVP);
                GetPlayer().RemovePlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().IsPvP() || GetPlayer().pvpInfo.EndTimer != 0)
                    GetPlayer().UpdatePvP(true, true);
            }
            else if (!GetPlayer().IsWarModeLocalActive())
            {
                GetPlayer().RemovePlayerFlag(PlayerFlags.InPVP);
                GetPlayer().SetPlayerFlag(PlayerFlags.PVPTimer);
                if (!GetPlayer().pvpInfo.IsHostile && GetPlayer().IsPvP())
                    GetPlayer().pvpInfo.EndTimer = GameTime.GetGameTime(); // start toggle-off
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetWarMode)]
        void HandleSetWarMode(SetWarMode packet)
        {
            _player.SetWarModeDesired(packet.Enable);
        }

        [WorldPacketHandler(ClientOpcodes.FarSight)]
        void HandleFarSight(FarSight farSight)
        {
            if (farSight.Enable)
            {
                Log.outDebug(LogFilter.Network, "Added FarSight {0} to player {1}", GetPlayer().m_activePlayerData.FarsightObject.ToString(), GetPlayer().GetGUID().ToString());
                WorldObject target = GetPlayer().GetViewpoint();
                if (target != null)
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

        [WorldPacketHandler(ClientOpcodes.SetTitle, Processing = PacketProcessing.Inplace)]
        void HandleSetTitle(SetTitle packet)
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
        void HandleResetInstances(ResetInstances packet)
        {
            Map map = _player.GetMap();
            if (map != null && map.Instanceable())
                return;

            Group group = GetPlayer().GetGroup();
            if (group != null)
            {
                if (!group.IsLeader(GetPlayer().GetGUID()))
                    return;

                if (group.IsLFGGroup())
                    return;

                group.ResetInstances(InstanceResetMethod.Manual, _player);
            }
            else
                GetPlayer().ResetInstances(InstanceResetMethod.Manual);
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

            if (!difficultyEntry.HasFlag(DifficultyFlags.CanSelect))
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
            if (map != null && map.Instanceable())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetDungeonDifficulty: player (Name: {0}, {1}) tried to reset the instance while player is inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            Group group = GetPlayer().GetGroup();
            if (group != null)
            {
                if (!group.IsLeader(_player.GetGUID()))
                    return;

                if (group.IsLFGGroup())
                    return;

                // the difficulty is set even if the instances can't be reset
                group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _player);
                group.SetDungeonDifficultyID(difficultyID);
            }
            else
            {
                GetPlayer().ResetInstances(InstanceResetMethod.OnChangeDifficulty);
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

            if (!difficultyEntry.HasFlag(DifficultyFlags.CanSelect))
            {
                Log.outDebug(LogFilter.Network, "WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
                    GetPlayer().GetGUID().ToString(), difficultyEntry.Id);
                return;
            }

            if (difficultyEntry.HasFlag(DifficultyFlags.Legacy) != (setRaidDifficulty.Legacy != 0))
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
            if (map != null && map.Instanceable())
            {
                Log.outDebug(LogFilter.Network, "WorldSession:HandleSetRaidDifficulty: player (Name: {0}, {1} tried to reset the instance while inside!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            Group group = GetPlayer().GetGroup();
            if (group != null)
            {
                if (!group.IsLeader(_player.GetGUID()))
                    return;

                if (group.IsLFGGroup())
                    return;

                // the difficulty is set even if the instances can't be reset
                group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _player);
                if (setRaidDifficulty.Legacy != 0)
                    group.SetLegacyRaidDifficultyID(difficultyID);
                else
                    group.SetRaidDifficultyID(difficultyID);
            }
            else
            {
                GetPlayer().ResetInstances(InstanceResetMethod.OnChangeDifficulty);
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
            if (packet.Enable)
                _player.SetPlayerFlag(PlayerFlags.TaxiBenchmark);
            else
                _player.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
        void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
        {
            Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId());
            if (guild != null)
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
                GetPlayer().ConfirmPendingBind();
            else
                GetPlayer().RepopAtGraveyard();

            GetPlayer().SetPendingBind(0, 0);
        }

        [WorldPacketHandler(ClientOpcodes.Warden3Data)]
        void HandleWarden3Data(WardenData packet)
        {
            if (_warden == null || packet.Data.GetSize() == 0)
                return;

            _warden.DecryptData(packet.Data.GetData());
            WardenOpcodes opcode = (WardenOpcodes)packet.Data.ReadUInt8();

            switch (opcode)
            {
                case WardenOpcodes.CmsgModuleMissing:
                    _warden.SendModuleToClient();
                    break;
                case WardenOpcodes.CmsgModuleOk:
                    _warden.RequestHash();
                    break;
                case WardenOpcodes.SmsgCheatChecksRequest:
                    _warden.HandleData(packet.Data);
                    break;
                case WardenOpcodes.CmsgMemChecksResult:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");
                    break;
                case WardenOpcodes.CmsgHashResult:
                    _warden.HandleHashResult(packet.Data);
                    _warden.InitializeModule();
                    break;
                case WardenOpcodes.CmsgModuleFailed:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MODULE_FAILED received!");
                    break;
                default:
                    Log.outDebug(LogFilter.Warden, "Got unknown warden opcode {0} of size {1}.", opcode, packet.Data.GetSize() - 1);
                    break;
            }
        }
    }
}
