namespace CodeGraphWeb.Constants;

public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string TechLead = "TechLead";
    public const string User = "User";

    public static readonly string[] All = [SystemAdmin, CompanyAdmin, TechLead, User];
}
