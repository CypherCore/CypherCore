using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Shaman
{
    // 8042 Earth Shock
    [SpellScript(51556)]
    public class spell_sha_earth_shock : SpellScript, IHasSpellEffects, ISpellOnTakePower
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public void TakePower(SpellPowerCost powerCost)
        {
            _takenPower = powerCost.Amount;
        }

        private void HandleCalcDamage(uint UnnamedParameter)
        {
            SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), _takenPower));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleCalcDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private int _takenPower = 0;
    }
}
