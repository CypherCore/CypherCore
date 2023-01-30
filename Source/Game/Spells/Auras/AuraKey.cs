// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Game.Entities;

namespace Game.Spells
{
    public class AuraKey : IEquatable<AuraKey>
    {
        public ObjectGuid Caster;
        public uint EffectMask { get; set; }
        public ObjectGuid Item;
        public uint SpellId { get; set; }

        public AuraKey(ObjectGuid caster, ObjectGuid item, uint spellId, uint effectMask)
        {
            Caster = caster;
            Item = item;
            SpellId = spellId;
            EffectMask = effectMask;
        }

        public bool Equals(AuraKey other)
        {
            return other.Caster == Caster && other.Item == Item && other.SpellId == SpellId && other.EffectMask == EffectMask;
        }

        public static bool operator ==(AuraKey first, AuraKey other)
        {
            if (ReferenceEquals(first, other))
                return true;

            if (first is null ||
                other is null)
                return false;

            return first.Equals(other);
        }

        public static bool operator !=(AuraKey first, AuraKey other)
        {
            return !(first == other);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is AuraKey && Equals((AuraKey)obj);
        }

        public override int GetHashCode()
        {
            return new
            {
                Caster,
                Item,
                SpellId,
                EffectMask
            }.GetHashCode();
        }
    }
}