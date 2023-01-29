// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;

namespace Game
{
    internal class AccountInfoQueryHolder : SQLQueryHolder<AccountInfoQueryLoad>
    {
        public void Initialize(uint accountId, uint battlenetAccountId)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOYS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountToys, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BATTLE_PETS);
            stmt.AddValue(0, battlenetAccountId);
            stmt.AddValue(1, Global.WorldMgr.GetRealmId().Index);
            SetQuery(AccountInfoQueryLoad.BattlePets, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BATTLE_PET_SLOTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.BattlePetSlot, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountHeirlooms, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MOUNTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.Mounts, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SelBnetCharacterCountsByAccountId);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalRealmCharacterCounts, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemAppearances, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemFavoriteAppearances, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_TRANSMOG_ILLUSIONS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.TransmogIllusions, stmt);
        }
    }
}
