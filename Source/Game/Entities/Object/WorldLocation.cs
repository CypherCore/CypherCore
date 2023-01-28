// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Maps;

namespace Game.Entities
{
    public class WorldLocation : Position
	{
		private uint _mapId;
		public ObjectCellMoveState MoveState { get; set; }

		public Position NewPosition { get; set; } = new();
		private Cell currentCell;

		public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0)
		{
			_mapId = mapId;
			Relocate(x, y, z, o);
		}

		public WorldLocation(uint mapId, Position pos)
		{
			_mapId = mapId;
			Relocate(pos);
		}

		public WorldLocation(WorldLocation loc)
		{
			_mapId = loc._mapId;
			Relocate(loc);
		}

		public WorldLocation(Position pos)
		{
			_mapId = 0xFFFFFFFF;
			Relocate(pos);
		}

		public void WorldRelocate(uint mapId, Position pos)
		{
			_mapId = mapId;
			Relocate(pos);
		}

		public void WorldRelocate(WorldLocation loc)
		{
			_mapId = loc._mapId;
			Relocate(loc);
		}

		public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
		{
			_mapId = mapId;
			Relocate(x, y, z, o);
		}

		public uint GetMapId()
		{
			return _mapId;
		}

		public void SetMapId(uint mapId)
		{
			_mapId = mapId;
		}

		public Cell GetCurrentCell()
		{
			if (currentCell == null)
				Log.outError(LogFilter.Server, "Calling currentCell  but its null");

			return currentCell;
		}

		public void SetCurrentCell(Cell cell)
		{
			currentCell = cell;
		}

		public void SetNewCellPosition(float x, float y, float z, float o)
		{
			MoveState = ObjectCellMoveState.Active;
			NewPosition.Relocate(x, y, z, o);
		}

		public WorldLocation GetWorldLocation()
		{
			return this;
		}

		public virtual string GetDebugInfo()
		{
			var mapEntry = CliDB.MapStorage.LookupByKey(_mapId);

			return $"MapID: {_mapId} Map Name: '{(mapEntry != null ? mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "<not found>")}' {base.ToString()}";
		}

		public override string ToString()
		{
			return $"X: {X} Y: {Y} Z: {Z} O: {Orientation} _mapId: {_mapId}";
		}
	}
}