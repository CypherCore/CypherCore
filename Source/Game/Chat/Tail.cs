// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Chat
{
    internal struct Tail
    {
        private string _str;

        public bool IsEmpty()
        {
            return _str.IsEmpty();
        }

        public static implicit operator string(Tail tail)
        {
            return tail._str;
        }

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            _str = args;

            return new ChatCommandResult(_str);
        }
    }
}