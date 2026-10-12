using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Application.Interfaces
{
    /// <summary>
    /// What the Application layer needs from the database.
    /// MeetingRoomBookingDbContext (Infrastructure) implements this, so Application
    /// never has to reference Infrastructure.
    /// </summary>
    public interface IMeetingRoomBookingDbContext
    {
        DbSet<Room> Rooms { get; }
        DbSet<Booking> Bookings { get; }
        DbSet<Staff> Staff { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}