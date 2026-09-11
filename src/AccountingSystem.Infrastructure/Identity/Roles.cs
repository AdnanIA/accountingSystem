namespace AccountingSystem.Infrastructure.Identity;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Accountant = "Accountant";
    public const string ARClerk = "ARClerk";
    public const string APClerk = "APClerk";
    public const string Viewer = "Viewer";

    public static readonly string[] All = { Admin, Accountant, ARClerk, APClerk, Viewer };
}
