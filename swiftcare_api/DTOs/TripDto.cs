using System;
using swiftcare_api.Models;

namespace swiftcare_api.DTOs
{
    public class TripDto
    {
        // ============================================================
        // TRIP
        // ============================================================

        public int TripId { get; set; }

        public Trip Trip { get; set; }

        // TransportRequest status
        public RequestStatus RequestStatus { get; set; }

        // Trip status
        public TripStatus Status { get; set; }
        // ============================================================
        // TRANSPORT REQUEST
        // ============================================================

        public int RequestId { get; set; }



        // ============================================================
        // RESIDENT
        // ============================================================

        public string? ResidentId { get; set; }

        public string? ResidentFirstName { get; set; }

        public string? ResidentLastName { get; set; }

        public string? ResidentName
        {
            get
            {
                return $"{ResidentFirstName} {ResidentLastName}".Trim();
            }
        }

        public string? ResidentProfilePicture { get; set; }

        public Room? RoomNumber { get; set; }


        // ============================================================
        // PICKUP LOCATION
        // ============================================================

        public string? PickupAddress { get; set; }

        public double? PickupLatitude { get; set; }

        public double? PickupLongitude { get; set; }


        // ============================================================
        // DESTINATION LOCATION
        // ============================================================

        public string? DestinationAddress { get; set; }

        public double? DestinationLatitude { get; set; }

        public double? DestinationLongitude { get; set; }


        // ============================================================
        // REQUEST INFORMATION
        // ============================================================

        public string? RequestReason { get; set; }

        public DateTime? RequestedForDate { get; set; }

        public DateTime? RequestedForTime { get; set; }

        public DateTime? RequestedAt { get; set; }


        // ============================================================
        // TRIP INFORMATION
        // ============================================================

        public string? TravelDistance { get; set; }

        public string? TravelTime { get; set; }
        public double? CurrentLatitude { get; set; }

        public double? CurrentLongitude { get; set; }

        // ============================================================
        // TRIP TIMESTAMPS
        // ============================================================

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }
    }
}