using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Shaman
{
    // 188070 Healing Surge
    [SpellScript(188070)]
    public class spell_sha_healing_surge : SpellScript, IHasSpellEffects, ISpellCalculateCastTime
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public int CalcCastTime(int castTime)
        {
            int requiredMaelstrom = GetEffectInfo(2).BasePoints;
            if (GetCaster().GetPower(PowerType.Maelstrom) >= requiredMaelstrom)
            {
                castTime = 0;
                _takenPower = requiredMaelstrom;
            }
        }

        private void HandleEnergize(uint UnnamedParameter)
        {
            SetEffectValue(-_takenPower);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
        }

        private int _takenPower = 0;
    }
}
