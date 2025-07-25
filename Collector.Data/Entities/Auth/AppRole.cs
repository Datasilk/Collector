using System.ComponentModel.DataAnnotations.Schema;

namespace Collector.Data.Entities.Auth
{
    [Table("AppRoles")]
    public class AppRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
