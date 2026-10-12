using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MeetingRoomBooking.Domain.Entities
{
    [Table("RoomTbl", Schema = "dbo")]
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoomName { get; set; } = string.Empty;        
        [StringLength(50)]
        public string? RoomLocation { get; set; }
        public int Capacity { get; set; }
        public bool? IsActive { get; set; } = true;

        // A room can have many bookings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
