namespace Collector.Data.Models
{
    public class FilteredUserResult
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public Guid Id { get; set; }
        public int Status { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastLogin { get; set; }
        public string RoleName { get; set; }
        public int RoleId { get; set; }
        public bool IsAdmin { get; set; }
    }
}
