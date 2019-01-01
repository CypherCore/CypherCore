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
 */

using Game.Entities;

namespace Scripts.Northrend.Ulduar
{
    class YoggSaron
    {
        public static Position[] ObservationRingKeepersPos =
        {
            new Position(1945.682f,  33.34201f, 411.4408f, 5.270895f),  // Freya
            new Position(1945.761f, -81.52171f, 411.4407f, 1.029744f),  // Hodir
            new Position(2028.822f, -65.73573f, 411.4426f, 2.460914f),  // Thorim
            new Position(2028.766f,  17.42014f, 411.4446f, 3.857178f),  // Mimiron
        };
        public static Position[] YSKeepersPos =
        {
            new Position(2036.873f,  25.42513f, 338.4984f, 3.909538f),  // Freya            
            new Position(1939.045f, -90.87457f, 338.5426f, 0.994837f),  // Hodir
            new Position(1939.148f,  42.49035f, 338.5427f, 5.235988f),  // Thorim
            new Position(2036.658f, -73.58822f, 338.4985f, 2.460914f),  // Mimiron
        };
    }
}
