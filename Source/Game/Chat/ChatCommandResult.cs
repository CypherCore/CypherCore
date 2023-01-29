// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Chat
{
    public struct ChatCommandResult
    {
        private bool _result;
        private readonly dynamic _value;
        private string _errorMessage;

        public ChatCommandResult(string _value = "")
        {
            _result = true;
            this._value = _value;
            _errorMessage = null;
        }

        public bool IsSuccessful()
        {
            return _result;
        }

        public bool HasErrorMessage()
        {
            return !_errorMessage.IsEmpty();
        }

        public string GetErrorMessage()
        {
            return _errorMessage;
        }

        public void SetErrorMessage(string _value)
        {
            _result = false;
            _errorMessage = _value;
        }

        public static ChatCommandResult FromErrorMessage(string str)
        {
            var result = new ChatCommandResult();
            result.SetErrorMessage(str);

            return result;
        }

        public static implicit operator string(ChatCommandResult stringResult)
        {
            return stringResult._value;
        }
    }
}