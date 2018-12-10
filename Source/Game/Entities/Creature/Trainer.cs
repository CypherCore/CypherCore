using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Network.Packets;
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

        public bool IsCastable() { return Global.SpellMgr.GetSpellInfo(SpellId).HasEffect(SpellEffectName.LearnSpell); }
    }

    public class Trainer
    {
        public Trainer(uint id, TrainerType type, string greeting, List<TrainerSpell> spells)
        {
            _id = id;
            _type = type;
            _spells = spells;

            _greeting[(int)LocaleConstant.enUS] = greeting;
        }

        public void SendSpells(Creature npc, Player player, LocaleConstant locale)
        {
            float reputationDiscount = player.GetReputationPriceDiscount(npc);

            TrainerList trainerList = new TrainerList();
            trainerList.TrainerGUID = npc.GetGUID();
            trainerList.TrainerType = (int)_type;
            trainerList.TrainerID = (int)_id;
            trainerList.Greeting = GetGreeting(locale);

            foreach (TrainerSpell trainerSpell in _spells)
            {
                if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
                    continue;

                TrainerListSpell trainerListSpell = new TrainerListSpell();
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
            TrainerSpell trainerSpell = GetSpell(spellId);
            if (trainerSpell == null || !CanTeachSpell(player, trainerSpell))
            {
                SendTeachFailure(npc, player, spellId, TrainerFailReason.Unavailable);
                return;
            }

            float reputationDiscount = player.GetReputationPriceDiscount(npc);
            long moneyCost = (long)(trainerSpell.MoneyCost * reputationDiscount);
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

        TrainerSpell GetSpell(uint spellId)
        {
            return _spells.Find(trainerSpell => trainerSpell.SpellId == spellId);
        }

        bool CanTeachSpell(Player player, TrainerSpell trainerSpell)
        {
            TrainerSpellState state = GetSpellState(player, trainerSpell);
            if (state != TrainerSpellState.Available)
                return false;

            SpellInfo trainerSpellInfo = Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId);
            if (trainerSpellInfo.IsPrimaryProfessionFirstRank() && player.GetFreePrimaryProfessionPoints() == 0)
                return false;

            return true;
        }

        TrainerSpellState GetSpellState(Player player, TrainerSpell trainerSpell)
        {
            if (player.HasSpell(trainerSpell.SpellId))
                return TrainerSpellState.Known;

            // check race/class requirement
            if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
                return TrainerSpellState.Unavailable;

            // check skill requirement
            if (trainerSpell.ReqSkillLine != 0 && player.GetBaseSkillValue((SkillType)trainerSpell.ReqSkillLine) < trainerSpell.ReqSkillRank)
                return TrainerSpellState.Unavailable;

            foreach (uint reqAbility in trainerSpell.ReqAbility)
                if (reqAbility != 0 && !player.HasSpell(reqAbility))
                    return TrainerSpellState.Unavailable;

            // check level requirement
            if (player.getLevel() < trainerSpell.ReqLevel)
                return TrainerSpellState.Unavailable;

            // check ranks
            bool hasLearnSpellEffect = false;
            bool knowsAllLearnedSpells = true;
            foreach (SpellEffectInfo spellEffect in Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId).GetEffectsForDifficulty(Difficulty.None))
            {
                if (spellEffect == null || !spellEffect.IsEffect(SpellEffectName.LearnSpell))
                    continue;

                hasLearnSpellEffect = true;
                if (!player.HasSpell(spellEffect.TriggerSpell))
                    knowsAllLearnedSpells = false;

                uint previousRankSpellId = Global.SpellMgr.GetPrevSpellInChain(spellEffect.TriggerSpell);
                if (previousRankSpellId != 0)
                    if (!player.HasSpell(previousRankSpellId))
                        return TrainerSpellState.Unavailable;
            }

            if (!hasLearnSpellEffect)
            {
                uint previousRankSpellId = Global.SpellMgr.GetPrevSpellInChain(trainerSpell.SpellId);
                if (previousRankSpellId != 0)
                    if (!player.HasSpell(previousRankSpellId))
                        return TrainerSpellState.Unavailable;
            }
            else if (knowsAllLearnedSpells)
                return TrainerSpellState.Known;

            // check additional spell requirement
            foreach (var spellId in Global.SpellMgr.GetSpellsRequiredForSpellBounds(trainerSpell.SpellId))
                if (!player.HasSpell(spellId))
                    return TrainerSpellState.Unavailable;

            return TrainerSpellState.Available;
        }

        void SendTeachFailure(Creature npc, Player player, uint spellId, TrainerFailReason reason)
        {
            TrainerBuyFailed trainerBuyFailed = new TrainerBuyFailed();
            trainerBuyFailed.TrainerGUID = npc.GetGUID();
            trainerBuyFailed.SpellID = spellId;
            trainerBuyFailed.TrainerFailedReason = reason;
            player.SendPacket(trainerBuyFailed);
        }

        string GetGreeting(LocaleConstant locale)
        {
            if (_greeting[(int)locale].IsEmpty())
                return _greeting[(int)LocaleConstant.enUS];

            return _greeting[(int)locale];
        }

        public void AddGreetingLocale(LocaleConstant locale, string greeting)
        {
            _greeting[(int)locale] = greeting;
        }

        uint _id;
        TrainerType _type;
        List<TrainerSpell> _spells;
        Array<string> _greeting = new Array<string>((int)LocaleConstant.Total);
    }
}
