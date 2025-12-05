using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Bookings.Dtos;

namespace Bookify.Application.Bookings.Queries.GetBooking;

public sealed record GetBookingQuery(Guid BookingId): IQuery<BookingResponseDto>;