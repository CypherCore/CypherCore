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
using Game.Entities;
using System;

namespace Game.Chat
{
    class PlayerIdentifier
    {
        string _name;
        ObjectGuid _guid;
        Player _player;

        public PlayerIdentifier() { }

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

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            ChatCommandResult next = CommandArgs.TryConsume(out dynamic tempVal, typeof(ulong), handler, args);
            if (!next.IsSuccessful())
                next = CommandArgs.TryConsume(out tempVal, typeof(string), handler, args);
            if (!next.IsSuccessful())
                return next;

            if (tempVal is ulong)
            {
                _guid = ObjectGuid.Create(HighGuid.Player, tempVal);
                if ((_player = Global.ObjAccessor.FindPlayerByLowGUID(_guid.GetCounter())) != null)
                    _name = _player.GetName();
                else if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(_guid, out _name))
                    return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharGuidNoExist, _guid.ToString()));
                return next;
            }
            else
            {
                _name = tempVal;

                if (!ObjectManager.NormalizePlayerName(ref _name))
                    return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharNameInvalid, _name));

                if ((_player = Global.ObjAccessor.FindPlayerByName(_name)) != null)
                    _guid = _player.GetGUID();
                else if ((_guid = Global.CharacterCacheStorage.GetCharacterGuidByName(_name)).IsEmpty())
                    return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharNameNoExist, _name));
                return next;
            }
        }
    }

    class AccountIdentifier
    {
        uint _id;
        string _name;
        WorldSession _session;

        public AccountIdentifier() { }
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

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            ChatCommandResult next = CommandArgs.TryConsume(out dynamic text, typeof(string), handler, args);
            if (!next.IsSuccessful())
                return next;

            // first try parsing as account name
            _name = text;
            _id = Global.AccountMgr.GetId(_name);
            _session = Global.WorldMgr.FindSession(_id);
            if (_id != 0) // account with name exists, we are done
                return next;

            // try parsing as account id instead
            if (uint.TryParse(text, out uint id))
                return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountNameNoExist, _name));
            _id = id;
            _session = Global.WorldMgr.FindSession(_id);

            if (Global.AccountMgr.GetName(_id, out _name))
                return next;
            else
                return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountIdNoExist, _id));
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

        public bool IsEmpty() { return str.IsEmpty(); }

        public static implicit operator string(Tail tail)
        {
            return tail.str;
        }

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            str = args;
            return new ChatCommandResult(str);
        }
    }

    struct QuotedString
    {
        string str;

        public bool IsEmpty() { return str.IsEmpty(); }

        public static implicit operator string(QuotedString quotedString)
        {
            return quotedString.str;
        }

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            str = "";

            if (args.IsEmpty())
                return ChatCommandResult.FromErrorMessage("");
            if ((args[0] != '"') && (args[0] != '\''))
                return CommandArgs.TryConsume(out dynamic str, typeof(string), handler, args);

            char QUOTE = args[0];
            for (var i = 1; i < args.Length; ++i)
            {
                if (args[i] == QUOTE)
                {
                    var (remainingToken, tail) = args.Substring(i + 1).Tokenize();
                    if (remainingToken.IsEmpty()) // if this is not empty, then we did not consume the full token
                        return new ChatCommandResult(tail);
                    else
                        return ChatCommandResult.FromErrorMessage("");
                }

                if (args[i] == '\\')
                {
                    ++i;
                    if (!(i < args.Length))
                        break;
                }
                str += args[i];
            }
            // if we reach this, we did not find a closing quote
            return ChatCommandResult.FromErrorMessage("");
        }
    }
}
