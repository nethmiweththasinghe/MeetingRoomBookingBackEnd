using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MeetingRoomBooking.Domain.Entities
{
    [Table("BookingTbl", Schema = "dbo")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // FK -> RoomTbl.RoomId
        public int RoomId { get; set; }

        // FK -> StaffTbl.StaffId  (FK_BookingsTbl_StaffTbl)
        public int StaffId { get; set; }

        public DateTime BookingDate { get; set; }

        public DateTime BookingTime { get; set; }
        public int MeetingHours { get; set; }

        [StringLength(50)]
        public string? Title { get; set; }
        public int NoAttendees { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey(nameof(RoomId))]
        public Room Room { get; set; } = null!;

        [ForeignKey(nameof(StaffId))]
        public Staff Staff { get; set; } = null!;
    }
}
