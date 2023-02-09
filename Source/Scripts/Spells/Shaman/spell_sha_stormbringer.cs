using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Stormbringer - 201845
    [SpellScript(201845)]
    public class spell_sha_stormbringer : AuraScript, IAuraCheckProc, IAuraOnProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack;
        }

        public void OnProc(ProcEventInfo info)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_STORMBRINGER_PROC, true);
                caster.GetSpellHistory().ResetCooldown(ShamanSpells.SPELL_SHAMAN_STORMSTRIKE, true);
            }
        }
    }
}
