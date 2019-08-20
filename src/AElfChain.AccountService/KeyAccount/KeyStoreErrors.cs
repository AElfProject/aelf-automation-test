namespace AElfChain.AccountService.KeyAccount
{
    public enum KeyStoreErrors
    {
        None = 0,
        AccountAlreadyUnlocked = 1,
        WrongPassword = 2,
        WrongAccountFormat = 3,
        AccountFileNotFound = 4
    }
}