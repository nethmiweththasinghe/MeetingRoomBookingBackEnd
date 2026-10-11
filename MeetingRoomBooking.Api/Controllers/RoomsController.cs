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
    public class RoomsController : ControllerBase
    {
        private readonly ILogger<RoomsController> _logger;
        private readonly MeetingRoomBookingDbContext _dbContext;

        public RoomsController(ILogger<RoomsController> logger, MeetingRoomBookingDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<RoomResponse>> GetAllRooms()
        {
            try
            {
                var roomResponse = await _dbContext.Rooms
                .Select(r => new RoomResponse { Id = r.RoomId, Name = r.RoomName, RoomLocation = r.RoomLocation, Capacity = r.Capacity })
                .ToListAsync();
                if (roomResponse == null || !roomResponse.Any())
                {
                    return NotFound("No rooms found.");
                }
                return Ok(roomResponse);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving rooms.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving rooms.");
            }
        }

        [HttpGet("{RoomId:int}")]
        public async Task<ActionResult<RoomResponse>> GetRoomById(int RoomId)
        {
            try
            {
                var room = await _dbContext.Rooms.FindAsync(RoomId);
                if (room == null)
                {
                    return NotFound();
                }

                var roomResponse = new RoomResponse
                {
                    Id = room.RoomId,
                    Name = room.RoomName,
                    Capacity = room.Capacity
                };

                return Ok(roomResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving room with ID {RoomId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the room.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateRoom([FromBody] RoomResponse roomRequest)
        {
            try
            {
                if (roomRequest == null)
                {
                    return BadRequest("Room data is required.");
                }
                if (string.IsNullOrEmpty(roomRequest.Name))
                {
                    return BadRequest("Room name is required.");
                }
                if (roomRequest.Capacity <= 0)
                {
                    return BadRequest("Room capacity must be greater than zero.");
                }

                var room = new Room
                {
                    RoomName = roomRequest.Name,
                    RoomLocation = roomRequest.RoomLocation,
                    Capacity = roomRequest.Capacity,
                    IsActive = roomRequest.IsActive
                };
                _dbContext.Rooms.Add(room);
                await _dbContext.SaveChangesAsync();

                return Ok(roomRequest);

                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a room.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the room.");
            }

        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateRoom(int id, [FromBody] RoomResponse roomRequest)
        {
            try
            {
                var room = await _dbContext.Rooms.FindAsync(id);
                if (room == null)
                {
                    return NotFound();
                }

                room.RoomName = roomRequest.Name;
                room.RoomLocation = roomRequest.RoomLocation;
                room.Capacity = roomRequest.Capacity;
                room.IsActive = roomRequest.IsActive;

                _dbContext.Rooms.Update(room);
                await _dbContext.SaveChangesAsync();

                return Ok(roomRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating room with ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the room.");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteRoom(int id)
        {
            var room = await _dbContext.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            _dbContext.Rooms.Remove(room);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
