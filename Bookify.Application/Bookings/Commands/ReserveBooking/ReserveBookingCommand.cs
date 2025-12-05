using Bookify.Application.Abstractions.Messaging;

namespace Bookify.Application.Bookings.Commands.ReserveBooking;

public record ReserveBookingCommand(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate) : ICommand<Guid>;