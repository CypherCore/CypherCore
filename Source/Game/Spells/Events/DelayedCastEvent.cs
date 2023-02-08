using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Spells.Events
{
    public class DelayedCastEvent : BasicEvent
    {
        public Unit Trigger { get; set; }
        public Unit Target { get; set; }
        public uint SpellId { get; set; }
        public CastSpellExtraArgs CastFlags { get; set; }
        public DelayedCastEvent(Unit trigger, Unit target, uint spellId, CastSpellExtraArgs args)
        {
            Trigger = trigger;
            Target = target;
            SpellId = spellId;
            CastFlags = args;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Trigger.CastSpell(Target, SpellId, CastFlags);
            return true;
        }
    }
}
