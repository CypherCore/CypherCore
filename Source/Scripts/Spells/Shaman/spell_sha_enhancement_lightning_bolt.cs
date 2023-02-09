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
    // 187837 - Lightning Bolt
    [SpellScript(187837)]
    public class spell_sha_enhancement_lightning_bolt : SpellScript, IHasSpellEffects, ISpellOnTakePower
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(ShamanSpells.SPELL_SHAMAN_OVERCHARGE);
        }

        public void TakePower(SpellPowerCost powerCost)
        {
            _maxTakenPower = 0;
            _maxDamagePercent = 0;

            Aura overcharge = GetCaster().GetAura(ShamanSpells.SPELL_SHAMAN_OVERCHARGE);
            if (overcharge != null)
            {
                _maxTakenPower = overcharge.GetSpellInfo().GetEffect(0).BasePoints;
                _maxDamagePercent = overcharge.GetSpellInfo().GetEffect(1).BasePoints;
            }

            _takenPower = powerCost.Amount = Math.Min(GetCaster().GetPower(PowerType.Maelstrom), _maxTakenPower);
        }

        private void HandleDamage(uint UnnamedParameter)
        {
            if (_maxTakenPower > 0)
            {
                var increasedDamagePercent = MathFunctions.CalculatePct(_maxDamagePercent, (float)_takenPower / (float)_maxTakenPower * 100.0f);
                var hitDamage = MathFunctions.CalculatePct(GetHitDamage(), 100 + increasedDamagePercent);
                SetHitDamage(hitDamage);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private int _takenPower;
        private int _maxTakenPower;
        private int _maxDamagePercent;
    }
}
