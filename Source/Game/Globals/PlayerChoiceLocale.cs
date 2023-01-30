// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;

namespace Game
{
    public class PlayerChoiceLocale
    {
        public StringArray Question { get; set; } = new((int)Locale.Total);
        public Dictionary<int /*ResponseId*/, PlayerChoiceResponseLocale> Responses { get; set; } = new();
    }
}