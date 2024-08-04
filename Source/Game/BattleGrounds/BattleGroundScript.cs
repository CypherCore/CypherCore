// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using System.Numerics;

namespace Game.BattleGrounds
{
    public class BattlegroundScript : ZoneScript
    {
        protected BattlegroundMap battlegroundMap;
        protected Battleground battleground;

        public BattlegroundScript(BattlegroundMap map)
        {
            battlegroundMap = map;
            battleground = map.GetBG();
        }

        public virtual Team GetPrematureWinner()
        {
            Team winner = Team.Other;
            if (battleground.GetPlayersCountByTeam(Team.Alliance) >= battleground.GetMinPlayersPerTeam())
                winner = Team.Alliance;
            else if (battleground.GetPlayersCountByTeam(Team.Horde) >= battleground.GetMinPlayersPerTeam())
                winner = Team.Horde;

            return winner;
        }

        public override void TriggerGameEvent(uint gameEventId, WorldObject source = null, WorldObject target = null)
        {
            ProcessEvent(target, gameEventId, source);
            GameEvents.TriggerForMap(gameEventId, battlegroundMap, source, target);
            foreach (var (playerGuid, _) in battleground.GetPlayers())
            {
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                if (player != null)
                    GameEvents.TriggerForPlayer(gameEventId, player);
            }
        }

        public void UpdateWorldState(int worldStateId, int value, bool hidden = false)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value, hidden, battlegroundMap);
        }

        public void UpdateWorldState(int worldStateId, bool value, bool hidden = false)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value ? 1 : 0, hidden, battlegroundMap);
        }

        public virtual void OnInit() { }
        public virtual void OnUpdate(uint diff) { }
        public virtual void OnPrepareStage1() { }
        public virtual void OnPrepareStage2() { }
        public virtual void OnPrepareStage3() { }
        public virtual void OnStart() { }
        public virtual void OnEnd(Team winner) { }
        public virtual void OnPlayerJoined(Player player, bool inBattleground) { }
        public virtual void OnPlayerLeft(Player player) { }
        public virtual void OnPlayerKilled(Player victim, Player killer) { }
        public virtual void OnUnitKilled(Creature victim, Unit killer) { }
    }

    public class ArenaScript : BattlegroundScript
    {
        public ArenaScript(BattlegroundMap map) : base(map) { }

        public GameObject CreateObject(uint entry, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3, GameObjectState goState = GameObjectState.Ready)
        {
            Quaternion rot = new(rotation0, rotation1, rotation2, rotation3);
            // Temporally add safety check for bad spawns and send log (object rotations need to be rechecked in sniff)
            if (rotation0 == 0 && rotation1 == 0 && rotation2 == 0 && rotation3 == 0)
            {
                Log.outDebug(LogFilter.Battleground, $"Battleground::AddObject: gameoobject [entry: {entry}] for BG (map: {battlegroundMap.GetId()}) has zeroed rotation fields, " +
                    "orientation used temporally, but please fix the spawn");

                rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            }

            // Must be created this way, adding to godatamap would add it to the base map of the instance
            // and when loading it (in go::LoadFromDB()), a new guid would be assigned to the object, and a new object would be created
            // So we must create it specific for this instance
            GameObject go = GameObject.CreateGameObject(entry, battlegroundMap, new Position(x, y, z, o), rot, 255, goState);
            if (go == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground::AddObject: cannot create gameobject (entry: {entry}) for BG (map: {battlegroundMap.GetId()}, instance id: {battleground.GetInstanceID()})!");
                return null;
            }

            if (!battlegroundMap.AddToMap(go))
            {
                go.Dispose();
                return null;
            }

            return go;
        }

        public Creature CreateCreature(uint entry, float x, float y, float z, float o)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground::AddCreature: creature template (entry: {entry}) does not exist for BG (map: {battlegroundMap.GetId()}, instance id: {battleground.GetInstanceID()})!");
                return null;
            }

            Position pos = new(x, y, z, o);

            Creature creature = Creature.CreateCreature(entry, battlegroundMap, pos);
            if (creature == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground::AddCreature: cannot create creature (entry: {entry}) for BG (map: {battlegroundMap.GetId()}, instance id: {battleground.GetInstanceID()})!");
                return null;
            }

            creature.SetHomePosition(pos);

            if (!battlegroundMap.AddToMap(creature))
            {
                creature.Dispose();
                return null;
            }

            return creature;
        }
    }
}