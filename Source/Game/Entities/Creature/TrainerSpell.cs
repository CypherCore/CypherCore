using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

public class TrainerSpell
{
    public uint MoneyCost { get; set; }
    public Array<uint> ReqAbility { get; set; } = new(3);
    public byte ReqLevel { get; set; }
    public uint ReqSkillLine { get; set; }
    public uint ReqSkillRank { get; set; }
    public uint SpellId { get; set; }

    public bool IsCastable()
    {
        return Global.SpellMgr.GetSpellInfo(SpellId, Difficulty.None).HasEffect(SpellEffectName.LearnSpell);
    }
}