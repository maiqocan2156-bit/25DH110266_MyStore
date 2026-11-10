
using System.ComponentModel.DataAnnotations;

namespace _25DH110266_MyStore.Models
{
    public class UserMetadata
    {
        [StringLength(20)]
        public string UserRole { get; set; }
    }

    [MetadataType(typeof(UserMetadata))]
    public partial class User
    {
    }
}
