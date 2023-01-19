// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Pets
{
    namespace Generic
    {
        struct SpellIds
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

        struct CreatureIds
        {
            // LichPet
            public const uint LichPet = 36979;
        }

        struct TextIds
        {
            //Mojo
            public const uint SayMojo = 0;

            //SoulTrader
            public const uint SaySoulTraderInto = 0;
        }

        [Script]
        class npc_pet_gen_soul_trader : ScriptedAI
        {
            public npc_pet_gen_soul_trader(Creature creature) : base(creature) { }

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
        class spell_gen_lich_pet_onsummon : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAura);
            }

            void HandleScriptEffect(uint effIndex)
            {
                Unit target = GetHitUnit();
                target.CastSpell(target, SpellIds.LichPetAura, true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 69736 - Lich Pet Aura Remove
        class spell_gen_lich_pet_aura_remove : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAura);
            }

            void HandleScriptEffect(uint effIndex)
            {
                GetHitUnit().RemoveAurasDueToSpell(SpellIds.LichPetAura);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 69732 - Lich Pet Aura
        class spell_gen_lich_pet_aura : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetAuraOnkill);
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetProcTarget().IsPlayer();
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                List<TempSummon> minionList = new();
                GetUnitOwner().GetAllMinionsByEntry(minionList, CreatureIds.LichPet);
                foreach (Creature minion in minionList)
                    if (minion.IsAIEnabled())
                        minion.GetAI().DoCastSelf(SpellIds.LichPetAuraOnkill);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
            }
        }

        [Script] // 70050 - [DND] Lich Pet
        class spell_pet_gen_lich_pet_periodic_emote : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.LichPetEmote);
            }

            void OnPeriodic(AuraEffect aurEff)
            {
                // The chance to cast this spell is not 100%.
                // Triggered spell roots creature for 3 sec and plays anim and sound (doesn't require any script).
                // Emote and sound never shows up in sniffs because both comes from spell visual directly.
                // Both 69683 and 70050 can trigger spells at once and are not linked together in any way.
                // Effect of 70050 is overlapped by effect of 69683 but not instantly (69683 is a series of spell casts, takes longer to execute).
                // However, for some reason emote is not played if creature is idle and only if creature is moving or is already rooted.
                // For now it's scripted manually in script below to play emote always.
                if (RandomHelper.randChance(50))
                    GetTarget().CastSpell(GetTarget(), SpellIds.LichPetEmote, true);
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
            }
        }

        [Script] // 70049 - [DND] Lich Pet
        class spell_pet_gen_lich_pet_emote : AuraScript
        {
            void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                GetTarget().HandleEmoteCommand(Emote.OneshotCustomSpell01);
            }

            public override void Register()
            {
                AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.ModRoot, AuraEffectHandleModes.Real));
            }
        }

        [Script] // 69682 - Lil' K.T. Focus
        class spell_pet_gen_lich_pet_focus : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
            }

            void HandleScript(uint effIndex)
            {
                GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue());
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }
    }
}
