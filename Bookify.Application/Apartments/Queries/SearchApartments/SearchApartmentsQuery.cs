using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Apartments.Dtos;

namespace Bookify.Application.Apartments.Queries.SearchApartments;

public sealed record SearchApartmentsQuery(
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<IReadOnlyList<ApartmentResponseDto>>;