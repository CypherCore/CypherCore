using Framework.Constants;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.Scripting.Interfaces.ISpell.EffectHandler;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Shaman
{
    // Summon Fire, Earth & Storm Elemental  - Called By 198067 Fire Elemental, 198103 Earth Elemental, 192249 Storm Elemental
    [SpellScript(new uint[] { 198067, 198103, 192249 })]
    public class spell_shaman_generic_summon_elemental : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private struct Spells
        {
            public const uint PrimalElementalist = 117013;
            public const uint SummonFireElemental = 198067;
            public const uint SummonFireElementalTriggered = 188592;
            public const uint SummonPrimalElementalistFireElemental = 118291;
            public const uint SummonEarthElemental = 198103;
            public const uint SummonEarthElementalTriggered = 188616;
            public const uint SummonPrimalElementalistEarthElemental = 118323;
            public const uint SummonStormElemental = 192249;
            public const uint SummonStormElementalTriggered = 157299;
            public const uint SummonPrimalElementalistStormElemental = 157319;
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.PrimalElementalist, Spells.SummonFireElemental, 
                Spells.SummonFireElementalTriggered, Spells.SummonPrimalElementalistFireElemental, 
                Spells.SummonEarthElemental, Spells.SummonEarthElementalTriggered, Spells.SummonPrimalElementalistEarthElemental,
                Spells.SummonStormElemental, Spells.SummonStormElementalTriggered, Spells.SummonPrimalElementalistStormElemental);
        }
        private void HandleSummon(uint UnnamedParameter)
        {
            uint triggerSpell;

            switch (GetSpellInfo().Id)
            {
                case Spells.SummonFireElemental:
                    triggerSpell = (GetCaster().HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistFireElemental : Spells.SummonFireElementalTriggered;
                    break;
                case Spells.SummonEarthElemental:
                    triggerSpell = (GetCaster().HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistEarthElemental : Spells.SummonEarthElementalTriggered;
                    break;
                case Spells.SummonStormElemental:
                    triggerSpell = (GetCaster().HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistStormElemental : Spells.SummonStormElementalTriggered;
                    break;
                default:
                    triggerSpell = 0;
                    break;
            }

            if (triggerSpell != 0)
            {
                GetCaster().CastSpell(GetCaster(), triggerSpell, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

}
