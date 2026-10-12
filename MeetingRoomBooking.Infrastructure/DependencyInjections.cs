using MeetingRoomBooking.Application.Interfaces;
using MeetingRoomBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRoomBooking.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MeetingRoomBookingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Application asks for the interface; give it the same DbContext
            services.AddScoped<IMeetingRoomBookingDbContext>(sp =>
                sp.GetRequiredService<MeetingRoomBookingDbContext>());

            return services;
        }
    }
}