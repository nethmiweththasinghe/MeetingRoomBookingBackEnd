using MeetingRoomBooking.Application.Common;
using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Application.Interfaces;
using MeetingRoomBooking.Application.Mappings;
using MeetingRoomBooking.Domain.Entities;
using MeetingRoomBooking.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Application.Services
{
    public class BookingService(IMeetingRoomBookingDbContext db, TimeProvider timeProvider) : IBookingService
    {
        // "Now" in the server's local time zone, which is how bookings are stored.
        // TimeProvider (instead of DateTime.Now) lets tests pick a fixed "now".
        private DateTime Now => timeProvider.GetLocalNow().DateTime;

        public async Task<ServiceResult<BookingResponse>> CreateAsync(BookingResponse request)
        {
            var problem = await ValidateAsync(
                request.RoomId, request.BookingDate, request.BookingTime,
                request.MeetingHours, request.NoAttendees, ignoreBookingId: null);
            if (problem is not null)
            {
                return problem;
            }

            var booking = new Booking
            {
                RoomId = request.RoomId,
                StaffId = request.StaffId,
                BookingDate = request.BookingDate,
                BookingTime = request.BookingTime,
                MeetingHours = request.MeetingHours,
                Title = request.Title.Trim(),
                NoAttendees = request.NoAttendees,
                Status = BookingStatus.Confirmed,   // set by the server
                CreatedTime = Now                   // set by the server
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            return ServiceResult<BookingResponse>.Ok(booking.ToResponse());
        }

        public async Task<ServiceResult<BookingResponse>> UpdateAsync(int id, BookingResponsePatch request)
        {
            var booking = await db.Bookings.FindAsync(id);
            if (booking is null)
            {
                return ServiceResult<BookingResponse>.NotFound($"Booking {id} doesn't exist.");
            }

            // What the booking would look like after the change: sent values, or the current ones
            var roomId = request.RoomId ?? booking.RoomId;
            var date = request.BookingDate ?? booking.BookingDate;
            var start = request.BookingTime ?? booking.BookingTime;
            var hours = request.MeetingHours ?? booking.MeetingHours;
            var attendees = request.NoAttendees ?? booking.NoAttendees;

            // Same rules as creating; ignore this booking itself in the overlap check
            var problem = await ValidateAsync(roomId, date, start, hours, attendees, ignoreBookingId: id);
            if (problem is not null)
            {
                return problem;
            }

            booking.RoomId = roomId;
            booking.BookingDate = date;
            booking.BookingTime = start;
            booking.MeetingHours = hours;
            booking.NoAttendees = attendees;
            if (request.Title is not null)
            {
                booking.Title = request.Title.Trim();
            }

            await db.SaveChangesAsync();
            return ServiceResult<BookingResponse>.Ok(booking.ToResponse());
        }

        public async Task<ServiceResult<BookingResponse>> CancelAsync(int id)
        {
            var booking = await db.Bookings.FindAsync(id);
            if (booking is null)
            {
                return ServiceResult<BookingResponse>.NotFound($"Booking {id} doesn't exist.");
            }

            // Rule 8: cancelling twice is an error, not a silent success
            if (booking.Status == BookingStatus.Cancelled)
            {
                return ServiceResult<BookingResponse>.Conflict("This booking is already cancelled.");
            }

            // Rule 8: only future bookings
            if (BookingRules.HasStarted(booking, Now))
            {
                return ServiceResult<BookingResponse>.Invalid("Only future bookings can be cancelled.");
            }

            booking.Status = BookingStatus.Cancelled;
            await db.SaveChangesAsync();

            return ServiceResult<BookingResponse>.Ok(booking.ToResponse());
        }

        /// <summary>Runs rules 1–7. Returns null if everything passes.</summary>
        private async Task<ServiceResult<BookingResponse>?> ValidateAsync(
            int roomId, DateOnly date, TimeOnly start, int hours, int attendees, int? ignoreBookingId)
        {
            // Rules 2–5 (no database needed, so check first)
            var timeError = BookingRules.CheckTime(date, start, hours, Now);
            if (timeError is not null)
            {
                return ServiceResult<BookingResponse>.Invalid(timeError);
            }

            // Rules 1 and 6
            var room = await db.Rooms.FindAsync(roomId);
            var roomError = BookingRules.CheckRoom(room, attendees);
            if (roomError is not null)
            {
                return ServiceResult<BookingResponse>.Invalid(roomError);
            }

            // Rule 7: only confirmed bookings in the same room on the same day can clash
            var sameDay = await db.Bookings
                .Where(b => b.RoomId == roomId
                         && b.BookingDate == date
                         && b.Status == BookingStatus.Confirmed
                         && b.BookingId != ignoreBookingId)
                .ToListAsync();

            var clash = sameDay.FirstOrDefault(b => BookingRules.Overlaps(start, hours, b.BookingTime, b.MeetingHours));
            if (clash is not null)
            {
                var clashEnd = clash.BookingTime.AddHours(clash.MeetingHours);
                return ServiceResult<BookingResponse>.Conflict(
                    $"{room!.RoomName} is already booked from {clash.BookingTime:HH:mm} to {clashEnd:HH:mm}.");
            }

            return null;
        }
    }
}