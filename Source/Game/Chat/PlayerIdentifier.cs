// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat
{
    internal class PlayerIdentifier
    {
        private ObjectGuid _guid;
        private string _name;
        private Player _player;

        public PlayerIdentifier()
        {
        }

        public PlayerIdentifier(Player player)
        {
            _name = player.GetName();
            _guid = player.GetGUID();
            _player = player;
        }

        public string GetName()
        {
            return _name;
        }

        public ObjectGuid GetGUID()
        {
            return _guid;
        }

        public bool IsConnected()
        {
            return _player != null;
        }

        public Player GetConnectedPlayer()
        {
            return _player;
        }

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
}