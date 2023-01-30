// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    internal class UiMapBounds
    {
        // these coords are mixed when calculated and used... its a mess
        public float[] Bounds = new float[4];
        public bool IsUiAssignment { get; set; }
        public bool IsUiLink { get; set; }
    }
}