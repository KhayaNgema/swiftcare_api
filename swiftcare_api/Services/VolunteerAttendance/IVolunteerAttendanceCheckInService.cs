using System;
using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Services.VolunteerAttendance
{
    public interface IVolunteerAttendanceCheckInService
    {
        Task<FaceLoginAttendanceResult> CheckInAfterFaceLoginAsync(
            string volunteerId,
            string diditSessionId,
            double? latitude,
            double? longitude,
            double? accuracy,
            CancellationToken cancellationToken = default);

        Task<VolunteerAttendanceCheckoutResult> CheckOutAsync(
            string volunteerId,
            int attendanceId,
            CancellationToken cancellationToken = default);
    }

    public sealed class FaceLoginAttendanceResult
    {
        public bool CheckedIn { get; init; }
        public bool AlreadyCheckedIn { get; init; }
        public string Status { get; init; } = "NotCheckedIn";
        public string Message { get; init; } = string.Empty;
        public int? AttendanceId { get; init; }
        public int? VolunteerRosterShiftId { get; init; }
        public DateTime? CheckInTime { get; init; }
        public int LateMinutes { get; init; }
    }

    public sealed class VolunteerAttendanceCheckoutResult
    {
        public bool CheckedOut { get; init; }
        public bool AlreadyCheckedOut { get; init; }
        public string Status { get; init; } = "NotCheckedIn";
        public string Message { get; init; } = string.Empty;

        public int? AttendanceId { get; init; }
        public int? VolunteerRosterShiftId { get; init; }

        public DateTime? CheckInTime { get; init; }
        public DateTime? CheckOutTime { get; init; }

        public int LateMinutes { get; init; }
        public int EarlyDepartureMinutes { get; init; }
    }
}