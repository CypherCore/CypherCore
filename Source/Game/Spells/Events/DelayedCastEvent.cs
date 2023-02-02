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
        private Unit _trigger;
        private Unit _target;
        private uint _spellId;
        private CastSpellExtraArgs _castFlags;
        public DelayedCastEvent(Unit trigger, Unit target, uint spellId, CastSpellExtraArgs args)
        {
            _trigger = trigger;
            _target = target;
            _spellId = spellId;
            _castFlags = args;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            _trigger.CastSpell(_target, _spellId, _castFlags);
            return true;
        }
    }
}
