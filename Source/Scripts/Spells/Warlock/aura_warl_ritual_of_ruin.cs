using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Warlock
{
    [Script]
    internal class aura_warl_ritual_of_ruin : ScriptObjectAutoAdd, IPlayerOnModifyPower
    {
        public Class PlayerClass { get; } = Class.Warlock;

        public aura_warl_ritual_of_ruin() : base("aura_warl_ritual_of_ruin")
        {
        }

        public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
        {
            if (regen || power != PowerType.SoulShards)
                return;

            var shardCost = oldValue - newValue;

            if (shardCost <= 0)
                return;

            var soulShardsSpent = player.VariableStorage.GetValue(WarlockSpells.RITUAL_OF_RUIN.ToString(), 0) + shardCost;
            var needed = Global.SpellMgr.GetSpellInfo(WarlockSpells.RITUAL_OF_RUIN).GetEffect(0).BasePoints * 10; // each soul shard is 10

            if (soulShardsSpent > needed)
            {
                player.AddAura(WarlockSpells.RITUAL_OF_RUIN_FREE_CAST_AURA, player);
                soulShardsSpent -= needed;
            }

            player.VariableStorage.Set(WarlockSpells.RITUAL_OF_RUIN.ToString(), soulShardsSpent);
        }

        public void OnProc(ProcEventInfo info)
        {
            
        }
    }
}
