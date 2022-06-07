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
using Game.Entities;
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
                if (ParseArgument(out dynamic value, parameterTypes[index], args))
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

        static bool ParseArgument(out dynamic value, Type type, StringArguments args)
        {
            value = default;

            if (Hyperlink.TryParse(out value, type, args))
                return true;

            if (args.IsAtEnd())
                return false;

            if (Hyperlink.TryParse(out value, type, args))
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

    class PlayerIdentifier
    {
        string _name;
        ObjectGuid _guid;
        Player _player;

        public PlayerIdentifier(string name, ObjectGuid guid)
        {
            _name = name;
            _guid = guid;
        }

        public PlayerIdentifier(Player player)
        {
            _name = player.GetName();
            _guid = player.GetGUID();
            _player = player;
        }

        public string GetName() { return _name; }
        public ObjectGuid GetGUID() { return _guid; }
        public Player GetPlayer() { return _player; }

        public static PlayerIdentifier FromTarget(CommandHandler handler)
        {
            Player player = handler.GetPlayer();
            if (player != null)
            {
                Player target = player.GetSelectedPlayer();
                if (target != null)
                    return new PlayerIdentifier(target);
            }

            return null;
        }

        public static PlayerIdentifier FromSelf(CommandHandler handler)
        {
            Player player = handler.GetPlayer();
            if (player != null)
                return new PlayerIdentifier(player);

            return null;
        }

        public static PlayerIdentifier FromTargetOrSelf(CommandHandler handler)
        {
            PlayerIdentifier fromTarget = FromTarget(handler);
            if (fromTarget != null)
                return fromTarget;
            else
                return FromSelf(handler);
        }

        public static PlayerIdentifier ParseFromString(string arg)
        {
            ulong guid = 0;
            string name;
            if (!Hyperlink.TryParse(out name, arg) || !ulong.TryParse(arg, out guid))
                name = arg;

            if (!name.IsEmpty())
            {
                ObjectManager.NormalizePlayerName(ref name);
                var player = Global.ObjAccessor.FindPlayerByName(name);
                if (player != null)
                    return new PlayerIdentifier(player);
                else
                {
                    var objectGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
                    if (objectGuid.IsEmpty())
                        return new PlayerIdentifier(name, objectGuid);
                }
            }
            else if (guid != 0)
            {
                var player = Global.ObjAccessor.FindPlayerByLowGUID(guid);
                if (player != null)
                    return new PlayerIdentifier(player);
            }

            return null;
        }
    }
}
