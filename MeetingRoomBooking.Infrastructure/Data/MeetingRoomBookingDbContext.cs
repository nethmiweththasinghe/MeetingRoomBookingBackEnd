using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Infrastructure.Data
{
    public class MeetingRoomBookingDbContext(DbContextOptions<MeetingRoomBookingDbContext> options) : DbContext(options)
    {
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Staff> Staff => Set<Staff>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeetingRoomBookingDbContext).Assembly);
    }
}
