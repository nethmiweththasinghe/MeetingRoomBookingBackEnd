namespace MeetingRoomBooking.Domain.Entities
{
    /// <summary>The values allowed in BookingsTbl.status. Use these instead of typing the strings.</summary>
    public static class BookingStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
    }
}