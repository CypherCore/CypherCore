// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public struct UpdateAdditionalSaveDataEvent
    {
        public string Key;
        public object Value;

        public UpdateAdditionalSaveDataEvent(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}