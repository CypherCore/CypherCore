// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells
{
    public class CorpseTargetInfo : TargetInfoBase
    {
        public ObjectGuid TargetGUID;
        public ulong TimeDelay;

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            Corpse corpse = ObjectAccessor.GetCorpse(spell.GetCaster(), TargetGUID);

            if (corpse == null)
                return;

            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, null, null, corpse, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }
}