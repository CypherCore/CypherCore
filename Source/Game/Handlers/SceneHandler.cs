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
using Game.Network;
using Game.Network.Packets;

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
