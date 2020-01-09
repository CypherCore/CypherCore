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
using System;

public class RandomHelper
{
    private static readonly Random rand;

    static RandomHelper()
    {
        rand = new Random();
    }

    /// <summary>
    /// Returns a random number between 0.0 and 1.0.
    /// </summary>
    /// <returns></returns>
    public static double NextDouble()
    {
        return rand.NextDouble();
    }

    /// <summary>
    /// Returns a nonnegative random number.
    /// </summary>
    /// <returns></returns>
    public static uint Rand32()
    {
        return (uint)rand.Next();
    }

    /// <summary>
    /// Returns a nonnegative random number less than the specified maximum.
    /// </summary>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static uint Rand32(dynamic maxValue)
    {
        return (uint)rand.Next(maxValue);
    }

    /// <summary>
    /// Returns a random number within a specified range.
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static int IRand(int minValue, int maxValue)
    {
        return rand.Next(minValue, maxValue);
    }
    public static uint URand(dynamic minValue, dynamic maxValue)
    {
        return (uint)rand.Next(Convert.ToInt32(minValue), Convert.ToInt32(maxValue));
    }
    public static float FRand(float min, float max)
    {
        Cypher.Assert(max >= min);
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// Returns true if rand.Next less then i
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public static bool randChance(float i)
    {
        return i > randChance();
    }

    public static double randChance()
    {
        return rand.NextDouble() * 100.0;
    }

    /// <summary>
    /// Fills the elements of a specified array of bytes with random numbers.
    /// </summary>
    /// <param name="buffer"></param>
    public static void NextBytes(byte[] buffer)
    {
        rand.NextBytes(buffer);
    }

    public static T RAND<T>(params T[] args)
    {
        int randIndex = IRand(0, args.Length - 1);

        return args[randIndex];
    }
}

