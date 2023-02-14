// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces
{
    public interface ISpellScript : IBaseSpellScript
    {
        void CreateItem(uint itemId, ItemContext context);
        void FinishCast(SpellCastResult result, int? param1 = null, int? param2 = null);
        Difficulty GetCastDifficulty();
        Unit GetCaster();
        Item GetCastItem();
        long GetCorpseTargetCountForEffect(uint effect);
        SpellEffectInfo GetEffectInfo();
        SpellEffectInfo GetEffectInfo(uint effIndex);
        int GetEffectValue();
        float GetEffectVariance();
        WorldLocation GetExplTargetDest();
        GameObject GetExplTargetGObj();
        Item GetExplTargetItem();
        Unit GetExplTargetUnit();
        WorldObject GetExplTargetWorldObject();
        long GetGameObjectTargetCountForEffect(uint effect);
        GameObject GetGObjCaster();
        Aura GetHitAura(bool dynObjAura = false);
        Corpse GetHitCorpse();
        Creature GetHitCreature();
        int GetHitDamage();
        WorldLocation GetHitDest();
        GameObject GetHitGObj();
        int GetHitHeal();
        Item GetHitItem();
        Player GetHitPlayer();
        Unit GetHitUnit();
        long GetItemTargetCountForEffect(uint effect);
        Unit GetOriginalCaster();
        Spell GetSpell();
        SpellInfo GetSpellInfo();
        SpellValue GetSpellValue();
        SpellInfo GetTriggeringSpell();
        long GetUnitTargetCountForEffect(uint effect);
        bool IsHitCrit();
        bool IsInCheckCastHook();
        bool IsInEffectHook();
        bool IsInHitPhase();
        bool IsInTargetHook();
        void PreventHitAura();
        void PreventHitDamage();
        void PreventHitDefaultEffect(uint effIndex);
        void PreventHitEffect(uint effIndex);
        void SelectRandomInjuredTargets(List<WorldObject> targets, uint maxTargets, bool prioritizePlayers);
        void SetCustomCastResultMessage(SpellCustomErrors result);
        void SetEffectValue(int value);
        void SetEffectVariance(float variance);
        void SetExplTargetDest(WorldLocation loc);
        void SetHitDamage(int damage);
        void SetHitHeal(int heal);
        void _FinishScriptCall();
        void _InitHit();
        bool _IsDefaultEffectPrevented(uint effIndex);
        bool _IsEffectPrevented(uint effIndex);
        bool _Load(Spell spell);
        void _PrepareScriptCall(SpellScriptHookType hookType);
    }
}