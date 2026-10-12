using MeetingRoomBooking.Application.Common;
using MeetingRoomBooking.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MeetingRoomBooking.Application.Interfaces
{
    public interface IBookingService
    {
        Task<ServiceResult<BookingResponse>> CreateAsync(BookingResponse request);
        Task<ServiceResult<BookingResponse>> UpdateAsync(int id, BookingResponsePatch request);
        Task<ServiceResult<BookingResponse>> CancelAsync(int id);
    }
}
