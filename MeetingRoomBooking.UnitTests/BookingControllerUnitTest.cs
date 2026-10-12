using MeetingRoomBooking.Api.Controllers;
using MeetingRoomBooking.Application.DTOs;
using MeetingRoomBooking.Application.Services;
using MeetingRoomBooking.Domain.Entities;
using MeetingRoomBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeetingRoomBooking.UnitTests;

/// <summary>
/// Controller tests: does each action return the right HTTP result?
/// The booking rules themselves are covered in BookingRulesTests and BookingServiceTests.
/// </summary>
public class BookingControllerUnitTest
{
    // "Now" is Monday 12 Oct 2026, 9:00, so the seeded bookings are in the future.
    private static readonly DateTime Now = new(2026, 10, 12, 9, 0, 0);

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static MeetingRoomBookingDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<MeetingRoomBookingDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static BookingController CreateController(MeetingRoomBookingDbContext db) =>
        new(NullLogger<BookingController>.Instance, db, new BookingService(db, new FixedTimeProvider(Now)));

    /// <summary>
    ///   Room 1: Kauri, seats 8      Booking 1: "Stand-up",    Kauri, 13 Oct 10:00, 1 h, Confirmed
    ///   Room 2: Rimu,  seats 4      Booking 2: "Code review", Kauri, 12 Oct 16:15, 1 h, Cancelled
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
                Status = BookingStatus.Confirmed,
                CreatedTime = new DateTime(2026, 10, 1, 9, 0, 0)
            },
            new Booking
            {
                BookingId = 2,
                RoomId = 1,
                StaffId = 1,
                BookingDate = new DateOnly(2026, 10, 12),
                BookingTime = new TimeOnly(16, 15),
                MeetingHours = 1,
                Title = "Code review",
                NoAttendees = 3,
                Status = BookingStatus.Cancelled,
                CreatedTime = new DateTime(2026, 10, 1, 9, 30, 0)
            });

        await db.SaveChangesAsync();
        return databaseName;
    }

    private static BookingResponse NewRequest(int startHour) => new()
    {
        RoomId = 1,
        StaffId = 1,
        BookingDate = new DateOnly(2026, 10, 13),
        BookingTime = new TimeOnly(startHour, 0),
        MeetingHours = 1,
        Title = "Design review",
        NoAttendees = 6
    };

    private static IActionResult Unwrap<T>(ActionResult<T> result) => result.Result ?? new OkObjectResult(result.Value);
    private static IActionResult Unwrap(IActionResult result) => result;

    private static int? StatusOf(IActionResult result) => Assert.IsAssignableFrom<ObjectResult>(result).StatusCode;

    // ---------- GET ----------

    [Fact]
    public async Task GetBookingDetails_ReturnsEveryBooking()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).GetBookingDetails());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<BookingResponse>>(ok.Value).Count());
    }

    [Fact]
    public async Task GetBookingDetails_NoBookings_ReturnsEmptyList()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());

        var result = Unwrap(await CreateController(db).GetBookingDetails());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<BookingResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetBookingDetailsById_ExistingId_ReturnsThatBooking()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).GetBookingDetailsById(1));

        var booking = Assert.IsType<BookingResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Stand-up", booking.Title);
    }

    [Fact]
    public async Task GetBookingDetailsById_UnknownId_ReturnsNotFound()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).GetBookingDetailsById(999));

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- POST ----------

    [Fact]
    public async Task AddBookingDetails_ValidBooking_Returns201WithTheNewId()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).AddBookingDetails(NewRequest(startHour: 12)));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var booking = Assert.IsType<BookingResponse>(created.Value);
        Assert.True(booking.BookingId > 0);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task AddBookingDetails_OverlapsConfirmedBooking_Returns409()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).AddBookingDetails(NewRequest(startHour: 10)));

        Assert.Equal(409, StatusOf(result));
    }

    [Fact]
    public async Task AddBookingDetails_BreaksARule_Returns400()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).AddBookingDetails(NewRequest(startHour: 7))); // before 8:00

        Assert.Equal(400, StatusOf(result));
    }

    // ---------- PATCH ----------

    [Fact]
    public async Task UpdateBookingData_UnknownBooking_Returns404()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).UpdateBookingData(999, new BookingResponsePatch { Title = "x" }));

        Assert.Equal(404, StatusOf(result));
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task CancelBooking_FutureBooking_Returns200()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).CancelBooking(1));

        var booking = Assert.IsType<BookingResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_Returns409()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).CancelBooking(2));

        Assert.Equal(409, StatusOf(result));
    }

    // ---------- DELETE ----------

    [Fact]
    public async Task DeleteBookings_ExistingId_RemovesItFromTheDatabase()
    {
        var databaseName = await SeedAsync();
        await using (var db = CreateContext(databaseName))
        {
            var result = Unwrap(await CreateController(db).DeleteBookings(1));

            Assert.IsType<NoContentResult>(result);
        }

        await using var check = CreateContext(databaseName);
        Assert.False(await check.Bookings.AnyAsync(b => b.BookingId == 1));
    }

    [Fact]
    public async Task DeleteBookings_UnknownId_ReturnsNotFound()
    {
        var databaseName = await SeedAsync();
        await using var db = CreateContext(databaseName);

        var result = Unwrap(await CreateController(db).DeleteBookings(999));

        Assert.IsType<NotFoundResult>(result);
    }
}