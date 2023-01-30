// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Spells
{
    public class CastSpellExtraArgs
    {
        public Difficulty CastDifficulty { get; set; }
        public Item CastItem { get; set; }
        public object CustomArg { get; set; }
        public ObjectGuid OriginalCaster = ObjectGuid.Empty;
        public ObjectGuid OriginalCastId = ObjectGuid.Empty;
        public int? OriginalCastItemLevel { get; set; }
        public Dictionary<SpellValueMod, int> SpellValueOverrides { get; set; } = new();
        public TriggerCastFlags TriggerFlags { get; set; }
        public AuraEffect TriggeringAura { get; set; }
        public Spell TriggeringSpell { get; set; }

        public CastSpellExtraArgs()
        {
        }

        public CastSpellExtraArgs(bool triggered)
        {
            TriggerFlags = triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None;
        }

        public CastSpellExtraArgs(TriggerCastFlags trigger)
        {
            TriggerFlags = trigger;
        }

        public CastSpellExtraArgs(Item item)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            CastItem = item;
        }

        public CastSpellExtraArgs(Spell triggeringSpell)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            SetTriggeringSpell(triggeringSpell);
        }

        public CastSpellExtraArgs(AuraEffect eff)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            SetTriggeringAura(eff);
        }

        public CastSpellExtraArgs(Difficulty castDifficulty)
        {
            CastDifficulty = castDifficulty;
        }

        public CastSpellExtraArgs(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(mod, val);
        }

        public CastSpellExtraArgs SetTriggerFlags(TriggerCastFlags flag)
        {
            TriggerFlags = flag;

            return this;
        }

        public CastSpellExtraArgs SetCastItem(Item item)
        {
            CastItem = item;

            return this;
        }

        public CastSpellExtraArgs SetTriggeringSpell(Spell triggeringSpell)
        {
            TriggeringSpell = triggeringSpell;

            if (triggeringSpell != null)
            {
                OriginalCastItemLevel = triggeringSpell.CastItemLevel;
                OriginalCastId = triggeringSpell.CastId;
            }

            return this;
        }

        public CastSpellExtraArgs SetTriggeringAura(AuraEffect triggeringAura)
        {
            TriggeringAura = triggeringAura;

            if (triggeringAura != null)
                OriginalCastId = triggeringAura.GetBase().GetCastId();

            return this;
        }

        public CastSpellExtraArgs SetOriginalCaster(ObjectGuid guid)
        {
            OriginalCaster = guid;

            return this;
        }

        public CastSpellExtraArgs SetCastDifficulty(Difficulty castDifficulty)
        {
            CastDifficulty = castDifficulty;

            return this;
        }

        public CastSpellExtraArgs SetOriginalCastId(ObjectGuid castId)
        {
            OriginalCastId = castId;

            return this;
        }

        public CastSpellExtraArgs AddSpellMod(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(mod, val);

            return this;
        }

        public CastSpellExtraArgs SetCustomArg(object customArg)
        {
            CustomArg = customArg;

            return this;
        }
    }
}