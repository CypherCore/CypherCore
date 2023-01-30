// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;

namespace Game.Entities
{
    internal class PetLoadQueryHolder : SQLQueryHolder<PetLoginQueryLoad>
    {
        public PetLoadQueryHolder(ulong ownerGuid, uint petNumber)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
            stmt.AddValue(0, ownerGuid);
            stmt.AddValue(1, petNumber);
            SetQuery(PetLoginQueryLoad.DeclinedNames, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Auras, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.AuraEffects, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Spells, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Cooldowns, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Charges, stmt);
        }
    }
}