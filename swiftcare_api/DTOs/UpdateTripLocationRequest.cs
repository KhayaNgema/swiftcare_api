namespace swiftcare_api.DTOs
{
    public class UpdateTripLocationRequest
    {
        public int TripId { get; set; }

        public double CurrentLatitude { get; set; }

        public double CurrentLongitude { get; set; }
    }
}
