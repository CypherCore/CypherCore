/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Maps
{
    public class SpawnGroupTemplateData
    {
        public uint groupId;
        public string name;
        public uint mapId;
        public SpawnGroupFlags flags;
        public bool isActive;
    }

    public class SpawnData
    {
        public SpawnObjectType type;
        public ulong spawnId;
        public uint Id; // entry in respective _template table
        public WorldLocation spawnPoint;
        public PhaseUseFlagsValues phaseUseFlags;
        public uint phaseId;
        public uint phaseGroup;
        public int terrainSwapMap = -1;
        public int spawntimesecs;
        public List<Difficulty> spawnDifficulties;
        public SpawnGroupTemplateData spawnGroupData;
        public uint ScriptId;
        public bool dbData = true;


        public SpawnData(SpawnObjectType t)
        {
            type = t;
        }
    }
}
