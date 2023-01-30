namespace Game.Accounts;

public enum RBACCommandResult
{
    OK,
    CantAddAlreadyAdded,
    CantRevokeNotInList,
    InGrantedList,
    InDeniedList,
    IdDoesNotExists
}