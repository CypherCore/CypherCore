﻿/*
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
using Game.DataStorage;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class SceneMgr
    {
        Player _player;
        Dictionary<uint, SceneTemplate> _scenesByInstance = new Dictionary<uint, SceneTemplate>();
        uint _standaloneSceneInstanceID;
        bool _isDebuggingScenes;

        public SceneMgr(Player player)
        {
            _player = player;
            _standaloneSceneInstanceID = 0;
            _isDebuggingScenes = false;
        }

        public uint PlayScene(uint sceneId, Position position = null)
        {
            var sceneTemplate = Global.ObjectMgr.GetSceneTemplate(sceneId);
            return PlaySceneByTemplate(sceneTemplate, position);
        }

        public uint PlaySceneByTemplate(SceneTemplate sceneTemplate, Position position = null)
        {
            if (sceneTemplate == null)
                return 0;

            var entry = CliDB.SceneScriptPackageStorage.LookupByKey(sceneTemplate.ScenePackageId);
            if (entry == null)
                return 0;

            // By default, take player position
            if (position == null)
                position = GetPlayer();

            var sceneInstanceID = GetNewStandaloneSceneInstanceID();

            if (_isDebuggingScenes)
                GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugPlay, sceneInstanceID, sceneTemplate.ScenePackageId, sceneTemplate.PlaybackFlags);

            var playScene = new PlayScene();
            playScene.SceneID = sceneTemplate.SceneId;
            playScene.PlaybackFlags = (uint)sceneTemplate.PlaybackFlags;
            playScene.SceneInstanceID = sceneInstanceID;
            playScene.SceneScriptPackageID = sceneTemplate.ScenePackageId;
            playScene.Location = position;
            playScene.TransportGUID = GetPlayer().GetTransGUID();
            playScene.Encrypted = sceneTemplate.Encrypted;

            GetPlayer().SendPacket(playScene);

            AddInstanceIdToSceneMap(sceneInstanceID, sceneTemplate);

            Global.ScriptMgr.OnSceneStart(GetPlayer(), sceneInstanceID, sceneTemplate);

            return sceneInstanceID;
        }

        public uint PlaySceneByPackageId(uint sceneScriptPackageId, SceneFlags playbackflags = SceneFlags.Unk16, Position position = null)
        {
            var sceneTemplate = new SceneTemplate();
            sceneTemplate.SceneId = 0;
            sceneTemplate.ScenePackageId = sceneScriptPackageId;
            sceneTemplate.PlaybackFlags = playbackflags;
            sceneTemplate.Encrypted = false;
            sceneTemplate.ScriptId = 0;

            return PlaySceneByTemplate(sceneTemplate, position);
        }

        void CancelScene(uint sceneInstanceID, bool removeFromMap = true)
        {
            if (removeFromMap)
                RemoveSceneInstanceId(sceneInstanceID);

            var cancelScene = new CancelScene();
            cancelScene.SceneInstanceID = sceneInstanceID;
            GetPlayer().SendPacket(cancelScene);
        }

        public void OnSceneTrigger(uint sceneInstanceID, string triggerName)
        {
            if (!HasScene(sceneInstanceID))
                return;

            if (_isDebuggingScenes)
                GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugTrigger, sceneInstanceID, triggerName);

            var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);
            Global.ScriptMgr.OnSceneTrigger(GetPlayer(), sceneInstanceID, sceneTemplate, triggerName);
        }

        public void OnSceneCancel(uint sceneInstanceID)
        {
            if (!HasScene(sceneInstanceID))
                return;

            if (_isDebuggingScenes)
                GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugCancel, sceneInstanceID);

            var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);

            // Must be done before removing aura
            RemoveSceneInstanceId(sceneInstanceID);

            if (sceneTemplate.SceneId != 0)
                RemoveAurasDueToSceneId(sceneTemplate.SceneId);

            Global.ScriptMgr.OnSceneCancel(GetPlayer(), sceneInstanceID, sceneTemplate);

            if (sceneTemplate.PlaybackFlags.HasAnyFlag(SceneFlags.CancelAtEnd))
                CancelScene(sceneInstanceID, false);
        }

        public void OnSceneComplete(uint sceneInstanceID)
        {
            if (!HasScene(sceneInstanceID))
                return;

            if (_isDebuggingScenes)
                GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugComplete, sceneInstanceID);

            var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);

            // Must be done before removing aura
            RemoveSceneInstanceId(sceneInstanceID);

            if (sceneTemplate.SceneId != 0)
                RemoveAurasDueToSceneId(sceneTemplate.SceneId);

            Global.ScriptMgr.OnSceneComplete(GetPlayer(), sceneInstanceID, sceneTemplate);

            if (sceneTemplate.PlaybackFlags.HasAnyFlag(SceneFlags.CancelAtEnd))
                CancelScene(sceneInstanceID, false);
        }

        bool HasScene(uint sceneInstanceID, uint sceneScriptPackageId = 0)
        {
            var sceneTempalte = _scenesByInstance.LookupByKey(sceneInstanceID);

            if (sceneTempalte != null)
                return sceneScriptPackageId == 0 || sceneScriptPackageId == sceneTempalte.ScenePackageId;

            return false;
        }

        void AddInstanceIdToSceneMap(uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            _scenesByInstance[sceneInstanceID] = sceneTemplate;
        }

        public void CancelSceneBySceneId(uint sceneId)
        {
            var instancesIds = new List<uint>();

            foreach (var pair in _scenesByInstance)
                if (pair.Value.SceneId == sceneId)
                    instancesIds.Add(pair.Key);

            foreach (var sceneInstanceID in instancesIds)
                CancelScene(sceneInstanceID);
        }

        public void CancelSceneByPackageId(uint sceneScriptPackageId)
        {
            var instancesIds = new List<uint>();

            foreach (var sceneTemplate in _scenesByInstance)
                if (sceneTemplate.Value.ScenePackageId == sceneScriptPackageId)
                    instancesIds.Add(sceneTemplate.Key);

            foreach (var sceneInstanceID in instancesIds)
                CancelScene(sceneInstanceID);
        }

        void RemoveSceneInstanceId(uint sceneInstanceID)
        {
            _scenesByInstance.Remove(sceneInstanceID);
        }

        void RemoveAurasDueToSceneId(uint sceneId)
        {
            var scenePlayAuras = GetPlayer().GetAuraEffectsByType(AuraType.PlayScene);
            foreach (var scenePlayAura in scenePlayAuras)
            {
                if (scenePlayAura.GetMiscValue() == sceneId)
                {
                    GetPlayer().RemoveAura(scenePlayAura.GetBase());
                    break;
                }
            }
        }

        SceneTemplate GetSceneTemplateFromInstanceId(uint sceneInstanceID)
        {
            return _scenesByInstance.LookupByKey(sceneInstanceID);
        }

        public uint GetActiveSceneCount(uint sceneScriptPackageId = 0)
        {
            uint activeSceneCount = 0;

            foreach (var sceneTemplate in _scenesByInstance.Values)
                if (sceneScriptPackageId == 0 || sceneTemplate.ScenePackageId == sceneScriptPackageId)
                    ++activeSceneCount;

            return activeSceneCount;
        }

        Player GetPlayer() { return _player; }

        void RecreateScene(uint sceneScriptPackageId, SceneFlags playbackflags = SceneFlags.Unk16, Position position = null)
        {
            CancelSceneByPackageId(sceneScriptPackageId);
            PlaySceneByPackageId(sceneScriptPackageId, playbackflags, position);
        }

        public Dictionary<uint, SceneTemplate> GetSceneTemplateByInstanceMap() { return _scenesByInstance; }

        uint GetNewStandaloneSceneInstanceID() { return ++_standaloneSceneInstanceID; }

        public void ToggleDebugSceneMode() { _isDebuggingScenes = !_isDebuggingScenes; }
        public bool IsInDebugSceneMode() { return _isDebuggingScenes; }
    }
}
