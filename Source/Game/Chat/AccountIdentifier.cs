// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat
{
    internal class AccountIdentifier
    {
        private uint _id;
        private string _name;
        private WorldSession _session;

        public AccountIdentifier()
        {
        }

        public AccountIdentifier(WorldSession session)
        {
            _id = session.GetAccountId();
            _name = session.GetAccountName();
            _session = session;
        }

        public uint GetID()
        {
            return _id;
        }

        public string GetName()
        {
            return _name;
        }

        public bool IsConnected()
        {
            return _session != null;
        }

        public WorldSession GetConnectedSession()
        {
            return _session;
        }

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            ChatCommandResult next = CommandArgs.TryConsume(out dynamic text, typeof(string), handler, args);

            if (!next.IsSuccessful())
                return next;

            // first try parsing as account Name
            _name = text;
            _id = Global.AccountMgr.GetId(_name);
            _session = Global.WorldMgr.FindSession(_id);

            if (_id != 0) // account with Name exists, we are done
                return next;

            // try parsing as account Id instead
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
}