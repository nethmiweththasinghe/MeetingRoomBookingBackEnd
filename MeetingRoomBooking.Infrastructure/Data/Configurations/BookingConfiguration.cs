using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomBooking.Infrastructure.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("BookingsTbl", "dbo");

            builder.HasKey(b => b.BookingId);

            builder.HasOne(b => b.Room)
                   .WithMany(r => r.Bookings)
                   .HasForeignKey(b => b.RoomId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Staff)
                   .WithMany(s => s.Bookings)
                   .HasForeignKey(b => b.StaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}