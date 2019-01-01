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

using System;
using System.Text.RegularExpressions;

namespace Framework.IO
{
    public sealed class StringArguments
    {
        public StringArguments(string args)
        {
            if (!args.IsEmpty())
                activestring = args.TrimStart(' ');
            activeposition = -1;
        }

        public bool Empty()
        {
            return activestring.IsEmpty();
        }

        public void MoveToNextChar(char c)
        {
            for (var i = activeposition; i < activestring.Length; ++i)
                if (activestring[i] == c)
                    break;
        }

        public string NextString(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return "";

            return Current;
        }

        public bool NextBoolean(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return false;

            bool value;
            if (bool.TryParse(Current, out value))
                return value;

            return false;
        }

        public char NextChar(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(char);

            char value;
            if (char.TryParse(Current, out value))
                return value;

            return default(char);
        }

        public byte NextByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(byte);

            byte value;
            if (byte.TryParse(Current, out value))
                return value;

            return default(byte);
        }

        public sbyte NextSByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(sbyte);

            sbyte value;
            if (sbyte.TryParse(Current, out value))
                return value;

            return default(sbyte);
        }

        public ushort NextUInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(ushort);

            ushort value;
            if (ushort.TryParse(Current, out value))
                return value;

            return default(ushort);
        }

        public short NextInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(short);

            short value;
            if (short.TryParse(Current, out value))
                return value;

            return default(short);
        }

        public uint NextUInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(uint);

            uint value;
            if (uint.TryParse(Current, out value))
                return value;

            return default(uint);
        }

        public int NextInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(int);

            int value;
            if (int.TryParse(Current, out value))
                return value;

            return default(int);
        }

        public ulong NextUInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(ulong);

            ulong value;
            if (ulong.TryParse(Current, out value))
                return value;

            return default(ulong);
        }

        public long NextInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(long);

            long value;
            if (long.TryParse(Current, out value))
                return value;

            return default(long);
        }

        public float NextSingle(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(float);

            float value;
            if (float.TryParse(Current, out value))
                return value;

            return default(float);
        }

        public double NextDouble(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(double);

            double value;
            if (double.TryParse(Current, out value))
                return value;

            return default(double);
        }

        public decimal NextDecimal(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default(decimal);

            decimal value;
            if (decimal.TryParse(Current, out value))
                return value;

            return default(decimal);
        }

        public void AlignToNextChar()
        {
            while (activeposition < activestring.Length && activestring[activeposition] != ' ')            
                activeposition++;            
        }

        public char this[int index]
        {
            get { return activestring[index]; }
        }

        public string GetString()
        {
            return activestring;
        }

        public void Reset()
        {
            activeposition = -1;
            Current = null;
        }

        bool MoveNext(string delimiters)
        {
            //the stringtotokenize was never set:
            if (activestring == null)
                return false;

            //all tokens have already been extracted:
            if (activeposition == activestring.Length)
                return false;

            //bypass delimiters:
            activeposition++;
            while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) > -1)
            {
                activeposition++;
            }

            //only delimiters were left, so return null:
            if (activeposition == activestring.Length)
                return false;

            //get starting position of string to return:
            int startingposition = activeposition;

            //read until next delimiter:
            do
            {
                activeposition++;
            } while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) == -1);

            Current = activestring.Substring(startingposition, activeposition - startingposition);
            return true;
        }

        bool Match(string pattern, out Match m)
        {
            Regex r = new Regex(pattern);
            m = r.Match(activestring);
            return m.Success;
        }

        private string activestring;
        private int activeposition;
        private string Current;
    }
}
