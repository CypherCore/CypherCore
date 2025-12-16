using Framework.Constants;
using Game.BattlePets;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class TrainerSpell
    {
        public uint SpellId;
        public uint MoneyCost;
        public uint ReqSkillLine;
        public uint ReqSkillRank;
        public Array<uint> ReqAbility = new(3);
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
            float reputationDiscount = player.GetReputationPriceDiscount(npc);

            TrainerList trainerList = new();
            trainerList.TrainerGUID = npc.GetGUID();
            trainerList.TrainerType = (sbyte)_type;
            trainerList.TrainerID = (int)_id;
            trainerList.Greeting = GetGreeting(locale);

            foreach (TrainerSpell trainerSpell in _spells)
            {
                if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingTrainerSpellConditions(_id, trainerSpell.SpellId, player))
                {
                    Log.outDebug(LogFilter.Condition, $"SendSpells: conditions not met for trainer id {_id} spell {trainerSpell.SpellId} player '{player.GetName()}' ({player.GetGUID()})");
                    continue;
                }

                TrainerListSpell trainerListSpell = new();
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

            bool sendSpellVisual = true;
            var speciesEntry = BattlePetMgr.GetBattlePetSpeciesBySpell(trainerSpell.SpellId);
            if (speciesEntry != null)
            {
                if (player.GetSession().GetBattlePetMgr().HasMaxPetCount(speciesEntry, player.GetGUID()))
                {
                    // Don't send any error to client (intended)
                    return;
                }

                sendSpellVisual = false;
            }

            float reputationDiscount = player.GetReputationPriceDiscount(npc);
            long moneyCost = (long)(trainerSpell.MoneyCost * reputationDiscount);
            if (!player.HasEnoughMoney(moneyCost))
            {
                SendTeachFailure(npc, player, spellId, TrainerFailReason.NotEnoughMoney);
                return;
            }

            player.ModifyMoney(-moneyCost);

            if (sendSpellVisual)
            {
                npc.SendPlaySpellVisualKit(179, 0, 0);     // 53 SpellCastDirected
                player.SendPlaySpellVisualKit(362, 1, 0);  // 113 EmoteSalute
            }

            // learn explicitly or cast explicitly
            if (trainerSpell.IsCastable())
                player.CastSpell(player, trainerSpell.SpellId, true);
            else
            {
                bool dependent = false;

                if (speciesEntry != null)
                {
                    player.GetSession().GetBattlePetMgr().AddPet(speciesEntry.Id, BattlePetMgr.SelectPetDisplay(speciesEntry), BattlePetMgr.RollPetBreed(speciesEntry.Id), BattlePetMgr.GetDefaultPetQuality(speciesEntry.Id));
                    // If the spell summons a battle pet, we fake that it has been learned and the battle pet is added
                    // marking as dependent prevents saving the spell to database (intended)
                    dependent = true;
                }

                player.LearnSpell(trainerSpell.SpellId, dependent);
            }
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

            SpellInfo trainerSpellInfo = Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId, Difficulty.None);
            if (trainerSpellInfo.IsPrimaryProfessionFirstRank() && player.GetFreePrimaryProfessionPoints() == 0)
                return false;

            foreach (SpellEffectInfo effect in trainerSpellInfo.GetEffects())
            {
                if (!effect.IsEffect(SpellEffectName.LearnSpell))
                    continue;

                SpellInfo learnedSpellInfo = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell, Difficulty.None);
                if (learnedSpellInfo != null && learnedSpellInfo.IsPrimaryProfessionFirstRank() && player.GetFreePrimaryProfessionPoints() == 0)
                    return false;
            }

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
            if (player.GetLevel() < trainerSpell.ReqLevel)
                return TrainerSpellState.Unavailable;

            // check ranks
            bool hasLearnSpellEffect = false;
            bool knowsAllLearnedSpells = true;
            foreach (var spellEffectInfo in Global.SpellMgr.GetSpellInfo(trainerSpell.SpellId, Difficulty.None).GetEffects())
            {
                if (!spellEffectInfo.IsEffect(SpellEffectName.LearnSpell))
                    continue;

                hasLearnSpellEffect = true;
                if (!player.HasSpell(spellEffectInfo.TriggerSpell))
                    knowsAllLearnedSpells = false;
            }

            if (hasLearnSpellEffect && knowsAllLearnedSpells)
                return TrainerSpellState.Known;

            return TrainerSpellState.Available;
        }

        void SendTeachFailure(Creature npc, Player player, uint spellId, TrainerFailReason reason)
        {
            TrainerBuyFailed trainerBuyFailed = new();
            trainerBuyFailed.TrainerGUID = npc.GetGUID();
            trainerBuyFailed.SpellID = spellId;
            trainerBuyFailed.TrainerFailedReason = reason;
            player.SendPacket(trainerBuyFailed);
        }

        string GetGreeting(Locale locale)
        {
            if (_greeting[(int)locale].IsEmpty())
                return _greeting[(int)Locale.enUS];

            return _greeting[(int)locale];
        }

        public void AddGreetingLocale(Locale locale, string greeting)
        {
            _greeting[(int)locale] = greeting;
        }

        uint _id;
        TrainerType _type;
        List<TrainerSpell> _spells;
        string[] _greeting = new string[(int)Locale.Total];
    }
}
