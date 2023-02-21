// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.DeathKnight;

[Script]
public class spell_dk_commander_of_the_dead_aura : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();
    private List<WorldObject> saveTargets = new();
    public override bool Validate(SpellInfo spellInfo) {
        return ValidateSpellInfo(DeathKnightSpells.DT_COMMANDER_BUFF);
    }

    public override void Register() {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetListOfUnits, 0, Targets.UnitCasterAndSummons));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = GetCaster();
        var target = GetHitUnit();
        if (caster != null) {
            if(saveTargets.Count> 0) {
                saveTargets.ForEach(k => {
                    caster.CastSpell(k, DeathKnightSpells.DT_COMMANDER_BUFF, true);
                });
                saveTargets.Clear(); 
            }
        }
    }

    private void GetListOfUnits(List<WorldObject> targets) {
        targets.RemoveIf((WorldObject target) =>
        {
            if (!target.ToUnit() || target.ToPlayer())
                return true;

            if (target.ToCreature().GetOwner() != GetCaster())
                return true;

            if (target.ToCreature().GetEntry() != DeathKnightSpells.DKNPCS.GARGOYLE 
            && target.ToCreature().GetEntry() != DeathKnightSpells.DKNPCS.AOTD_GHOUL)
                return true;

            saveTargets.Add(target);
            return false;
        });

        targets.Clear();
    }
}

[SpellScript(390259)]
public class spell_dk_commander_of_the_dead_aura_proc : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetSpellInfo() != null)
        {
            return eventInfo.GetSpellInfo().Id == DeathKnightSpells.DARK_TRANSFORMATION;
        }
        return false;
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(eventInfo.GetProcTarget(), DeathKnightSpells.DT_COMMANDER_BUFF, true);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }
}