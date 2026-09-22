using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class Trip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TripId { get; set; }


        // ============================================================
        // TRANSPORT REQUEST
        // ============================================================

        [Required]
        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public virtual TransportRequest TransportRequest { get; set; }


        // ============================================================
        // TRIP STATUS
        // ============================================================

        public TripStatus Status { get; set; } = TripStatus.Not_Started;


        // ============================================================
        // CURRENT DRIVER LOCATION
        // ============================================================

        public double? CurrentLatitude { get; set; }

        public double? CurrentLongitude { get; set; }


        // ============================================================
        // TRIP TIMES
        // ============================================================

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }
    }


    public enum TripStatus
    {
        [Display(Name = "Not Started")]
        Not_Started,

        [Display(Name = "Started")]
        Started,

        [Display(Name = "Ended")]
        Ended
    }
}