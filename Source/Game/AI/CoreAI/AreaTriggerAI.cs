// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public class AreaTriggerAI
    {
        protected AreaTrigger at;

        public AreaTriggerAI(AreaTrigger a)
        {
            at = a;
        }

        // Called when the AreaTrigger has just been initialized, just before added to map
        public virtual void OnInitialize() { }

        // Called when the AreaTrigger has just been created
        public virtual void OnCreate(Spell creatingSpell) { }

        // Called on each AreaTrigger update
        public virtual void OnUpdate(uint diff) { }

        // Called when the AreaTrigger reach splineIndex
        public virtual void OnSplineIndexReached(int splineIndex) { }

        // Called when the AreaTrigger reach its destination
        public virtual void OnDestinationReached() { }

        // Called when an unit enter the AreaTrigger
        public virtual void OnUnitEnter(Unit unit) { }

        // Called when an unit exit the AreaTrigger, or when the AreaTrigger is removed
        public virtual void OnUnitExit(Unit unit) { }

        // Called when the AreaTrigger is removed
        public virtual void OnRemove() { }

        // Pass parameters between AI
        public virtual void DoAction(int param) { }
        public virtual uint GetData(uint id = 0) { return 0; }
        public virtual void SetData(uint id, uint value) { }
        public virtual void SetGUID(ObjectGuid guid, int id = 0) { }
        public virtual ObjectGuid GetGUID(int id = 0) { return ObjectGuid.Empty; }
    }

    class NullAreaTriggerAI : AreaTriggerAI
    {
        public NullAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }
    }
}
