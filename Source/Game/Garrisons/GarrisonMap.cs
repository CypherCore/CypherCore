// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;

namespace Game.Garrisons
{
    class GarrisonMap : Map
    {
        public GarrisonMap(uint id, long expiry, uint instanceId, ObjectGuid owner) : base(id, expiry, instanceId, Difficulty.Normal)
        {
            _owner = owner;
            InitVisibilityDistance();
        }

        public override void LoadGridObjects(Grid grid, Cell cell)
        {
            base.LoadGridObjects(grid, cell);

            GarrisonGridLoader loader = new(grid, this, cell);
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
        public GarrisonGridLoader(Grid grid, GarrisonMap map, Cell cell)
        {
            i_cell = cell;
            i_grid = grid;
            i_map = map;
            i_garrison = map.GetGarrison();
        }

        public void LoadN()
        {
            if (i_garrison != null)
            {
                i_cell.data.cell_y = 0;
                for (uint x = 0; x < MapConst.MaxCells; ++x)
                {
                    i_cell.data.cell_x = x;
                    for (uint y = 0; y < MapConst.MaxCells; ++y)
                    {
                        i_cell.data.cell_y = y;

                        //Load creatures and game objects
                        var visitor = new Visitor(this, GridMapTypeMask.AllGrid);
                        i_grid.VisitGrid(x, y, visitor);
                    }
                }
            }

            Log.outDebug(LogFilter.Maps, "{0} GameObjects and {1} Creatures loaded for grid {2} on map {3}", i_gameObjects, i_creatures, i_grid.GetGridId(), i_map.GetId());
        }

        public override void Visit(IList<GameObject> objs)
        {
            ICollection<Garrison.Plot> plots = i_garrison.GetPlots();
            if (!plots.Empty())
            {
                CellCoord cellCoord = i_cell.GetCellCoord();
                foreach (Garrison.Plot plot in plots)
                {
                    Position spawn = plot.PacketInfo.PlotPos;
                    if (cellCoord != GridDefines.ComputeCellCoord(spawn.GetPositionX(), spawn.GetPositionY()))
                        continue;

                    GameObject go = plot.CreateGameObject(i_map, i_garrison.GetFaction());
                    if (go == null)
                        continue;

                    var cell = new Cell(cellCoord);
                    i_map.AddToGrid(go, cell);
                    go.AddToWorld();
                    ++i_gameObjects;
                }
            }
        }

        public override void Visit(IList<Creature> objs) { }

        Cell i_cell;
        Grid i_grid;
        GarrisonMap i_map;
        Garrison i_garrison;
        uint i_gameObjects;
        uint i_creatures;
    }
}
