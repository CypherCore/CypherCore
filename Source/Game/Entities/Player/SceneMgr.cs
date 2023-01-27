// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IScene;

namespace Game.Entities
{
	public class SceneMgr
	{
		private List<ServerPacket> _delayedScenes = new();
		private bool _isDebuggingScenes;
		private Player _player;
		private Dictionary<uint, SceneTemplate> _scenesByInstance = new();
		private uint _standaloneSceneInstanceID;

		public SceneMgr(Player player)
		{
			_player                    = player;
			_standaloneSceneInstanceID = 0;
			_isDebuggingScenes         = false;
		}

		public uint PlayScene(uint sceneId, Position position = null)
		{
			SceneTemplate sceneTemplate = Global.ObjectMgr.GetSceneTemplate(sceneId);

			return PlaySceneByTemplate(sceneTemplate, position);
		}

		public uint PlaySceneByTemplate(SceneTemplate sceneTemplate, Position position = null)
		{
			if (sceneTemplate == null)
				return 0;

			SceneScriptPackageRecord entry = CliDB.SceneScriptPackageStorage.LookupByKey(sceneTemplate.ScenePackageId);

			if (entry == null)
				return 0;

			// By default, take player position
			if (position == null)
				position = GetPlayer();

			uint sceneInstanceID = GetNewStandaloneSceneInstanceID();

			if (_isDebuggingScenes)
				GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugPlay, sceneInstanceID, sceneTemplate.ScenePackageId, sceneTemplate.PlaybackFlags);

			PlayScene playScene = new();
			playScene.SceneID              = sceneTemplate.SceneId;
			playScene.PlaybackFlags        = (uint)sceneTemplate.PlaybackFlags;
			playScene.SceneInstanceID      = sceneInstanceID;
			playScene.SceneScriptPackageID = sceneTemplate.ScenePackageId;
			playScene.Location             = position;
			playScene.TransportGUID        = GetPlayer().GetTransGUID();
			playScene.Encrypted            = sceneTemplate.Encrypted;
			playScene.Write();

			if (GetPlayer().IsInWorld)
				GetPlayer().SendPacket(playScene);
			else
				_delayedScenes.Add(playScene);

			AddInstanceIdToSceneMap(sceneInstanceID, sceneTemplate);

			Global.ScriptMgr.RunScript<ISceneOnSceneStart>(script => script.OnSceneStart(GetPlayer(), sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);

			return sceneInstanceID;
		}

		public uint PlaySceneByPackageId(uint sceneScriptPackageId, SceneFlags playbackflags, Position position = null)
		{
			SceneTemplate sceneTemplate = new();
			sceneTemplate.SceneId        = 0;
			sceneTemplate.ScenePackageId = sceneScriptPackageId;
			sceneTemplate.PlaybackFlags  = playbackflags;
			sceneTemplate.Encrypted      = false;
			sceneTemplate.ScriptId       = 0;

			return PlaySceneByTemplate(sceneTemplate, position);
		}

		private void CancelScene(uint sceneInstanceID, bool removeFromMap = true)
		{
			if (removeFromMap)
				RemoveSceneInstanceId(sceneInstanceID);

			CancelScene cancelScene = new();
			cancelScene.SceneInstanceID = sceneInstanceID;
			GetPlayer().SendPacket(cancelScene);
		}

		public void OnSceneTrigger(uint sceneInstanceID, string triggerName)
		{
			if (!HasScene(sceneInstanceID))
				return;

			if (_isDebuggingScenes)
				GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugTrigger, sceneInstanceID, triggerName);

			SceneTemplate sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);
			Global.ScriptMgr.RunScript<ISceneOnSceneTrigger>(script => script.OnSceneTriggerEvent(GetPlayer(), sceneInstanceID, sceneTemplate, triggerName), sceneTemplate.ScriptId);
		}

		public void OnSceneCancel(uint sceneInstanceID)
		{
			if (!HasScene(sceneInstanceID))
				return;

			if (_isDebuggingScenes)
				GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugCancel, sceneInstanceID);

			SceneTemplate sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);

			if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.NotCancelable))
				return;

			// Must be done before removing aura
			RemoveSceneInstanceId(sceneInstanceID);

			if (sceneTemplate.SceneId != 0)
				RemoveAurasDueToSceneId(sceneTemplate.SceneId);

			Global.ScriptMgr.RunScript<ISceneOnSceneChancel>(script => script.OnSceneCancel(GetPlayer(), sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);

			if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.FadeToBlackscreenOnCancel))
				CancelScene(sceneInstanceID, false);
		}

		public void OnSceneComplete(uint sceneInstanceID)
		{
			if (!HasScene(sceneInstanceID))
				return;

			if (_isDebuggingScenes)
				GetPlayer().SendSysMessage(CypherStrings.CommandSceneDebugComplete, sceneInstanceID);

			SceneTemplate sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceID);

			// Must be done before removing aura
			RemoveSceneInstanceId(sceneInstanceID);

			if (sceneTemplate.SceneId != 0)
				RemoveAurasDueToSceneId(sceneTemplate.SceneId);

			Global.ScriptMgr.RunScript<ISceneOnSceneComplete>(script => script.OnSceneComplete(GetPlayer(), sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);

			if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.FadeToBlackscreenOnComplete))
				CancelScene(sceneInstanceID, false);
		}

		private bool HasScene(uint sceneInstanceID, uint sceneScriptPackageId = 0)
		{
			var sceneTempalte = _scenesByInstance.LookupByKey(sceneInstanceID);

			if (sceneTempalte != null)
				return sceneScriptPackageId == 0 || sceneScriptPackageId == sceneTempalte.ScenePackageId;

			return false;
		}

		private void AddInstanceIdToSceneMap(uint sceneInstanceID, SceneTemplate sceneTemplate)
		{
			_scenesByInstance[sceneInstanceID] = sceneTemplate;
		}

		public void CancelSceneBySceneId(uint sceneId)
		{
			List<uint> instancesIds = new();

			foreach (var pair in _scenesByInstance)
				if (pair.Value.SceneId == sceneId)
					instancesIds.Add(pair.Key);

			foreach (uint sceneInstanceID in instancesIds)
				CancelScene(sceneInstanceID);
		}

		public void CancelSceneByPackageId(uint sceneScriptPackageId)
		{
			List<uint> instancesIds = new();

			foreach (var sceneTemplate in _scenesByInstance)
				if (sceneTemplate.Value.ScenePackageId == sceneScriptPackageId)
					instancesIds.Add(sceneTemplate.Key);

			foreach (uint sceneInstanceID in instancesIds)
				CancelScene(sceneInstanceID);
		}

		private void RemoveSceneInstanceId(uint sceneInstanceID)
		{
			_scenesByInstance.Remove(sceneInstanceID);
		}

		private void RemoveAurasDueToSceneId(uint sceneId)
		{
			var scenePlayAuras = GetPlayer().GetAuraEffectsByType(AuraType.PlayScene);

			foreach (var scenePlayAura in scenePlayAuras)
				if (scenePlayAura.GetMiscValue() == sceneId)
				{
					GetPlayer().RemoveAura(scenePlayAura.GetBase());

					break;
				}
		}

		private SceneTemplate GetSceneTemplateFromInstanceId(uint sceneInstanceID)
		{
			return _scenesByInstance.LookupByKey(sceneInstanceID);
		}

		public uint GetActiveSceneCount(uint sceneScriptPackageId = 0)
		{
			uint activeSceneCount = 0;

			foreach (var sceneTemplate in _scenesByInstance.Values)
				if (sceneScriptPackageId == 0 ||
				    sceneTemplate.ScenePackageId == sceneScriptPackageId)
					++activeSceneCount;

			return activeSceneCount;
		}

		public void TriggerDelayedScenes()
		{
			foreach (var playScene in _delayedScenes)
				GetPlayer().SendPacket(playScene);

			_delayedScenes.Clear();
		}

		private Player GetPlayer()
		{
			return _player;
		}

		private void RecreateScene(uint sceneScriptPackageId, SceneFlags playbackflags, Position position = null)
		{
			CancelSceneByPackageId(sceneScriptPackageId);
			PlaySceneByPackageId(sceneScriptPackageId, playbackflags, position);
		}

		public Dictionary<uint, SceneTemplate> GetSceneTemplateByInstanceMap()
		{
			return _scenesByInstance;
		}

		private uint GetNewStandaloneSceneInstanceID()
		{
			return ++_standaloneSceneInstanceID;
		}

		public void ToggleDebugSceneMode()
		{
			_isDebuggingScenes = !_isDebuggingScenes;
		}

		public bool IsInDebugSceneMode()
		{
			return _isDebuggingScenes;
		}
	}
}