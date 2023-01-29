// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Maps;

namespace Game.Entities
{
    public struct GameObjectValue
    {
        public transport Transport;

        public fishinghole FishingHole;

        public building Building;

        public capturePoint CapturePoint;

        //11 GAMEOBJECT_TYPE_TRANSPORT
        public struct transport
        {
            public uint PathProgress;
            public TransportAnimation AnimationInfo;
            public uint CurrentSeg;
            public List<uint> StopFrames;
            public uint StateUpdateTimer;
        }

        //25 GAMEOBJECT_TYPE_FISHINGHOLE
        public struct fishinghole
        {
            public uint MaxOpens;
        }

        //33 GAMEOBJECT_TYPE_DESTRUCTIBLE_BUILDING
        public struct building
        {
            public uint Health;
            public uint MaxHealth;
        }

        //42 GAMEOBJECT_TYPE_CAPTURE_POINT
        public struct capturePoint
        {
            public int LastTeamCapture;
            public BattlegroundCapturePointState State;
            public uint AssaultTimer;
        }
    }
}