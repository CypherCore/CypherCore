// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public class PersistentInstanceScriptValueBase
    {
        protected InstanceScript Instance;
        protected string Name;
        protected object Value;

        protected PersistentInstanceScriptValueBase(InstanceScript instance, string name, object value)
        {
            Instance = instance;
            Name = name;
            Value = value;

            Instance.RegisterPersistentScriptValue(this);
        }

        public string GetName()
        {
            return Name;
        }

        public UpdateAdditionalSaveDataEvent CreateEvent()
        {
            return new UpdateAdditionalSaveDataEvent(Name, Value);
        }

        public void LoadValue(long value)
        {
            Value = value;
        }

        public void LoadValue(double value)
        {
            Value = value;
        }
    }
}