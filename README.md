# \# Meeting Room Booking

# 

# A system for booking meeting rooms. Staff pick a room, choose a date, start time and length, and the booking is saved against the room and the person who made it.

# 

# \- \*\*Backend:\*\* ASP.NET Core Web API with Entity Framework Core and SQL Server

# \- \*\*Frontend:\*\* Angular

# \- \*\*Tests:\*\* xUnit with the EF Core in-memory database

# 

# \---

# 

# \## Solution structure

# 

# ```

# MeetingRoomBooking.Api             Controllers, Program.cs, appsettings.json

# MeetingRoomBooking.Application     DTOs (request and response classes)

# MeetingRoomBooking.Domain          Entities (Room, Staff, Booking)

# MeetingRoomBooking.Infrastructure  MeetingRoomBookingDbContext and entity configurations

# MeetingRoomBooking.UnitTests       xUnit tests for the controllers

# ```

# 

# The Angular app is a separate project. Its booking page combines two components:

# `Room` (pick a room) and `BookingDetails` (meeting details), and loads rooms through `RoomService`.

# 

# \---

# 

# \## Database

# 

# Database name: \*\*BookingDB\*\* (SQL Server)

# 

# | Table | Key columns | Notes |

# |---|---|---|

# | `RoomTbl` | `roomId` (PK, identity) | `roomName`, `capacity`, `roomLocation`, `isActive` (bit, default 1) |

# | `StaffTbl` | `staffId` (PK, identity) | Staff details |

# | `BookingsTbl` | `bookingId` (PK, identity) | `roomId` → `RoomTbl`, `staffId` → `StaffTbl`, `bookingDate` (date), `bookingTime` (time), `meetingHours` (int), `title`, `noAttendees` (int), `status`, `createdTime` (datetime) |

# 

# \*\*Booking status values:\*\* `Pending`, `Confirmed`, `Cancelled`

# 

# \### Column types and C# types

# 

# Entity and DTO property types must match the SQL column types, or EF fails when reading data.

# 

# | SQL type | C# type |

# |---|---|

# | `date` | `DateOnly` |

# | `time` | `TimeOnly` |

# | `datetime` / `datetime2` | `DateTime` |

# | nullable column | nullable property (`string?`, `int?`, …) |

# 

# \### One-time database fixes

# 

# If the foreign key from bookings to rooms shows as `FK\_BookingsTbl\_BookingsTbl`, it points at the wrong table. Replace it:

# 

# ```sql

# ALTER TABLE dbo.BookingsTbl DROP CONSTRAINT FK\_BookingsTbl\_BookingsTbl;

# ALTER TABLE dbo.BookingsTbl

# &#x20;   ADD CONSTRAINT FK\_BookingsTbl\_RoomTbl FOREIGN KEY (roomId) REFERENCES dbo.RoomTbl (roomId);

# ```

# 

# Check that each ID column is an identity (auto-increment) column. This should return three rows:

# 

# ```sql

# SELECT OBJECT\_NAME(object\_id) AS TableName, name AS IdentityColumn

# FROM sys.identity\_columns

# WHERE OBJECT\_NAME(object\_id) IN ('RoomTbl', 'StaffTbl', 'BookingsTbl');

# ```

# 

# \---

# 

# \## Running the API

# 

# \### Prerequisites

# 

# \- The .NET SDK that matches the projects' target framework

# \- SQL Server (Express is fine) with the `BookingDB` database

# \- Visual Studio, or the `dotnet` CLI

# 

# \### 1. Set the connection string

# 

# In `MeetingRoomBooking.Api/appsettings.json`:

# 

# ```json

# "ConnectionStrings": {

# &#x20; "DefaultConnection": "Server=YOUR\_SERVER\\\\SQLEXPRESS;Database=BookingDB;Trusted\_Connection=True;TrustServerCertificate=True;"

# }

# ```

# 

# \- `Server=` must be exactly what you type in SSMS's Connect window.

# \- Backslashes must be doubled in JSON (`\\\\`).

# 

# \### 2. Run it

# 

# ```

# dotnet run --project MeetingRoomBooking.Api

# ```

# 

# The API runs at `https://localhost:7006`. Interactive API docs (Scalar) are at `https://localhost:7006/scalar`.

# 

# \### 3. Allow the Angular app to call the API (CORS)

# 

# `Program.cs` must allow the Angular dev server's address:

# 

# ```csharp

# builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>

# &#x20;   p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

# 

# // after var app = builder.Build();  and before app.MapControllers();

# app.UseCors("Frontend");

# ```

# 

# \---

# 

# \## API endpoints

# 

# \### Rooms: `/api/Rooms`

# 

# | Method | Route | What it does |

# |---|---|---|

# | GET | `/api/Rooms` | List all rooms |

# | GET | `/api/Rooms/{id}` | Get one room |

# | POST | `/api/Rooms` | Create a room (returns 201 with the new room) |

# | PUT | `/api/Rooms/{id}` | Replace all of a room's details |

# | PATCH | `/api/Rooms/{id}` | Change only the fields sent, e.g. `{ "isActive": false }` |

# | DELETE | `/api/Rooms/{id}` | Delete a room |

# 

# \### Bookings: `/api/Booking`

# 

# | Method | Route | What it does |

# |---|---|---|

# | GET | `/api/Booking` | List all bookings |

# | GET | `/api/Booking/{id}` | Get one booking |

# | POST | `/api/Booking` | Create a booking |

# | PUT | `/api/Booking/{id}` | Replace all of a booking's details |

# | PATCH | `/api/Booking/{id}` | Change only the fields sent, e.g. `{ "status": "Cancelled" }` |

# | DELETE | `/api/Booking/{id}` | Delete a booking |

# 

# PATCH on a booking checks that the room exists and that `noAttendees` isn't more than the room's capacity. If either check fails, it returns \*\*400\*\* and saves nothing.

# 

# \### Response codes

# 

# | Code | Meaning |

# |---|---|

# | 200 / 201 / 204 | Success |

# | 400 | The request is invalid (missing fields, too many attendees, unknown room) |

# | 404 | No room or booking has that ID |

# | 500 | Unexpected error, logged on the server |

# 

# \### Example: create a booking

# 

# ```http

# POST /api/Booking

# Content-Type: application/json

# 

# {

# &#x20; "roomId": 1,

# &#x20; "staffId": 1,

# &#x20; "bookingDate": "2026-10-20",

# &#x20; "bookingTime": "14:00:00",

# &#x20; "meetingHours": 2,

# &#x20; "title": "Design review",

# &#x20; "noAttendees": 6,

# &#x20; "status": "Confirmed",

# &#x20; "createdTime": "2026-10-11T12:00:00"

# }

# ```

# 

# Dates are sent as `yyyy-MM-dd` and times as `HH:mm:ss`.

# 

# \---

# 

# \## Running the frontend

# 

# From the Angular project folder:

# 

# ```

# npm install

# ng serve

# ```

# 

# Open `http://localhost:4200`. The API must be running, and the API URL in `RoomService` must match it (`https://localhost:7006/api`).

# 

# The room service converts room IDs from the API (numbers) to strings for the UI. Convert them back with `Number(room.id)` when sending a booking.

# 

# \---

# 

# \## Running the tests

# 

# ```

# dotnet test

# ```

# 

# or use \*\*Test Explorer\*\* in Visual Studio.

# 

# `BookingControllerUnitTest` has 18 tests covering every booking endpoint, including not-found cases, partial updates with PATCH, capacity checks, and checks that rejected changes save nothing.

# 

# How the tests work:

# 

# \- Each test gets its own in-memory database, filled with 2 rooms and 2 bookings, so tests never affect each other.

# \- Tests that change data read it back through a fresh `DbContext` to prove it was really saved.

# \- The in-memory database \*\*doesn't\*\* enforce foreign keys, and these tests call the controller methods directly, so DTO validation attributes (`\[Range]`, `\[Required]`…) don't run. Testing those needs integration tests with `WebApplicationFactory`.

# 

# \---

# 

# \## Troubleshooting

# 

# | Error | Cause | Fix |

# |---|---|---|

# | `Keyword not supported: '...;database'` | The connection string is missing `Server=` | Start it with `Server=YOUR\_SERVER\\\\...` |

# | `Invalid object name 'Rooms'` | EF uses the DbSet name instead of the real table name | Map each entity with `ToTable("RoomTbl")` etc. in its configuration class |

# | `Invalid column name '...'` | An entity property has no matching column | Rename the property, map it with `HasColumnName`, or add the column |

# | `Unable to cast 'TimeSpan' to 'DateTime'` | A `time` column is mapped to `DateTime` | Use `TimeOnly` |

# | `Data is Null. This method or property cannot be called on Null values` | A NULL column is mapped to a non-nullable property | Make the property nullable, or make the column NOT NULL with a default |

# | `A possible object cycle was detected` | An entity was returned instead of a DTO | Always return a `...Response` DTO, never an entity |

# | `No route matches the supplied values` (after POST) | The route parameter name differs from the value given to `CreatedAtAction` | Use the same name in both, e.g. `{id:int}` with `new { id = ... }` |

# | `Cannot insert explicit value for identity column` | An ID was included in an INSERT | Leave the ID out; the database creates it |

# | `blocked by CORS policy` (browser) | The API doesn't allow requests from the Angular app | Add `AddCors` and `UseCors("Frontend")` |

# | `net::ERR\_CERT\_AUTHORITY\_INVALID` (browser) | The HTTPS dev certificate isn't trusted | Run `dotnet dev-certs https --trust` |

# | PATCH returns 400 "The Name field is required" | The PATCH body class has non-nullable fields | Use a PATCH DTO where every property is nullable |

# 

# \---

# 

# \## Still to do

# 

# \- \[ ] Staff endpoints (`/api/staff`) so the frontend can list who is booking

# \- \[ ] POST booking: check that the room exists, the room has enough seats, and the time doesn't overlap another booking in the same room

# \- \[ ] Run the same double-booking check in PATCH and PUT when the room, date, time or length changes

# \- \[ ] Set `status` and `createdTime` on the server instead of taking them from the request

# \- \[ ] Connect the Angular booking form to `POST /api/Booking`

# \- \[ ] Tests for `RoomsController`

# \- \[ ] Global exception handler, so each action doesn't need its own try/catch

# \- \[ ] Sign-in for staff

