// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.SceneTriggerEvent)]
        void HandleSceneTriggerEvent(SceneTriggerEvent sceneTriggerEvent)
        {
            Log.outDebug(LogFilter.Scenes, "HandleSceneTriggerEvent: SceneInstanceID: {0} Event: {1}", sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);

            GetPlayer().GetSceneMgr().OnSceneTrigger(sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);
        }

        [WorldPacketHandler(ClientOpcodes.ScenePlaybackComplete)]
        void HandleScenePlaybackComplete(ScenePlaybackComplete scenePlaybackComplete)
        {
            Log.outDebug(LogFilter.Scenes, "HandleScenePlaybackComplete: SceneInstanceID: {0}", scenePlaybackComplete.SceneInstanceID);

            GetPlayer().GetSceneMgr().OnSceneComplete(scenePlaybackComplete.SceneInstanceID);
        }

        [WorldPacketHandler(ClientOpcodes.ScenePlaybackCanceled)]
        void HandleScenePlaybackCanceled(ScenePlaybackCanceled scenePlaybackCanceled)
        {
            Log.outDebug(LogFilter.Scenes, "HandleScenePlaybackCanceled: SceneInstanceID: {0}", scenePlaybackCanceled.SceneInstanceID);

            GetPlayer().GetSceneMgr().OnSceneCancel(scenePlaybackCanceled.SceneInstanceID);
        }

    }
}
