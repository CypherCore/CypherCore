// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.Garrisons
{
    internal class GarrisonMap : Map
    {
        private Player _loadingPlayer; // @workaround Player is not registered in ObjectAccessor during login

        private ObjectGuid _owner;

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
            _VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInInstances();
            _VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInInstances();
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
    }

    internal class GarrisonGridLoader : Notifier
    {
        private readonly Cell i_cell;
        private readonly uint i_creatures;
        private readonly Garrison i_garrison;
        private readonly Grid i_grid;
        private readonly GarrisonMap i_map;
        private uint i_gameObjects;

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

                    if (!go)
                        continue;

                    var cell = new Cell(cellCoord);
                    i_map.AddToGrid(go, cell);
                    go.AddToWorld();
                    ++i_gameObjects;
                }
            }
        }

        public override void Visit(IList<Creature> objs)
        {
        }
    }
}