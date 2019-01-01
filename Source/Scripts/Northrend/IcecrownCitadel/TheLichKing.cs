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

namespace Scripts.Northrend.IcecrownCitadel
{
    public class TheLichKing
    {
        Position CenterPosition = new Position(503.6282f, -2124.655f, 840.8569f, 0.0f);
        Position TirionIntro = new Position(489.2970f, -2124.840f, 840.8569f, 0.0f);
        Position TirionCharge = new Position(482.9019f, -2124.479f, 840.8570f, 0.0f);
        Position[] LichKingIntro =
        {
            new Position(432.0851f, -2123.673f, 864.6582f, 0.0f),
            new Position(457.8351f, -2123.423f, 841.1582f, 0.0f),
            new Position(465.0730f, -2123.470f, 840.8569f, 0.0f),
        };
        Position OutroPosition1 = new Position(493.6286f, -2124.569f, 840.8569f, 0.0f);
        Position OutroFlying = new Position(508.9897f, -2124.561f, 845.3565f, 0.0f);
        public static Position TerenasSpawn = new Position(495.5542f, -2517.012f, 1050.000f, 4.6993f);
        Position TerenasSpawnHeroic = new Position(495.7080f, -2523.760f, 1050.000f, 0.0f);
        public static Position SpiritWardenSpawn = new Position(495.3406f, -2529.983f, 1050.000f, 1.5592f);
    }
}
