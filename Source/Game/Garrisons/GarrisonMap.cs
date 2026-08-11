// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.Garrisons
{
    class GarrisonMap : Map
    {
        public GarrisonMap(uint id, long expiry, uint instanceId, ObjectGuid owner) : base(id, expiry, instanceId, Difficulty.Normal)
        {
            _owner = owner;
            InitVisibilityDistance();
        }

        public override void LoadGridObjects(Grid grid)
        {
            base.LoadGridObjects(grid);

            GarrisonGridLoader loader = new(grid, this);
            loader.LoadN();
        }

        public Garrison GetGarrison()
        {
            if (_loadingPlayer != null)
                return _loadingPlayer.GetGarrison();

            Player owner = Global.ObjAccessor.FindConnectedPlayer(_owner);
            if (owner != null)
                return owner.GetGarrison();

            return null;
        }

        public override void InitVisibilityDistance()
        {
            //init visibility distance for instances
            m_VisibleDistance = WorldConfig.GetFloatValue(WorldCfg.MaxVisibilityDistanceInstance);
            m_VisibilityNotifyPeriod = WorldConfig.GetIntValue(WorldCfg.VisibilityNotifyPeriodInstance);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            if (player.GetGUID() == _owner)
                _loadingPlayer = player;

            bool result = base.AddPlayerToMap(player, initPlayer);

            if (player.GetGUID() == _owner)
                _loadingPlayer = null;

            return result;
        }

        ObjectGuid _owner;
        Player _loadingPlayer; // @workaround Player is not registered in ObjectAccessor during login
    }

    class GarrisonGridLoader : Notifier
    {
        public GarrisonGridLoader(Grid grid, GarrisonMap map)
        {
            i_grid = grid;
            i_map = map;
            i_garrison = map.GetGarrison();
        }

        public void LoadN()
        {
            if (i_garrison != null)
            {
                foreach (var plot in i_garrison.GetPlots())
                {
                    GameObject go = plot.CreateGameObject(i_map, i_garrison.GetFaction());
                    if (go == null)
                        continue;

                    ObjectGridLoaderBase.AddToMap(go, i_map, ref i_gameObjects);
                }
            }

            Log.outDebug(LogFilter.Maps, $"{i_gameObjects} GameObjects and {i_creatures} Creatures loaded for grid {i_grid.GetGridId()} on map {i_map.GetId()}");
        }

        Grid i_grid;
        GarrisonMap i_map;
        Garrison i_garrison;
        uint i_gameObjects;
        uint i_creatures;
    }
}
