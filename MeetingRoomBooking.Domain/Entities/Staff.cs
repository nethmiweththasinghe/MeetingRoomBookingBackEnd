using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MeetingRoomBooking.Domain.Entities
{
    [Table("StaffTbl", Schema = "dbo")]
    public class Staff
    {
        [Key]
        public int StaffId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
        
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
