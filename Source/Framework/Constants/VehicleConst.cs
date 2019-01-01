/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
 */﻿

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
}
