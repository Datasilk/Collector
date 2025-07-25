namespace Collector.Auth.Policies
{
    public static class AuthConstants
    {
        public enum RoleType
        {
            admin = 1, 
            owner = 2,
            manager = 3, 
            user = 4
        }

        public enum Policy
        {
            ManageUsers = 0,
            ManageRadioStations = 1,
            ManagePresentationTemplates = 2,
            ManagePresentationThemes = 3
        }
    }
}
