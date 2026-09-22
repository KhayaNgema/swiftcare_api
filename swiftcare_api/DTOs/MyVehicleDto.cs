using swiftcare_api.Models;

namespace swiftcare_api.DTOs
{
    public class MyVehicleDto
    {
        public int VehicleId { get; set; }

        public string? VehicleName { get; set; }

        public string? RegNumber { get; set; }

        public string? Model { get; set; }

        public string? Year { get; set; }

        public VehicleStatus Status { get; set; }
    }
}