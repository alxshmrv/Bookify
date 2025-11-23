using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Bookings.Events;

public record BookingCanceledDomainEvent(Guid BookingId) : IDomainEvent;
