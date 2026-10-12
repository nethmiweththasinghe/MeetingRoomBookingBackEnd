using MeetingRoomBooking.Domain.Entities;

namespace MeetingRoomBooking.Domain.Rules
{
    /// <summary>
    /// The booking rules, as plain methods with no database access,
    /// so they can be unit tested on their own.
    /// Each Check method returns an error message, or null if the rule passes.
    /// </summary>
    public static class BookingRules
    {
        public static readonly TimeOnly OpensAt = new(8, 0);
        public static readonly TimeOnly ClosesAt = new(18, 0);
        public const int MaxHours = 4;

        /// <summary>Rules 2, 3, 4 and 5: length, future, business hours on a single day.</summary>
        public static string? CheckTime(DateOnly date, TimeOnly start, int hours, DateTime now)
        {
            // Rule 2: the table stores start + hours, so "end after start" means at least 1 hour
            if (hours < 1)
                return "The meeting must end after it starts, so it needs to be at least 1 hour long.";

            // Rule 3
            if (hours > MaxHours)
                return $"A booking can't be longer than {MaxHours} hours.";

            // Rule 4
            if (date.ToDateTime(start) <= now)
                return "Bookings must be in the future.";

            // Rule 5: start at or after 8:00, finish at or before 18:00 (so always the same day)
            if (start < OpensAt || EndOf(start, hours) > ClosesAt.ToTimeSpan())
                return "Bookings must be between 8:00 and 18:00 on a single day.";

            return null;
        }

        /// <summary>Rules 1 and 6: the room exists, is active, and fits everyone.</summary>
        public static string? CheckRoom(Room? room, int attendees)
        {
            if (room is null)
                return "That room doesn't exist.";

            if ((bool)!room.IsActive)
                return $"{room.RoomName} isn't available for booking.";

            if (attendees < 1)
                return "A booking needs at least 1 attendee.";

            if (attendees > room.Capacity)
                return $"{room.RoomName} seats {room.Capacity}. Reduce the attendees or choose a bigger room.";

            return null;
        }

        /// <summary>
        /// Rule 7: do two time slots on the same day overlap?
        /// Back-to-back slots (one ends at 10:00, the next starts at 10:00) don't count.
        /// </summary>
        public static bool Overlaps(TimeOnly startA, int hoursA, TimeOnly startB, int hoursB) =>
            startA.ToTimeSpan() < EndOf(startB, hoursB) &&
            startB.ToTimeSpan() < EndOf(startA, hoursA);

        /// <summary>Rule 8: a booking that has already started can't be cancelled.</summary>
        public static bool HasStarted(Booking booking, DateTime now) =>
            booking.BookingDate.ToDateTime(booking.BookingTime) <= now;

        // Uses TimeSpan rather than TimeOnly.AddHours, which would wrap past midnight
        private static TimeSpan EndOf(TimeOnly start, int hours) =>
            start.ToTimeSpan() + TimeSpan.FromHours(hours);
    }
}