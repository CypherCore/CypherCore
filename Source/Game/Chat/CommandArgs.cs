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
        public static bool Parse(out dynamic[] parsedArgs, CommandHandler handler, Type[] parameterTypes, StringArguments args)
        {
            parsedArgs = new dynamic[parameterTypes.Length];
            parsedArgs[0] = handler;

            for (var i = 1; i < parameterTypes.Length; i++)
            {
                if (!ParseArgument(out dynamic value, parameterTypes[i], args))
                    return false;

                parsedArgs[i] = value;
            }

            return true;
        }

        static bool ParseArgument(out dynamic value, Type type, StringArguments args, bool IsOptional = false)
        {
            value = default;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OptionalArg<>))
                return ParseArgument(out value, type.GetGenericArguments()[0], args, true);

            //todo remove me when all commands to changed to OptionalArg<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return ParseArgument(out value, Nullable.GetUnderlyingType(type), args, true);

            if (args.IsAtEnd())
                return IsOptional;

            if (Hyperlink.TryParse(out value, type, args))
                return true;

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
                    switch (type.Name)
                    {
                        case nameof(PlayerIdentifier):
                            value = PlayerIdentifier.ParseFromString(args.NextString());
                            break;
                        case nameof(AccountIdentifier):
                            value = AccountIdentifier.ParseFromString(args.NextString());
                            break;
                        default:
                            return false;
                    }
                    break;
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
        public bool IsConnected() { return _player != null; }
        public Player GetConnectedPlayer() { return _player; }
        
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

    class AccountIdentifier
    {
        uint _id;
        string _name;
        WorldSession _session;

        public AccountIdentifier(WorldSession session)
        {
            _id = session.GetAccountId();
            _name = session.GetAccountName();
            _session = session;
        }

        public uint GetID() { return _id; }
        public string GetName() { return _name; }
        public bool IsConnected() { return _session != null; }
        public WorldSession GetConnectedSession() { return _session; }

        public static AccountIdentifier ParseFromString(string arg)
        {
            // try parsing as account name
            var session = Global.WorldMgr.FindSession(Global.AccountMgr.GetId(arg));
            if (session != null) // account with name exists, we are done
                return new AccountIdentifier(session);

            // try parsing as account id
            if (uint.TryParse(arg, out uint id))
                return null;

            session = Global.WorldMgr.FindSession(id);
            if (session != null)
                return new AccountIdentifier(session);

            return null;
        }

        public static AccountIdentifier FromTarget(CommandHandler handler)
        {
            Player player = handler.GetPlayer();
            if (player != null)
            {
                Player target = player.GetSelectedPlayer();
                if (target != null)
                {
                    WorldSession session = target.GetSession();
                    if (session != null)
                        return new AccountIdentifier(session);
                }
            }

            return null;
        }
    }

    struct OptionalArg<T>
    {
        private bool _hasValue;
        public T Value;

        public OptionalArg(T value)
        {
            Value = value;
            _hasValue = true;
        }

        public bool HasValue
        {
            get { return _hasValue; }
        }

        public void Set(T value)
        {
            Value = value;
            _hasValue = true;
        }

        public void Clear()
        {
            _hasValue = false;
            Value = default;
        }

        public T ValueOr(T otherValue)
        {
            return HasValue ? Value : otherValue;
        }

        public static explicit operator T(OptionalArg<T> optional)
        {
            return optional.Value;
        }

        public static implicit operator OptionalArg<T>(T value)
        {
            return new OptionalArg<T>(value);
        }

        public Type GetValueType()
        {
            return typeof(T);
        }
    }
}
