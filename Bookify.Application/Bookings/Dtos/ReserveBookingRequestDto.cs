namespace Bookify.Application.Bookings.Dtos;

public sealed record ReserveBookingRequestDto(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate);