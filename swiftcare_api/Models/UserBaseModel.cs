using Microsoft.AspNetCore.Identity;

namespace swiftcare_api.Models
{
    public class UserBaseModel : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string? ProfilePicture { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public DateTime ModifiedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
    }
}
