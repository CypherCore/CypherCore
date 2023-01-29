// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;

namespace Game
{
    public class PlayerChoiceResponseLocale
    {
        public StringArray Answer { get; set; } = new((int)Locale.Total);
        public StringArray ButtonTooltip { get; set; } = new((int)Locale.Total);
        public StringArray Confirmation { get; set; } = new((int)Locale.Total);
        public StringArray Description { get; set; } = new((int)Locale.Total);
        public StringArray Header { get; set; } = new((int)Locale.Total);
        public StringArray SubHeader { get; set; } = new((int)Locale.Total);
    }
}