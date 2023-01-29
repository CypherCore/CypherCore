// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class VehicleSeatAddon
    {
        public VehicleExitParameters ExitParameter { get; set; }
        public float ExitParameterO { get; set; }
        public float ExitParameterX { get; set; }
        public float ExitParameterY { get; set; }
        public float ExitParameterZ { get; set; }
        public float SeatOrientationOffset { get; set; }

        public VehicleSeatAddon(float orientatonOffset, float exitX, float exitY, float exitZ, float exitO, byte param)
        {
            SeatOrientationOffset = orientatonOffset;
            ExitParameterX = exitX;
            ExitParameterY = exitY;
            ExitParameterZ = exitZ;
            ExitParameterO = exitO;
            ExitParameter = (VehicleExitParameters)param;
        }
    }
}