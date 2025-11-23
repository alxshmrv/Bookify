using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Bookings.Validation;

public static class BookingErrors
{
    public static readonly Error NotFound = new(
        "Booking.Found",
        "The booking was not found.");
    
    public static readonly Error Overlap = new(
        "Booking.Overlap",
        "The current booking has overlapping bookings.");
    
    public static readonly Error NotReserved = new(
        "Booking.NotReserved",
        "The current booking is not pending.");
    
    public static readonly Error NotConfirmed = new(
        "Booking.NotConfirmed",
        "The booking is not confirmed.");
    
    public static readonly Error AlreadyStarted = new(
        "Booking.AlreadyStarted",
        "The booking has already been started.");
}