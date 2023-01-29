// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Chat
{
    internal struct QuotedString
    {
        private string _str;

        public bool IsEmpty()
        {
            return _str.IsEmpty();
        }

        public static implicit operator string(QuotedString quotedString)
        {
            return quotedString._str;
        }

        public ChatCommandResult TryConsume(CommandHandler handler, string args)
        {
            _str = "";

            if (args.IsEmpty())
                return ChatCommandResult.FromErrorMessage("");

            if ((args[0] != '"') &&
                (args[0] != '\''))
                return CommandArgs.TryConsume(out dynamic str, typeof(string), handler, args);

            char QUOTE = args[0];

            for (var i = 1; i < args.Length; ++i)
            {
                if (args[i] == QUOTE)
                {
                    var (remainingToken, tail) = args[(i + 1)..].Tokenize();

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

                _str += args[i];
            }

            // if we reach this, we did not find a closing quote
            return ChatCommandResult.FromErrorMessage("");
        }
    }
}