using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models
{
    public class Supervisor : UserBaseModel
    {
        [StringLength(150)]
        public string? JobTitle { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Required]
        public VolunteerArea Area { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? AssignedDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ICollection<Volunteer> Volunteers { get; set; }
            = new List<Volunteer>();

        public ICollection<VolunteerRoster> Rosters { get; set; }
            = new List<VolunteerRoster>();
    }
}