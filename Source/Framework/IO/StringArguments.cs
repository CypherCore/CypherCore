// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

        public StringArguments(StringArguments args)
        {
            activestring = args.activestring;
            activeposition = args.activeposition;
            Current = args.Current;
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
            
            if ((Current == "1") || Current.Equals("y", StringComparison.OrdinalIgnoreCase) || Current.Equals("on", StringComparison.OrdinalIgnoreCase) || Current.Equals("yes", StringComparison.OrdinalIgnoreCase) || Current.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
            if ((Current == "0") || Current.Equals("n", StringComparison.OrdinalIgnoreCase) || Current.Equals("off", StringComparison.OrdinalIgnoreCase) || Current.Equals("no", StringComparison.OrdinalIgnoreCase) || Current.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            return false;
        }

        public char NextChar(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            char value;
            if (char.TryParse(Current, out value))
                return value;

            return default;
        }

        public byte NextByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            byte value;
            if (byte.TryParse(Current, out value))
                return value;

            return default;
        }

        public sbyte NextSByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            sbyte value;
            if (sbyte.TryParse(Current, out value))
                return value;

            return default;
        }

        public ushort NextUInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            ushort value;
            if (ushort.TryParse(Current, out value))
                return value;

            return default;
        }

        public short NextInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            short value;
            if (short.TryParse(Current, out value))
                return value;

            return default;
        }

        public uint NextUInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            uint value;
            if (uint.TryParse(Current, out value))
                return value;

            return default;
        }

        public int NextInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            int value;
            if (int.TryParse(Current, out value))
                return value;

            return default;
        }

        public ulong NextUInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            ulong value;
            if (ulong.TryParse(Current, out value))
                return value;

            return default;
        }

        public long NextInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            long value;
            if (long.TryParse(Current, out value))
                return value;

            return default;
        }

        public float NextSingle(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            float value;
            if (float.TryParse(Current, out value))
                return value;

            return default;
        }

        public double NextDouble(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            double value;
            if (double.TryParse(Current, out value))
                return value;

            return default;
        }

        public decimal NextDecimal(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            decimal value;
            if (decimal.TryParse(Current, out value))
                return value;

            return default;
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

        public bool IsAtEnd()
        {
            return activestring.IsEmpty() || activeposition == activestring.Length;
        }

        public int GetCurrentPosition()
        {
            return activeposition;
        }

        public void SetCurrentPosition(int currentPosition)
        {
            activeposition = currentPosition;
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
            Regex r = new(pattern);
            m = r.Match(activestring);
            return m.Success;
        }

        private string activestring;
        private int activeposition;
        private string Current;
    }
}
