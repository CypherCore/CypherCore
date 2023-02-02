// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Spells;

namespace Game.Chat
{
    [CommandGroup("pet")]
    class PetCommands
    {
        [Command("create", RBACPermissions.CommandPetCreate)]
        static bool HandlePetCreateCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            Creature creatureTarget = handler.GetSelectedCreature();

            if (!creatureTarget || creatureTarget.IsPet() || creatureTarget.IsTypeId(TypeId.Player))
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            CreatureTemplate creatureTemplate = creatureTarget.GetCreatureTemplate();
            // Creatures with family CreatureFamily.None crashes the server
            if (creatureTemplate.Family == CreatureFamily.None)
            {
                handler.SendSysMessage("This creature cannot be tamed. (Family id: 0).");
                return false;
            }

            if (!player.GetPetGUID().IsEmpty())
            {
                handler.SendSysMessage("You already have a pet");
                return false;
            }

            // Everything looks OK, create new pet
            Pet pet = player.CreateTamedPetFrom(creatureTarget);

            // "kill" original creature
            creatureTarget.DespawnOrUnsummon();

            // prepare visual effect for levelup
            pet.SetLevel(player.GetLevel() - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetLevel(player.GetLevel());

            // caster have pet now
            player.SetMinion(pet, true);

            pet.SavePetToDB(PetSaveMode.AsCurrent);
            player.PetSpellInitialize();

            return true;
        }

        [Command("learn", RBACPermissions.CommandPetLearn)]
        static bool HandlePetLearnCommand(CommandHandler handler, uint spellId)
        {
            Pet pet = GetSelectedPlayerPetOrOwn(handler);
            if (!pet)
            {
                handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);
                return false;
            }

            if (spellId == 0 || !Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
                return false;

            // Check if pet already has it
            if (pet.HasSpell(spellId))
            {
                handler.SendSysMessage("Pet already has spell: {0}", spellId);
                return false;
            }

            // Check if spell is valid
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo))
            {
                handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellId);
                return false;
            }

            pet.LearnSpell(spellId);

            handler.SendSysMessage("Pet has learned spell {0}", spellId);
            return true;
        }

        [Command("unlearn", RBACPermissions.CommandPetUnlearn)]
        static bool HandlePetUnlearnCommand(CommandHandler handler, uint spellId)
        {
            Pet pet = GetSelectedPlayerPetOrOwn(handler);
            if (!pet)
            {
                handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);
                return false;
            }

            if (pet.HasSpell(spellId))
                pet.RemoveSpell(spellId, false);
            else
                handler.SendSysMessage("Pet doesn't have that spell");

            return true;
        }

        [Command("level", RBACPermissions.CommandPetLevel)]
        static bool HandlePetLevelCommand(CommandHandler handler, int level)
        {
            Pet pet = GetSelectedPlayerPetOrOwn(handler);
            Player owner = pet ? pet.GetOwner() : null;
            if (!pet || !owner)
            {
                handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);
                return false;
            }

            if (level == 0)
                level = (int)(owner.GetLevel() - pet.GetLevel());
            if (level == 0 || level < -SharedConst.StrongMaxLevel || level > SharedConst.StrongMaxLevel)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            int newLevel = (int)pet.GetLevel() + level;
            if (newLevel < 1)
                newLevel = 1;
            else if (newLevel > owner.GetLevel())
                newLevel = (int)owner.GetLevel();

            pet.GivePetLevel(newLevel);
            return true;
        }

        static Pet GetSelectedPlayerPetOrOwn(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();
            if (target)
            {
                if (target.IsTypeId(TypeId.Player))
                    return target.ToPlayer().GetPet();
                if (target.IsPet())
                    return target.ToPet();
                return null;
            }

            Player player = handler.GetSession().GetPlayer();
            return player ? player.GetPet() : null;
        }
    }
}
