using swiftcare_api.Models;
using System;

namespace swiftcare_api.DTOs
{
    public class TransportRequestDto
    {
        public int RequestId { get; set; }

        public string RequestedForId { get; set; }

        public string RequestReason { get; set; }

        public DateTime RequestedForDate { get; set; }

        public DateTime RequestedForTime { get; set; }

        public string PickUpAddress { get; set; }

        public string DestinationAddress { get; set; }

        public double? PickUpLatitude { get; set; }

        public double? PickUpLongitude { get; set; }

        public double? DestinationLatitude { get; set; }

        public double? DestinationLongitude { get; set; }

        public DateTime RequestedAt { get; set; }

        public string? TravelDistance { get; set; }

        public string? TravelTime { get; set; }

        public string? AssignedToId { get; set; }

        public int? VehicleId { get; set; }

        // ============================================================
        // TRANSPORT REQUEST STATUS
        // ============================================================

        public RequestStatus RequestStatus { get; set; }

        // ============================================================
        // TRIP DETAILS
        // ============================================================

        public int? TripId { get; set; }

        public TripStatus? TripStatus { get; set; }

        // ============================================================
        // RESIDENT DETAILS
        // ============================================================

        public string ResidentFirstName { get; set; }

        public string ResidentLastName { get; set; }

        public string? ResidentProfilePicture { get; set; }

        public Room RoomNumber { get; set; }
    }
}