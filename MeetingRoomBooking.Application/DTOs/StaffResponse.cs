using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Application.DTOs
{
    public class StaffResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
