using MeetingRoomBooking.Api.Controllers;
using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Domain.Entities;
using MeetingRoomBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeetingRoomBooking.UnitTests;

public class BookingControllerUnitTest
{
    // =====================================================================
    //  Setup helpers
    // =====================================================================

    /// <summary>Each test gets its own in-memory database (unique name).</summary>
    private static MeetingRoomBookingDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<MeetingRoomBookingDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static BookingController CreateController(MeetingRoomBookingDbContext db) =>
        new(NullLogger<BookingController>.Instance, db);

    /// <summary>
    /// Adds 2 rooms and 2 bookings, and returns the database name so a test
    /// can open a fresh context afterwards to check what was really saved.
    ///   Room 1: Kauri, seats 8      Booking 1: "Stand-up",    room 1, 5 people, 13 Oct 10:00
    ///   Room 2: Rimu,  seats 4      Booking 2: "Code review", room 1, 3 people, 12 Oct 16:15
    /// </summary>
    private static async Task<string> SeedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);

        db.Rooms.AddRange(
            new Room { RoomId = 1, RoomName = "Kauri", Capacity = 8, RoomLocation = "Level 2", IsActive = true },
            new Room { RoomId = 2, RoomName = "Rimu", Capacity = 4, RoomLocation = "Level 1", IsActive = true });

        db.Bookings.AddRange(
            new Booking
            {
                BookingId = 1,
                RoomId = 1,
                StaffId = 1,
                BookingDate = new DateOnly(2026, 10, 13),
                BookingTime = new TimeOnly(10, 0),
                MeetingHours = 1,
                Title = "Stand-up",
                NoAttendees = 5,
                Status = "Confirmed",
                CreatedTime = new DateTime(2026, 10, 1, 9, 0, 0)
            },
            new Booking
            {
                BookingId = 2,
                RoomId = 1,
                StaffId = 1,
                BookingDate = new DateOnly(2026, 10, 12),
                BookingTime = new TimeOnly(16, 15),
                MeetingHours = 2,
                Title = "Code review",
                NoAttendees = 3,
                Status = "Confirmed",
                CreatedTime = new DateTime(2026, 10, 1, 9, 30, 0)
            });

        await db.SaveChangesAsync();
        return databaseName;
    }

    /// <summary>A valid booking body for POST/PUT tests.</summary>
    private static BookingResponse NewBookingBody(int bookingId = 0) => new()
    {
        BookingId = bookingId,
        RoomId = 1,
        StaffId = 1,
        BookingDate = new DateOnly(2026, 10, 20),
        BookingTime = new TimeOnly(14, 0),
        MeetingHours = 2,
        Title = "Design review",
        NoAttendees = 6,
        Status = "Confirmed",
        CreatedTime = new DateTime(2026, 10, 11, 12, 0, 0)
    };

    // Lets tests read the result whether an action returns ActionResult<T> or plain ActionResult
    private static IActionResult Unwrap<T>(ActionResult<T> result) => result.Result ?? new OkObjectResult(result.Value);
    private static IActionResult Unwrap(IActionResult result) => result;

    // =====================================================================
    //  GET api/Booking
    // =====================================================================

    [Fact]
    public async Task GetBookingDetails_ReturnsEveryBooking()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.GetBookingDetails());

        var ok = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsAssignableFrom<IEnumerable<BookingResponse>>(ok.Value).ToList();
        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, b => b.BookingId == 1 && b.Title == "Stand-up");
        Assert.Contains(bookings, b => b.BookingId == 2 && b.Title == "Code review");
    }

    [Fact]
    public async Task GetBookingDetails_NoBookings_ReturnsEmptyList()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = Unwrap(await controller.GetBookingDetails());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<BookingResponse>>(ok.Value));
    }

    // =====================================================================
    //  GET api/Booking/{id}
    // =====================================================================

    [Fact]
    public async Task GetBookingDetailsById_ExistingId_ReturnsThatBooking()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.GetBookingDetailsById(2));

        var ok = Assert.IsType<OkObjectResult>(result);
        var booking = Assert.IsType<BookingResponse>(ok.Value);
        Assert.Equal(2, booking.BookingId);
        Assert.Equal("Code review", booking.Title);
        Assert.Equal(new TimeOnly(16, 15), booking.BookingTime);
    }

    [Fact]
    public async Task GetBookingDetailsById_UnknownId_ReturnsNotFound()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.GetBookingDetailsById(999));

        Assert.IsType<NotFoundResult>(result);
    }

    // =====================================================================
    //  POST api/Booking
    // =====================================================================

    [Fact]
    public async Task AddBookingDetails_NullBody_ReturnsBadRequest()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = Unwrap(await controller.AddBookingDetails(null!));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddBookingDetails_ValidBooking_SavesIt()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.AddBookingDetails(NewBookingBody()));

            Assert.IsType<OkObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        Assert.Equal(3, await check.Bookings.CountAsync());
        var saved = await check.Bookings.SingleAsync(b => b.Title == "Design review");
        Assert.Equal(new TimeOnly(14, 0), saved.BookingTime);
        Assert.Equal(6, saved.NoAttendees);
    }

    /// <summary>
    /// FAILS until fixed: the action returns the request body, so the client
    /// gets bookingId 0 instead of the ID the database just created.
    /// Fix: return Ok(ToResponse(booking)) instead of Ok(bookingResponse).
    /// </summary>
    [Fact]
    public async Task AddBookingDetails_ValidBooking_ReturnsTheNewBookingId()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.AddBookingDetails(NewBookingBody()));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BookingResponse>(ok.Value);
        Assert.True(response.BookingId > 0, "Response should contain the new booking's ID, not 0.");
    }

    // =====================================================================
    //  PUT api/Booking/{id}
    // =====================================================================

    [Fact]
    public async Task UpdateBookingDetails_UnknownId_ReturnsNotFound()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.UpdateBookingDetails(999, NewBookingBody(bookingId: 999)));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateBookingDetails_ExistingId_ReplacesTheFields()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.UpdateBookingDetails(1, NewBookingBody(bookingId: 1)));

            Assert.IsType<OkObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal("Design review", saved.Title);
        Assert.Equal(new TimeOnly(14, 0), saved.BookingTime);
        Assert.Equal(6, saved.NoAttendees);
    }

    /// <summary>
    /// FAILS until fixed: the action copies BookingId from the body onto the
    /// booking. If the body's bookingId isn't the same as the URL's id (e.g. the
    /// client leaves it as 0), EF refuses to change a primary key and you get a 500.
    /// Fix: delete the line  booking.BookingId = bookingResponse.BookingId;
    /// </summary>
    [Fact]
    public async Task UpdateBookingDetails_BodyWithoutBookingId_StillUpdates()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.UpdateBookingDetails(1, NewBookingBody(bookingId: 0)));

            Assert.IsType<OkObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal("Design review", saved.Title);
    }

    // =====================================================================
    //  PATCH api/Booking/{id}
    // =====================================================================

    [Fact]
    public async Task UpdateBookingData_OnlyStatusSent_ChangesOnlyStatus()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.UpdateBookingData(1, new BookingResponsePatch { Status = "Cancelled" }));

            Assert.IsType<OkObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal("Cancelled", saved.Status);
        Assert.Equal("Stand-up", saved.Title);                // untouched
        Assert.Equal(new TimeOnly(10, 0), saved.BookingTime); // untouched
        Assert.Equal(5, saved.NoAttendees);                   // untouched
    }

    [Fact]
    public async Task UpdateBookingData_SeveralFieldsSent_ChangesThoseFields()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);
            var request = new BookingResponsePatch
            {
                BookingTime = new TimeOnly(15, 0),
                MeetingHours = 2,
                Title = "  Retro  "
            };

            var result = Unwrap(await controller.UpdateBookingData(1, request));

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BookingResponse>(ok.Value);
            Assert.Equal("Retro", response.Title); // trimmed
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal(new TimeOnly(15, 0), saved.BookingTime);
        Assert.Equal(2, saved.MeetingHours);
        Assert.Equal("Retro", saved.Title);
        Assert.Equal("Confirmed", saved.Status); // untouched
    }

    [Fact]
    public async Task UpdateBookingData_MoreAttendeesThanCapacity_ReturnsBadRequestAndSavesNothing()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            // Kauri seats 8
            var result = Unwrap(await controller.UpdateBookingData(1, new BookingResponsePatch { NoAttendees = 10 }));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal(5, saved.NoAttendees);
    }

    [Fact]
    public async Task UpdateBookingData_MoveToRoomTooSmall_ReturnsBadRequestAndSavesNothing()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            // Booking 1 has 5 people; Rimu only seats 4
            var result = Unwrap(await controller.UpdateBookingData(1, new BookingResponsePatch { RoomId = 2 }));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        await using var check = CreateContext(databaseName);
        var saved = await check.Bookings.SingleAsync(b => b.BookingId == 1);
        Assert.Equal(1, saved.RoomId);
    }

    [Fact]
    public async Task UpdateBookingData_RoomDoesNotExist_ReturnsBadRequest()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.UpdateBookingData(1, new BookingResponsePatch { RoomId = 99 }));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBookingData_UnknownBooking_ReturnsNotFound()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);
        var controller = CreateController(db);

        var result = Unwrap(await controller.UpdateBookingData(999, new BookingResponsePatch { Status = "Cancelled" }));

        Assert.IsType<NotFoundResult>(result);
    }

    // =====================================================================
    //  DELETE api/Booking/{id}
    // =====================================================================

    [Fact]
    public async Task DeleteBookings_ExistingId_RemovesItFromTheDatabase()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.DeleteBookings(1));

            Assert.IsType<OkResult>(result);
        }

        // Fresh context: would fail if SaveChangesAsync() were missing
        await using var check = CreateContext(databaseName);
        Assert.False(await check.Bookings.AnyAsync(b => b.BookingId == 1));
        Assert.Equal(1, await check.Bookings.CountAsync());
    }

    [Fact]
    public async Task DeleteBookings_UnknownId_ReturnsNotFoundAndDeletesNothing()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var controller = CreateController(db);

            var result = Unwrap(await controller.DeleteBookings(999));

            Assert.IsType<NotFoundResult>(result);
        }

        await using var check = CreateContext(databaseName);
        Assert.Equal(2, await check.Bookings.CountAsync());
    }
}