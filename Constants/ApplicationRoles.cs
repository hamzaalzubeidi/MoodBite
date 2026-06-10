namespace MoodBite.Constants
{
    public static class ApplicationRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";

        // Reserved for the MoodBite Clinic B2B layer. Do not use until clinic access
        // checks and tenant scoping are implemented.
        public const string ClinicOwner = "ClinicOwner";
        public const string Dietitian = "Dietitian";
        public const string ClinicStaff = "ClinicStaff";
        public const string ClinicAreaAccess = Admin + "," + ClinicOwner + "," + Dietitian + "," + ClinicStaff;

        public static readonly string[] SeededRoles =
        [
            Admin,
            User
        ];

        public static readonly string[] FutureClinicRoles =
        [
            ClinicOwner,
            Dietitian,
            ClinicStaff
        ];

        public static readonly string[] AdminAssignableRoles =
        [
            User,
            Admin
        ];

        public static bool IsAdminAssignable(string role) =>
            AdminAssignableRoles.Contains(role, StringComparer.Ordinal);
    }
}
