using Bookify.Application.Bookings.Commands.ReserveBooking;
using Bookify.Application.Bookings.Dtos;
using Bookify.Application.Bookings.Queries.GetBooking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.WebApi.Controllers.Bookings;

[Authorize]
[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    
    private readonly ISender _sender;

    public BookingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetBookingQuery(id);
        
        var result = await _sender.Send(query, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> ReserveBooking(
        ReserveBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ReserveBookingCommand(
            request.ApartmentId,
            request.UserId,
            request.StartDate,
            request.EndDate);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        
        return CreatedAtAction(nameof(GetBooking), new { id = result.Value}, result.Value);
    }
}