using Azure.Core;
using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Domain.Entities;
using MeetingRoomBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ILogger<BookingController> _logger;
        private readonly MeetingRoomBookingDbContext _dbContext;

        public BookingController(ILogger<BookingController> logger, MeetingRoomBookingDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookingDetails()
        {
            try
            {
                var bookingResponse = await _dbContext.Bookings.Select(b => new BookingResponse
                {
                    BookingId = b.BookingId,
                    RoomId = b.RoomId,
                    StaffId = b.StaffId,
                    BookingDate = b.BookingDate,
                    BookingTime = b.BookingTime,
                    MeetingHours = b.MeetingHours,
                    Title = b.Title,
                    NoAttendees = b.NoAttendees,
                    Status = b.Status,
                    CreatedTime = b.CreatedTime
                }).ToListAsync();

                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while fetching bookings");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occured while fetching bookings");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookingResponse>> GetBookingDetailsById(int id)
        {
            try
            {
                var b = await _dbContext.Bookings.FindAsync(id);
                if (b == null)
                {
                    return NotFound();
                }

                var bookingResponse = new BookingResponse
                {
                    BookingId = b.BookingId,
                    RoomId = b.RoomId,
                    StaffId = b.StaffId,
                    BookingDate = b.BookingDate,
                    BookingTime = b.BookingTime,
                    MeetingHours = b.MeetingHours,
                    Title = b.Title,
                    NoAttendees = b.NoAttendees,
                    Status = b.Status,
                    CreatedTime = b.CreatedTime
                };

                return Ok(bookingResponse);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured while fetching booking");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occured while fetching booking");
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddBookingDetails([FromBody]BookingResponse bookingResponse)
        {
            try
            {
                if (bookingResponse == null)
                {
                    return BadRequest("Booking data is required");
                }
                var booking = new Booking
                {
                    BookingId = bookingResponse.BookingId,
                    RoomId = bookingResponse.RoomId,
                    StaffId = bookingResponse.StaffId,
                    BookingDate = bookingResponse.BookingDate,
                    BookingTime = bookingResponse.BookingTime,
                    MeetingHours = bookingResponse.MeetingHours,
                    Title = bookingResponse.Title,
                    NoAttendees = bookingResponse.NoAttendees,
                    Status = bookingResponse.Status,
                    CreatedTime = bookingResponse.CreatedTime
                };
                _dbContext.Bookings.Add(booking);
                await _dbContext.SaveChangesAsync();
                return Ok(bookingResponse);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured while adding bookings");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occured while adding bookings");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateBookingDetails(int id, [FromBody] BookingResponse bookingResponse)
        {
            try
            {
                var booking = await _dbContext.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }

                booking.BookingId = bookingResponse.BookingId;
                booking.RoomId = bookingResponse.RoomId;
                booking.StaffId = bookingResponse.StaffId;
                booking.BookingDate = bookingResponse.BookingDate;
                booking.BookingTime = bookingResponse.BookingTime;
                booking.MeetingHours = bookingResponse.MeetingHours;
                booking.Title = bookingResponse.Title;
                booking.NoAttendees = bookingResponse.NoAttendees;
                booking.Status = bookingResponse.Status;
                booking.CreatedTime = bookingResponse.CreatedTime;
                
                _dbContext.Bookings.Update(booking);
                await _dbContext.SaveChangesAsync();
                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while updating bookings");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occured while updating bookings");
            }
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<BookingResponsePatch>> UpdateBookingData(int id, [FromBody] BookingResponsePatch request)
        {
            try
            {
                var booking = await _dbContext.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }

                if (request.RoomId != null) booking.RoomId = request.RoomId.Value;
                if (request.BookingDate != null) booking.BookingDate = request.BookingDate.Value;
                if (request.BookingTime != null) booking.BookingTime = request.BookingTime.Value;
                if (request.MeetingHours != null) booking.MeetingHours = request.MeetingHours.Value;
                if (request.Title != null) booking.Title = request.Title.Trim();
                if (request.NoAttendees != null) booking.NoAttendees = request.NoAttendees.Value;
                if (request.Status != null) booking.Status = request.Status;

                var room = await _dbContext.Rooms.FindAsync(booking.RoomId);
                if (room == null)
                {
                    return BadRequest("That room doesn't exist");
                }
                if (booking.NoAttendees > room.Capacity)
                {
                    return BadRequest($"{room.RoomName} seats {room.Capacity}. Reduce the attendees or choose a bigger room.");
                }

                await _dbContext.SaveChangesAsync();

                return Ok(ToResponse(booking));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occured while updating booking data");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error occured while updating booking data");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteBookings(int id)
        {
            var booking = await _dbContext.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            _dbContext.Bookings.Remove(booking);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        private static BookingResponse ToResponse(Booking booking) => new()
        {
            BookingId = booking.BookingId,
            RoomId = booking.RoomId,
            StaffId = booking.StaffId,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            MeetingHours = booking.MeetingHours,
            Title = booking.Title,
            NoAttendees = booking.NoAttendees,
            Status = booking.Status,
            CreatedTime = booking.CreatedTime
        };
    }
}
