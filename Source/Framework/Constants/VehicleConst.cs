// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.﻿

namespace Framework.Constants
{
    public enum VehiclePowerType
    {
        Steam = 61,
        Pyrite = 41,
        Heat = 101,
        Ooze = 121,
        Blood = 141,
        Wrath = 142,
        ArcaneEnergy = 143,
        LifeEnergy = 144,
        SunEnergy = 145,
        SwingVelocity = 146,
        ShadowflameEnergy = 147,
        BluePower = 148,
        PurplePower = 149,
        GreenPower = 150,
        OrangePower = 151,
        Energy2 = 153,
        Arcaneenergy = 161,
        Wind1 = 162,
        Wind2 = 163,
        Wind3 = 164,
        Fuel = 165,
        SunPower = 166,
        TwilightEnergy = 169,
        Venom = 174,
        Orange2 = 176,
        ConsumingFlame = 177,
        PyroclasticFrenzy = 178,
        Flashfire = 179,
    }

    public enum VehicleFlags
    {
        NoStrafe = 0x01,           // Sets Moveflag2NoStrafe
        NoJumping = 0x02,           // Sets Moveflag2NoJumping
        Fullspeedturning = 0x04,           // Sets Moveflag2Fullspeedturning
        AllowPitching = 0x10,           // Sets Moveflag2AllowPitching
        Fullspeedpitching = 0x20,           // Sets Moveflag2Fullspeedpitching
        CustomPitch = 0x40,           // If Set Use Pitchmin And Pitchmax From Dbc, Otherwise Pitchmin = -Pi/2, Pitchmax = Pi/2
        AdjustAimAngle = 0x400,           // LuaIsvehicleaimangleadjustable
        AdjustAimPower = 0x800,            // LuaIsvehicleaimpoweradjustable
        FixedPosition = 0x200000            // Used for cannons, when they should be rooted
    }

    public enum VehicleExitParameters
    {
        VehicleExitParamNone = 0, // provided parameters will be ignored
        VehicleExitParamOffset = 1, // provided parameters will be used as offset values
        VehicleExitParamDest = 2, // provided parameters will be used as absolute destination
        VehicleExitParamMax
    }
}
