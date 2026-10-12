using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Application.DTOs
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? RoomLocation { get; set; }
        public int Capacity { get; set; }
        public bool? IsActive { get; set; }
    }
}
