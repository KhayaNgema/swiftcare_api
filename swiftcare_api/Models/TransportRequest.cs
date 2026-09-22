using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class TransportRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }


        // ============================================================
        // REQUESTED BY - NURSE
        // ============================================================

        [Required]
        public string RequestedById { get; set; }

        [ForeignKey(nameof(RequestedById))]
        public virtual Nurse Nurse { get; set; }


        // ============================================================
        // REQUESTED FOR - RESIDENT
        // ============================================================

        [Required]
        public string RequestedForId { get; set; }

        [ForeignKey(nameof(RequestedForId))]
        public virtual Resident Resident { get; set; }


        // ============================================================
        // ASSIGNED DRIVER
        // ============================================================

        public string? AssignedToId { get; set; }

        [ForeignKey(nameof(AssignedToId))]
        public virtual Driver? Driver { get; set; }


        // ============================================================
        // ASSIGNED VEHICLE
        // ============================================================

        public int? VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }


        // ============================================================
        // REQUEST DETAILS
        // ============================================================

        [Required]
        public string RequestReason { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequestedForDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public DateTime RequestedForTime { get; set; }


        // ============================================================
        // PICKUP LOCATION
        // ============================================================

        [Required]
        public string PickUpAddress { get; set; }

        public double? PickUpLatitude { get; set; }

        public double? PickUpLongitude { get; set; }


        // ============================================================
        // DESTINATION LOCATION
        // ============================================================

        [Required]
        public string DestinationAddress { get; set; }

        public double? DestinationLatitude { get; set; }

        public double? DestinationLongitude { get; set; }


        // ============================================================
        // ROUTE INFORMATION
        // ============================================================

        public string? TravelDistance { get; set; }

        public string? TravelTime { get; set; }


        // ============================================================
        // REQUEST STATUS
        // ============================================================

        public RequestStatus Status { get; set; } = RequestStatus.Pending;


        // ============================================================
        // TRIP
        // ============================================================

        public virtual Trip? Trip { get; set; }
    }


    public enum RequestStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Driver Assigned")]
        Driver_Assigned,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Rejected")]
        Rejected,

        [Display(Name = "Driver Approved")]
        Driver_Approved,

        [Display(Name = "Driver Rejected")]
        Driver_Rejected
    }
}