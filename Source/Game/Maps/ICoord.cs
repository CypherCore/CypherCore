// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public interface ICoord
    {
        uint X_coord { get; set; }
        uint Y_coord { get; set; }
        bool IsCoordValid();
        ICoord Normalize();
        uint GetId();
        void Dec_x(uint val);
        void Inc_x(uint val);
        void Dec_y(uint val);
        void Inc_y(uint val);
    }
}