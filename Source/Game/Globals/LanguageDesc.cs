// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class LanguageDesc
    {
        public uint SkillId { get; set; }
        public uint SpellId { get; set; }

        public LanguageDesc()
        {
        }

        public LanguageDesc(uint spellId, uint skillId)
        {
            SpellId = spellId;
            SkillId = skillId;
        }

        public override int GetHashCode()
        {
            return SpellId.GetHashCode() ^ SkillId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is LanguageDesc)
                return (LanguageDesc)obj == this;

            return false;
        }

        public static bool operator ==(LanguageDesc left, LanguageDesc right)
        {
            return left.SpellId == right.SpellId && left.SkillId == right.SkillId;
        }

        public static bool operator !=(LanguageDesc left, LanguageDesc right)
        {
            return !(left == right);
        }
    }
}