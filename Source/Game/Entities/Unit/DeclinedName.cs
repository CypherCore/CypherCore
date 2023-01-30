using Framework.Collections;
using Framework.Constants;

namespace Game.Entities;

public class DeclinedName
{
    public StringArray Name { get; set; } = new(SharedConst.MaxDeclinedNameCases);
}