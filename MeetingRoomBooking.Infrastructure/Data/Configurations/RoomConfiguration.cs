using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Infrastructure.Data.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.HasKey(r => r.RoomId);
            builder.Property(r => r.RoomName).IsRequired().HasMaxLength(50);
            builder.Property(r => r.RoomLocation).IsRequired().HasMaxLength(50);
            builder.Property(r => r.Capacity).IsRequired();
            builder.Property(r => r.IsActive).IsRequired();
        }
    }
}
