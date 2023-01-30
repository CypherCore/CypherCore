// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Spells
{
    internal class ProcReflectDelayed : BasicEvent
    {
        private readonly Unit _victim;
        private ObjectGuid _casterGuid;

        public ProcReflectDelayed(Unit owner, ObjectGuid casterGuid)
        {
            _victim = owner;
            _casterGuid = casterGuid;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Unit caster = Global.ObjAccessor.GetUnit(_victim, _casterGuid);

            if (!caster)
                return true;

            ProcFlags typeMaskActor = ProcFlags.None;
            ProcFlags typeMaskActionTarget = ProcFlags.TakeHarmfulSpell | ProcFlags.TakeHarmfulAbility;
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
            ProcFlagsSpellPhase spellPhaseMask = ProcFlagsSpellPhase.None;
            ProcFlagsHit hitMask = ProcFlagsHit.Reflect;

            Unit.ProcSkillsAndAuras(caster, _victim, new ProcFlagsInit(typeMaskActor), new ProcFlagsInit(typeMaskActionTarget), spellTypeMask, spellPhaseMask, hitMask, null, null, null);

            return true;
        }
    }
}