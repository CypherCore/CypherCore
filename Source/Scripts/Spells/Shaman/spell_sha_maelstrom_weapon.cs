using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    //187880 - Maelstrom Weapon
    [SpellScript(187880)]
    public class spell_sha_maelstrom_weapon : AuraScript, IHasAuraEffects, IAuraCheckProc
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(ShamanSpells.SPELL_SHAMAN_MAELSTROM_WEAPON_POWER);
        }

        public bool CheckProc(ProcEventInfo info)
        {
            return info.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack || info.GetDamageInfo().GetAttackType() == WeaponAttackType.OffAttack || info.GetSpellInfo().Id == ShamanSpells.SPELL_SHAMAN_WINDFURY_ATTACK;
        }

        public void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_MAELSTROM_WEAPON_POWER, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
