// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells
{
    public class AuraCreateInfo
    {
        public int[] BaseAmount { get; set; }
        public Unit Caster { get; set; }
        public ObjectGuid CasterGUID;
        public ObjectGuid CastItemGUID;
        public uint CastItemId { get; set; } = 0;
        public int CastItemLevel { get; set; } = -1;
        public bool IsRefresh { get; set; }
        public bool ResetPeriodicTimer { get; set; } = true;
        internal uint AuraEffectMask;
        internal Difficulty CastDifficulty;

        internal ObjectGuid CastId;
        internal WorldObject Owner;
        internal SpellInfo SpellInfo;

        internal uint TargetEffectMask;

        public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, uint auraEffMask, WorldObject owner)
        {
            CastId = castId;
            SpellInfo = spellInfo;
            CastDifficulty = castDifficulty;
            AuraEffectMask = auraEffMask;
            Owner = owner;

            Cypher.Assert(spellInfo != null);
            Cypher.Assert(auraEffMask != 0);
            Cypher.Assert(owner != null);

            Cypher.Assert(auraEffMask <= SpellConst.MaxEffectMask);
        }

        public void SetCasterGUID(ObjectGuid guid)
        {
            CasterGUID = guid;
        }

        public void SetCaster(Unit caster)
        {
            Caster = caster;
        }

        public void SetBaseAmount(int[] bp)
        {
            BaseAmount = bp;
        }

        public void SetCastItem(ObjectGuid guid, uint itemId, int itemLevel)
        {
            CastItemGUID = guid;
            CastItemId = itemId;
            CastItemLevel = itemLevel;
        }

        public void SetPeriodicReset(bool reset)
        {
            ResetPeriodicTimer = reset;
        }

        public void SetOwnerEffectMask(uint effMask)
        {
            TargetEffectMask = effMask;
        }

        public void SetAuraEffectMask(uint effMask)
        {
            AuraEffectMask = effMask;
        }

        public SpellInfo GetSpellInfo()
        {
            return SpellInfo;
        }

        public uint GetAuraEffectMask()
        {
            return AuraEffectMask;
        }

        public WorldObject GetOwner()
        {
            return Owner;
        }
    }
}