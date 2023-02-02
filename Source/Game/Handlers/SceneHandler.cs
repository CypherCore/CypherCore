// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
