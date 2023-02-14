// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Maps.Interfaces;
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
            if (_loadingPlayer)
                return _loadingPlayer.GetGarrison();

            Player owner = Global.ObjAccessor.FindConnectedPlayer(_owner);
            if (owner)
                return owner.GetGarrison();

            return null;
        }

        public override void InitVisibilityDistance()
        {
            //init visibility distance for instances
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInInstances();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInInstances();
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

    class GarrisonGridLoader : IGridNotifierGameObject
    {
        public GridType GridType { get; set; }
        public GarrisonGridLoader(Grid grid, GarrisonMap map, Cell cell, GridType gridType = GridType.Grid)
        {
            i_cell = cell;
            i_grid = grid;
            i_map = map;
            i_garrison = map.GetGarrison();
            GridType = gridType;
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
                        i_grid.VisitGrid(x, y, this);
                    }
                }
            }

            Log.outDebug(LogFilter.Maps, "{0} GameObjects and {1} Creatures loaded for grid {2} on map {3}", i_gameObjects, i_creatures, i_grid.GetGridId(), i_map.GetId());
        }

        public void Visit(IList<GameObject> objs)
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
                    if (!go)
                        continue;

                    var cell = new Cell(cellCoord);
                    i_map.AddToGrid(go, cell);
                    go.AddToWorld();
                    ++i_gameObjects;
                }
            }
        }

        Cell i_cell;
        Grid i_grid;
        GarrisonMap i_map;
        Garrison i_garrison;
        uint i_gameObjects;
        uint i_creatures;
    }
}
