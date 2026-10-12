using MeetingRoomBooking.Application.Common;
using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Application.Interfaces;
using MeetingRoomBooking.Application.Mappings;
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
        private readonly IBookingService _bookingService;

        public BookingController(
            ILogger<BookingController> logger,
            MeetingRoomBookingDbContext dbContext,
            IBookingService bookingService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _bookingService = bookingService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookingDetails()
        {
            try
            {
                var bookings = await _dbContext.Bookings.ToListAsync();
                return Ok(bookings.Select(b => b.ToResponse()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching bookings");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching bookings");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookingResponse>> GetBookingDetailsById(int id)
        {
            try
            {
                var booking = await _dbContext.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }

                return Ok(booking.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching booking {BookingId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching booking");
            }
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponse>> AddBookingDetails([FromBody] BookingResponse request)
        {
            var result = await _bookingService.CreateAsync(request);

            return result.Status == ResultStatus.Ok
                ? CreatedAtAction(nameof(GetBookingDetailsById), new { id = result.Value!.BookingId }, result.Value)
                : ToProblem(result);
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<BookingResponse>> UpdateBookingData(int id, [FromBody] BookingResponsePatch request)
        {
            var result = await _bookingService.UpdateAsync(id, request);
            return result.Status == ResultStatus.Ok ? Ok(result.Value) : ToProblem(result);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<BookingResponse>> CancelBooking(int id)
        {
            var result = await _bookingService.CancelAsync(id);
            return result.Status == ResultStatus.Ok ? Ok(result.Value) : ToProblem(result);
        }

        // Hard delete: keep for admins only. Normal users should cancel instead.
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
            return NoContent();
        }

        /// <summary>Turns a failed ServiceResult into 400 / 404 / 409 with the message as the title.</summary>
        private ObjectResult ToProblem<T>(ServiceResult<T> result) => Problem(
            title: result.Error,
            statusCode: result.Status switch
            {
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            });
    }
}