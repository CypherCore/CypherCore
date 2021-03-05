using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
    public class TrainerSpell
    {
        public uint SpellId;
        public uint MoneyCost;
        public uint ReqSkillLine;
        public uint ReqSkillRank;
        public Array<uint> ReqAbility = new Array<uint>(3);
        public byte ReqLevel;

        public bool IsCastable() { return Global.SpellMgr.GetSpellInfo(SpellId, Difficulty.None).HasEffect(SpellEffectName.LearnSpell); }
    }

    public class Trainer
    {
        public Trainer(uint id, TrainerType type, string greeting, List<TrainerSpell> spells)
        {
            _id = id;
            _type = type;
            _spells = spells;

            _greeting[(int)Locale.enUS] = greeting;
        }

        public void SendSpells(Creature npc, Player player, Locale locale)
        {
            var reputationDiscount = player.GetReputationPriceDiscount(npc);

            var trainerList = new TrainerList();
            trainerList.TrainerGUID = npc.GetGUID();
            trainerList.TrainerType = (int)_type;
            trainerList.TrainerID = (int)_id;
            trainerList.Greeting = GetGreeting(locale);

            foreach (var trainerSpell in _spells)
            {
                if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
                    continue;

                var trainerListSpell = new TrainerListSpell();
                trainerListSpell.SpellID = trainerSpell.SpellId;
                trainerListSpell.MoneyCost = (uint)(trainerSpell.MoneyCost * reputationDiscount);
                trainerListSpell.ReqSkillLine = trainerSpell.ReqSkillLine;
                trainerListSpell.ReqSkillRank = trainerSpell.ReqSkillRank;
                trainerListSpell.ReqAbility = trainerSpell.ReqAbility.ToArray();
                trainerListSpell.Usable = GetSpellState(player, trainerSpell);
                trainerListSpell.ReqLevel = trainerSpell.ReqLevel;
                trainerList.Spells.Add(trainerListSpell);
            }

            player.SendPacket(trainerList);
        }

        public void TeachSpell(Creature npc, Player player, uint spellId)
        {
            var trainerSpell = GetSpell(spellId);
            if (trainerSpell == null || !CanTeachSpell(player, trainerSpell))
            {
                SendTeachFailure(npc, player, spellId, TrainerFailReason.Unavailable);
                return;
            }

            var reputationDiscount = player.GetReputationPriceDiscount(npc);
            var moneyCost = (long)(trainerSpell.MoneyCost * reputationDiscount);
            if (!player.HasEnoughMoney(moneyCost))
            {
                SendTeachFailure(npc, player, spellId, TrainerFailReason.NotEnoughMoney);
                return;
            }

            player.ModifyMoney(-moneyCost);

            npc.SendPlaySpellVisualKit(179, 0, 0);     // 53 SpellCastDirected
            player.SendPlaySpellVisualKit(362, 1, 0);  // 113 EmoteSalute

            // learn explicitly or cast explicitly
            if (trainerSpell.IsCastable())
                player.CastSpell(player, trainerSpell.SpellId, true);
            else
                player.LearnSpell(trainerSpell.SpellId, false);
        }

        private TrainerSpell GetSpell(uint spellId)
        {
            return _spells.Find(trainerSpell => trainerSpell.SpellId == spellId);
        }

        private bool CanTeachSpell(Player player, TrainerSpell trainerSpell)
        {
            var state = GetSpellState(player, trainerSpell);
            if (state != TrainerSpellState.Available)
                return false;

            var trainerSpellInfo = Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId, Difficulty.None);
            if (trainerSpellInfo.IsPrimaryProfessionFirstRank() && player.GetFreePrimaryProfessionPoints() == 0)
                return false;

            return true;
        }

        private TrainerSpellState GetSpellState(Player player, TrainerSpell trainerSpell)
        {
            if (player.HasSpell(trainerSpell.SpellId))
                return TrainerSpellState.Known;

            // check race/class requirement
            if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
                return TrainerSpellState.Unavailable;

            // check skill requirement
            if (trainerSpell.ReqSkillLine != 0 && player.GetBaseSkillValue((SkillType)trainerSpell.ReqSkillLine) < trainerSpell.ReqSkillRank)
                return TrainerSpellState.Unavailable;

            foreach (var reqAbility in trainerSpell.ReqAbility)
                if (reqAbility != 0 && !player.HasSpell(reqAbility))
                    return TrainerSpellState.Unavailable;

            // check level requirement
            if (player.GetLevel() < trainerSpell.ReqLevel)
                return TrainerSpellState.Unavailable;

            // check ranks
            var hasLearnSpellEffect = false;
            var knowsAllLearnedSpells = true;
            foreach (var spellEffect in Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId, Difficulty.None).GetEffects())
            {
                if (spellEffect == null || !spellEffect.IsEffect(SpellEffectName.LearnSpell))
                    continue;

                hasLearnSpellEffect = true;
                if (!player.HasSpell(spellEffect.TriggerSpell))
                    knowsAllLearnedSpells = false;
            }

            if (hasLearnSpellEffect && knowsAllLearnedSpells)
                return TrainerSpellState.Known;

            return TrainerSpellState.Available;
        }

        private void SendTeachFailure(Creature npc, Player player, uint spellId, TrainerFailReason reason)
        {
            var trainerBuyFailed = new TrainerBuyFailed();
            trainerBuyFailed.TrainerGUID = npc.GetGUID();
            trainerBuyFailed.SpellID = spellId;
            trainerBuyFailed.TrainerFailedReason = reason;
            player.SendPacket(trainerBuyFailed);
        }

        private string GetGreeting(Locale locale)
        {
            if (_greeting[(int)locale].IsEmpty())
                return _greeting[(int)Locale.enUS];

            return _greeting[(int)locale];
        }

        public void AddGreetingLocale(Locale locale, string greeting)
        {
            _greeting[(int)locale] = greeting;
        }

        private uint _id;
        private TrainerType _type;
        private List<TrainerSpell> _spells;
        private Array<string> _greeting = new Array<string>((int)Locale.Total);
    }
}
