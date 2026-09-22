using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class Vehicle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VehicleId { get; set; }

        public string VehicleName { get; set; } 

        public string RegNumber { get; set; }

        public string Model { get; set; }

        public string Year { get; set; }

        public VehicleStatus Status { get; set; }

    }

    public enum VehicleStatus
    {
        Available,
        Unavailable
    }
}
