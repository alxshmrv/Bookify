using Bookify.Application.Abstractions.Caching;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Bookings.Dtos;

namespace Bookify.Application.Bookings.Queries.GetBooking;

public sealed record GetBookingQuery(Guid BookingId) : ICachedQuery<BookingResponseDto>
{
    public string CacheKey  => $"{nameof(GetBookingQuery)}-{BookingId}";
    
    public TimeSpan? Expiration => null;
}