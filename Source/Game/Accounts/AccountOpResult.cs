namespace Game;

public enum AccountOpResult
{
    Ok,
    NameTooLong,
    PassTooLong,
    EmailTooLong,
    NameAlreadyExist,
    NameNotExist,
    DBInternalError,
    BadLink
}