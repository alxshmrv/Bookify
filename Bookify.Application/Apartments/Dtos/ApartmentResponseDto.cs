namespace Bookify.Application.Apartments.Dtos;

public sealed class ApartmentResponseDto
{
    public Guid Id { get; init; }
    
    public string Name { get; init; }
    
    public string Description { get; init; }
    
    public decimal Price { get; init; }
    
    public string Currency { get; init; }
    
    public AddressResponseDto Address { get; set; }
}