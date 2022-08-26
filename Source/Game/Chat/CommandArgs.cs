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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Game.Chat
{
    class CommandArgs
    {
        static Dictionary<Type, Func<CommandArguments, ParseStringResult>> Parsers = new()
        {
            { typeof(sbyte), args => args.NextSByte() },
            { typeof(short), args => args.NextInt16() },
            { typeof(int), args => args.NextInt32() },
            { typeof(long), args => args.NextInt64() },
            { typeof(byte), args => args.NextByte() },
            { typeof(ushort), args => args.NextUInt16() },
            { typeof(uint), args => args.NextUInt32() },
            { typeof(ulong), args => args.NextUInt64() },
            { typeof(float), args => args.NextSingle() },
            { typeof(string), args => args.NextString() },
            { typeof(bool), args => args.NextBoolean() },
            { typeof(PlayerIdentifier), args => PlayerIdentifier.ParseFromString(args) },
            { typeof(AccountIdentifier), args => AccountIdentifier.ParseFromString(args) },
            { typeof(Tail), args => Tail.ParseFromString(args) },
        };

        public static bool Parse(out dynamic[] parsedArgs, CommandHandler handler, ParameterInfo[] parameterInfos, string tailCmd)
        {
            parsedArgs = new dynamic[parameterInfos.Length];
            parsedArgs[0] = handler;

            CommandArguments args = new CommandArguments(tailCmd);

            for (var i = 1; i < parameterInfos.Length; i++)
            {
                if (!ParseArgument(out dynamic value, parameterInfos[i], args))
                    return false;

                parsedArgs[i] = value;
            }

            return true;
        }

        static bool ParseArgument(out dynamic value, ParameterInfo parameterInfo, CommandArguments args)
        {
            var parameterType = parameterInfo.ParameterType;

            if (Hyperlink.TryParse(out value, parameterType, args))
                return true;

            bool isOptional = false;

            var optionalArgAttribute = parameterInfo.GetCustomAttribute<OptionalArgAttribute>(true);
            if (optionalArgAttribute != null)
                isOptional = true;

            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                parameterType = Nullable.GetUnderlyingType(parameterType);
                isOptional = true;
            }

            if (parameterType.IsEnum)
                parameterType = parameterType.GetEnumUnderlyingType();

            var pos = args.GetCurrentPosition();

            var result = Parsers[parameterType](args);
            if (result != ParseResult.Ok)
            {
                if (!isOptional)
                    return false;
                else
                    args.SetCurrentPosition(pos);
            }

            value = result.GetValue();
            return true;
        }
    }

    public enum ParseResult
    {
        Ok,
        Error,
        EndOfString
    }

    public struct ParseStringResult
    {
        ParseResult result;
        dynamic value;

        public ParseStringResult(ParseResult _result, dynamic _value = default)
        {
            result = _result;
            value = _value;
        }

        public dynamic GetValue() { return value; }

        public static implicit operator ParseResult(ParseStringResult stringResult)
        {
            return stringResult.result;
        }

        public static implicit operator string(ParseStringResult stringResult)
        {
            return stringResult.value;
        }
    }

    public sealed class CommandArguments
    {
        static ParseStringResult ErrorResult = new(ParseResult.Error);
        static ParseStringResult EndOfStringResult = new(ParseResult.EndOfString);

        public CommandArguments(string args)
        {
            if (!args.IsEmpty())
                activestring = args.TrimStart(' ');
            activeposition = -1;
        }

        public CommandArguments(CommandArguments args)
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

        public ParseStringResult NextString(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return new ParseStringResult(ParseResult.EndOfString, "");

            if (Current.Any(c => char.IsLetter(c)))
                return new ParseStringResult(ParseResult.Ok, Current);

            return new ParseStringResult(ParseResult.Error, "");
        }

        public ParseStringResult NextBoolean(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            bool value;
            if (bool.TryParse(Current, out value))
                return new ParseStringResult(ParseResult.Ok, value);

            if ((Current == "1") || Current.Equals("y", StringComparison.OrdinalIgnoreCase) || Current.Equals("on", StringComparison.OrdinalIgnoreCase) || Current.Equals("yes", StringComparison.OrdinalIgnoreCase) || Current.Equals("true", StringComparison.OrdinalIgnoreCase))
                return new ParseStringResult(ParseResult.Ok, true);
            if ((Current == "0") || Current.Equals("n", StringComparison.OrdinalIgnoreCase) || Current.Equals("off", StringComparison.OrdinalIgnoreCase) || Current.Equals("no", StringComparison.OrdinalIgnoreCase) || Current.Equals("false", StringComparison.OrdinalIgnoreCase))
                return new ParseStringResult(ParseResult.Ok, false);

            return ErrorResult;
        }

        public ParseStringResult NextChar(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (char.TryParse(Current, out char value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (byte.TryParse(Current, out byte value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextSByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (sbyte.TryParse(Current, out sbyte value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextUInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (ushort.TryParse(Current, out ushort value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (short.TryParse(Current, out short value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextUInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (uint.TryParse(Current, out uint value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (int.TryParse(Current, out int value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextUInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (ulong.TryParse(Current, out ulong value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (long.TryParse(Current, out long value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextSingle(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (float.TryParse(Current, out float value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextDouble(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (double.TryParse(Current, out double value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
        }

        public ParseStringResult NextDecimal(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return EndOfStringResult;

            if (decimal.TryParse(Current, out decimal value))
                return new ParseStringResult(ParseResult.Ok, value);

            return ErrorResult;
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

        private string activestring;
        private int activeposition;
        private string Current;
    }
}
