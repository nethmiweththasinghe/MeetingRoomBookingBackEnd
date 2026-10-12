using MeetingRoomBooking.Application.Interfaces;
using MeetingRoomBooking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRoomBooking.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IBookingService, BookingService>();

            return services;
        }
    }
}
