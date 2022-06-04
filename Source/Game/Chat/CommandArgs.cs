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

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    class CommandArgs
    {
        public static object[] Parse(CommandHandler handler, Type[] parameterTypes, StringArguments args)
        {
            List<object> arguments = new();
            arguments.Add(handler);

            //for each arg we need to see if its a:
            //1: hyperlink
            //2: Optional arg
            //3: reg arg.
            int index = 1;
            while (index < parameterTypes.Length)
            {
                int oldPos = args.GetCurrentPosition();

                //Is this a hyperlink?
                if (Hyperlink.TryConsume(out dynamic value, parameterTypes[index], args) || ParseArgument(out value, parameterTypes[index], args))
                    index++;

                if (args.IsAtEnd() && index < parameterTypes.Length)
                {
                    //We found a optional arg and we dont have the correct amount of args
                    args.SetCurrentPosition(oldPos);
                    arguments.Add(default);
                }
                else
                    arguments.Add(value);

            }

            return arguments.ToArray();
        }

        public static bool ParseArgument(out dynamic value, Type type, StringArguments args)
        {
            value = default;

            if (args.IsAtEnd())
                return false;

            if (Hyperlink.TryConsume(out value, type, args))
                return value;

            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    value = args.NextSByte();
                    break;
                case TypeCode.Int16:
                    value = args.NextInt16();
                    break;
                case TypeCode.Int32:
                    value = args.NextInt32();
                    break;
                case TypeCode.Int64:
                    value = args.NextInt64();
                    break;
                case TypeCode.Byte:
                    value = args.NextByte();
                    break;
                case TypeCode.UInt16:
                    value = args.NextUInt16();
                    break;
                case TypeCode.UInt32:
                    value = args.NextUInt32();
                    break;
                case TypeCode.UInt64:
                    value = args.NextUInt64();
                    break;
                case TypeCode.Single:
                    value = args.NextSingle();
                    break;
                case TypeCode.String:
                    value = args.NextString();
                    break;
                case TypeCode.Boolean:
                    value = args.NextBoolean();
                    break;
                case TypeCode.Char:
                    value = args.NextChar();
                    break;
                case TypeCode.Object:
                    if (type.IsGenericType)
                        type = type.GenericTypeArguments[0].UnderlyingSystemType;

                    switch (type.Name)
                    {
                        case nameof(AchievementRecord):
                            value = CliDB.AchievementStorage.LookupByKey(args.NextUInt32());
                            break;
                        case nameof(CurrencyTypesRecord):
                            value = CliDB.CurrencyTypesStorage.LookupByKey(args.NextUInt32());
                            break;
                        case nameof(GameTele):
                            value = Global.ObjectMgr.GetGameTele(args.NextString());
                            break;
                        case nameof(SpellInfo):
                            value = Global.SpellMgr.GetSpellInfo(args.NextUInt32(), Difficulty.None);
                            break;
                        default:
                            return false;
                    }

                    return true;
                default:
                    return false;
            }
            return true;
        }
    }
}
