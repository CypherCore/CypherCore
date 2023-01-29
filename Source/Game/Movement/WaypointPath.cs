// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class WaypointPath
    {
        public uint Id { get; set; }

        public List<WaypointNode> Nodes { get; set; } = new();

        public WaypointPath()
        {
        }

        public WaypointPath(uint _id, List<WaypointNode> _nodes)
        {
            Id = _id;
            Nodes = _nodes;
        }
    }
}