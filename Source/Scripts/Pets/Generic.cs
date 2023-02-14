// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.Pets
{
    namespace Generic
    {
        internal struct SpellIds
        {
            //Mojo
            public const uint FeelingFroggy = 43906;
            public const uint SeductionVisual = 43919;

            //SoulTrader
            public const uint EtherealOnSummon = 50052;
            public const uint EtherealPetRemoveAura = 50055;

            // LichPet
            public const uint LichPetAura = 69732;
            public const uint LichPetAuraOnkill = 69731;
            public const uint LichPetEmote = 70049;
        }

        internal struct CreatureIds
        {
            // LichPet
            public const uint LichPet = 36979;
        }

        internal struct TextIds
        {
            //Mojo
            public const uint SayMojo = 0;

            //SoulTrader
            public const uint SaySoulTraderInto = 0;
        }

        [Script]
        internal class npc_pet_gen_soul_trader : ScriptedAI
        {
            public npc_pet_gen_soul_trader(Creature creature) : base(creature)
            {
            }

            public override void OnDespawn()
            {
                Unit owner = me.GetOwner();

                if (owner != null)
                    DoCast(owner, SpellIds.EtherealPetRemoveAura);
            }

            public override void JustAppeared()
            {
                Talk(TextIds.SaySoulTraderInto);

                Unit owner = me.GetOwner();

                if (owner != null)
                    DoCast(owner, SpellIds.EtherealOnSummon);

                base.JustAppeared();
            }
        }

        [Script] // 69735 - Lich Pet OnSummon
        internal class spell_gen_lich_pet_onsummon : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAura);
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScriptEffect(uint effIndex)
            {
                Unit target = GetHitUnit();
                target.CastSpell(target, SpellIds.LichPetAura, true);
            }
        }

        [Script] // 69736 - Lich Pet Aura Remove
        internal class spell_gen_lich_pet_aura_remove : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAura);
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScriptEffect(uint effIndex)
            {
                GetHitUnit().RemoveAurasDueToSpell(SpellIds.LichPetAura);
            }
        }

        [Script] // 69732 - Lich Pet Aura
        internal class spell_gen_lich_pet_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAuraOnkill);
            }

            public bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetProcTarget().IsPlayer();
            }

            public override void Register()
            {
                AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
            }

            public List<IAuraEffectHandler> AuraEffects { get; } = new();

            private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                List<TempSummon> minionList = new();
                GetUnitOwner().GetAllMinionsByEntry(minionList, CreatureIds.LichPet);

                foreach (Creature minion in minionList)
                    if (minion.IsAIEnabled())
                        minion.GetAI().DoCastSelf(SpellIds.LichPetAuraOnkill);
            }
        }

        [Script] // 70050 - [DND] Lich Pet
        internal class spell_pet_gen_lich_pet_periodic_emote : AuraScript, IHasAuraEffects
        {
            public List<IAuraEffectHandler> AuraEffects { get; } = new();

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetEmote);
            }

            public override void Register()
            {
                AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
            }

            private void OnPeriodic(AuraEffect aurEff)
            {
                // The chance to cast this spell is not 100%.
                // Triggered spell roots creature for 3 sec and plays anim and sound (doesn't require any script).
                // Emote and sound never shows up in sniffs because both comes from spell visual directly.
                // Both 69683 and 70050 can trigger spells at once and are not linked together in any way.
                // Effect of 70050 is overlapped by effect of 69683 but not instantly (69683 is a series of spell casts, takes longer to execute).
                // However, for some reason Emote is not played if creature is idle and only if creature is moving or is already rooted.
                // For now it's scripted manually in script below to play Emote always.
                if (RandomHelper.randChance(50))
                    GetTarget().CastSpell(GetTarget(), SpellIds.LichPetEmote, true);
            }
        }

        [Script] // 70049 - [DND] Lich Pet
        internal class spell_pet_gen_lich_pet_emote : AuraScript, IHasAuraEffects
        {
            public List<IAuraEffectHandler> AuraEffects { get; } = new();

            public override void Register()
            {
                AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.ModRoot, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            }

            private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                GetTarget().HandleEmoteCommand(Emote.OneshotCustomSpell01);
            }
        }

        [Script] // 69682 - Lil' K.T. Focus
        internal class spell_pet_gen_lich_pet_focus : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScript(uint effIndex)
            {
                GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue());
            }
        }
    }
}