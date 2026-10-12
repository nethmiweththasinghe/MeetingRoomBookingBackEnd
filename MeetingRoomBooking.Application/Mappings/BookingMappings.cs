using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Domain.Entities;

namespace MeetingRoomBooking.Application.Mappings
{
    /// <summary>One place to turn a Booking entity into a BookingResponse. Use as booking.ToResponse().</summary>
    public static class BookingMappings
    {
        public static BookingResponse ToResponse(this Booking booking) => new()
        {
            BookingId = booking.BookingId,
            RoomId = booking.RoomId,
            StaffId = booking.StaffId,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            MeetingHours = booking.MeetingHours,
            Title = booking.Title,
            NoAttendees = booking.NoAttendees,
            Status = booking.Status,
            CreatedTime = booking.CreatedTime
        };
    }
}