public sealed class AccountProfileData
{
    public string Account { get; }
    public string Name { get; }
    public AcType Type { get; }

    public AccountProfileData(string account, string name, AcType type)
    {
        Account = account;
        Name = name;
        Type = type;
    }
}
