// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;

namespace Game
{
    internal class AccountInfoQueryHolderPerRealm : SQLQueryHolder<AccountInfoQueryLoad>
    {
        public void Initialize(uint accountId, uint battlenetAccountId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_DATA);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_TUTORIALS);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.TutorialsIndexPerRealm, stmt);
        }
    }
}
