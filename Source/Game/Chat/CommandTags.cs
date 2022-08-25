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

using Game.Entities;
using System;

namespace Game.Chat
{
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

        public static ParseStringResult ParseFromString(CommandArguments args)
        {
            var result = args.NextString();
            if (result != ParseResult.Ok)
                return new ParseStringResult(ParseResult.Error);

            string arg = result.GetValue();

            ulong guid = 0;
            string name;
            if (!Hyperlink.TryParse(out name, arg) || !ulong.TryParse(arg, out guid))
                name = arg;

            if (!name.IsEmpty())
            {
                ObjectManager.NormalizePlayerName(ref name);
                var player = Global.ObjAccessor.FindPlayerByName(name);
                if (player != null)
                    return new ParseStringResult(ParseResult.Ok, new PlayerIdentifier(player));
                else
                {
                    var objectGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
                    if (objectGuid.IsEmpty())
                        return new ParseStringResult(ParseResult.Ok, new PlayerIdentifier(name, objectGuid));
                }
            }
            else if (guid != 0)
            {
                var player = Global.ObjAccessor.FindPlayerByLowGUID(guid);
                if (player != null)
                    return new ParseStringResult(ParseResult.Ok, new PlayerIdentifier(player));
            }

            return new ParseStringResult(ParseResult.Error);
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

        public static ParseStringResult ParseFromString(CommandArguments args)
        {
            var result = args.NextString();
            if (result != ParseResult.Ok)
                return new ParseStringResult(ParseResult.Error);

            // try parsing as account name
            var session = Global.WorldMgr.FindSession(Global.AccountMgr.GetId(result.GetValue()));
            if (session != null) // account with name exists, we are done
                return new ParseStringResult(ParseResult.Ok, new AccountIdentifier(session));

            // try parsing as account id
            if (!uint.TryParse(result.GetValue(), out uint id))
                return new ParseStringResult(ParseResult.Error);

            session = Global.WorldMgr.FindSession(id);
            if (session != null)
                return new ParseStringResult(ParseResult.Ok, new AccountIdentifier(session));

            return new ParseStringResult(ParseResult.Error);
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

    struct Tail
    {
        string str;

        public Tail(CommandArguments args)
        {
            str = args.NextString("").GetValue();
        }

        public bool IsEmpty() { return str.IsEmpty(); }

        public static implicit operator string(Tail tail)
        {
            return tail.str;
        }

        public static ParseStringResult ParseFromString(CommandArguments args)
        {
            return new ParseStringResult(ParseResult.Ok, new Tail(args));
        }
    }
}
