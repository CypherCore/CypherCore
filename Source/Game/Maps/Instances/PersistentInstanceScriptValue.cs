// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    internal class PersistentInstanceScriptValue<T> : PersistentInstanceScriptValueBase
    {
        public PersistentInstanceScriptValue(InstanceScript instance, string name, T value) : base(instance, name, value)
        {
        }

        public PersistentInstanceScriptValue<T> SetValue(T value)
        {
            Value = value;
            NotifyValueChanged();

            return this;
        }

        private void NotifyValueChanged()
        {
            Instance.Instance.UpdateInstanceLock(CreateEvent());
        }

        private void LoadValue(T value)
        {
            Value = value;
        }
    }
}