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
        long GetCorpseTargetCountForEffect(int effect);
        SpellEffectInfo GetEffectInfo();
        SpellEffectInfo GetEffectInfo(int effIndex);
        double GetEffectValue();
        double GetEffectVariance();
        WorldLocation GetExplTargetDest();
        GameObject GetExplTargetGObj();
        Item GetExplTargetItem();
        Unit GetExplTargetUnit();
        WorldObject GetExplTargetWorldObject();
        long GetGameObjectTargetCountForEffect(int effect);
        GameObject GetGObjCaster();
        Aura GetHitAura(bool dynObjAura = false);
        Corpse GetHitCorpse();
        Creature GetHitCreature();
        double GetHitDamage();
        WorldLocation GetHitDest();
        GameObject GetHitGObj();
        double GetHitHeal();
        Item GetHitItem();
        Player GetHitPlayer();
        Unit GetHitUnit();
        long GetItemTargetCountForEffect(int effect);
        Unit GetOriginalCaster();
        Spell GetSpell();
        SpellInfo GetSpellInfo();
        SpellValue GetSpellValue();
        SpellInfo GetTriggeringSpell();
        long GetUnitTargetCountForEffect(int effect);
        bool IsHitCrit();
        bool IsInCheckCastHook();
        bool IsInEffectHook();
        bool IsInHitPhase();
        bool IsInTargetHook();
        void PreventHitAura();
        void PreventHitDamage();
        void PreventHitDefaultEffect(int effIndex);
        void PreventHitEffect(int effIndex);
        void SelectRandomInjuredTargets(List<WorldObject> targets, uint maxTargets, bool prioritizePlayers);
        void SetCustomCastResultMessage(SpellCustomErrors result);
        void SetEffectValue(double value);
        void SetEffectVariance(double variance);
        void SetExplTargetDest(WorldLocation loc);
        void SetHitDamage(double damage);
        void SetHitHeal(double heal);
        void _FinishScriptCall();
        void _InitHit();
        bool _IsDefaultEffectPrevented(int effIndex);
        bool _IsEffectPrevented(int effIndex);
        bool _Load(Spell spell);
        void _PrepareScriptCall(SpellScriptHookType hookType);
    }
}