// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Pets.Generic
{
    [Script]
    class npc_pet_gen_pandaren_monk : NullCreatureAI
    {
        const uint SpellPandarenMonk = 69800;

        Action<TaskContext> focusAction;

        public npc_pet_gen_pandaren_monk(Creature creature) : base(creature)
        {
            focusAction = task =>
            {
                Unit owner = me.GetCharmerOrOwner();
                if (owner != null)
                    me.SetFacingToObject(owner);

                _scheduler.Schedule(TimeSpan.FromSeconds(1), _ =>
                {
                    me.HandleEmoteCommand(Emote.OneshotBow);
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), _ =>
                    {
                        Unit owner = me.GetCharmerOrOwner();
                        if (owner != null)
                            me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                    });
                });
            };
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(1), focusAction);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!_EnterEvadeMode(why))
                return;

            Reset();
        }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            me.InterruptSpell(CurrentSpellTypes.Channeled);
            me.StopMoving();

            switch (emote)
            {
                case TextEmotes.Bow:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), focusAction);
                    break;
                case TextEmotes.Drink:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), _ => me.CastSpell(me, SpellPandarenMonk, false));
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            Unit owner = me.GetCharmerOrOwner();
            if (owner != null)
                if (!me.IsWithinDist(owner, 30.0f))
                    me.InterruptSpell(CurrentSpellTypes.Channeled);
        }
    }

    [Script]
    class npc_pet_gen_soul_trader : ScriptedAI
    {
        const uint SaySoulTraderIntro = 0;

        const uint SpellEtherealOnsummon = 50052;
        const uint SpellEtherealPetRemoveAura = 50055;

        public npc_pet_gen_soul_trader(Creature creature) : base(creature) { }

        public override void OnDespawn()
        {
            Unit owner = me.GetOwner();
            if (owner != null)
                DoCast(owner, SpellEtherealPetRemoveAura);
        }

        public override void JustAppeared()
        {
            Talk(SaySoulTraderIntro);
            Unit owner = me.GetOwner();
            if (owner != null)
                DoCast(owner, SpellEtherealOnsummon);

            base.JustAppeared();
        }
    }

    [Script] // 69735 - Lich Pet OnSummon
    class spell_pet_gen_lich_pet_onsummon : SpellScript
    {
        const uint SpellLichPetAura = 69732;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLichPetAura);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, SpellLichPetAura, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 69736 - Lich Pet Aura Remove
    class spell_pet_gen_lich_pet_aura_Remove : SpellScript
    {
        const uint SpellLichPetAura = 69732;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLichPetAura);
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellLichPetAura);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 69732 - Lich Pet Aura
    class spell_pet_gen_lich_pet_AuraScript : AuraScript
    {
        const uint SpellLichPetAuraOnkill = 69731;

        const uint NpcLichPet = 36979;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLichPetAuraOnkill);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget().IsPlayer();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit owner = GetUnitOwner();

            List<TempSummon> minionList = new();
            owner.GetAllMinionsByEntry(minionList, NpcLichPet);
            foreach (TempSummon minion in minionList)
                owner.CastSpell(minion, SpellLichPetAuraOnkill, true);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 70050 - [Dnd] Lich Pet
    class spell_pet_gen_lich_pet_periodic_emote : AuraScript
    {
        const uint SpellLichPetEmote = 70049;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLichPetEmote);
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
                GetTarget().CastSpell(GetTarget(), SpellLichPetEmote, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 70049 - [Dnd] Lich Pet
    class spell_pet_gen_lich_pet_emote : AuraScript
    {
        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().HandleEmoteCommand(Emote.OneshotCustomSpell01);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 0, AuraType.ModRoot, AuraEffectHandleModes.Real));
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
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
}