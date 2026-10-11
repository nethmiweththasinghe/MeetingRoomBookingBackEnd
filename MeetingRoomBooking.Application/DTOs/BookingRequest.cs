using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Application.DTOs
{
    public class BookingRequest
    {
        public int RoomId { get; set; }
        public int StaffId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
