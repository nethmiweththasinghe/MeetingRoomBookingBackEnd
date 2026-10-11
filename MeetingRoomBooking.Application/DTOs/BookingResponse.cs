using MeetingRoomBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MeetingRoomBooking.Application.DTOs
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public int RoomId { get; set; }
        public int StaffId { get; set; }
        public DateOnly BookingDate { get; set; }
        public TimeOnly BookingTime { get; set; }
        public int MeetingHours { get; set; }
        public string? Title { get; set; }
        public int NoAttendees { get; set; }
        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
