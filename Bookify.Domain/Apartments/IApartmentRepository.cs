namespace Bookify.Domain.Apartments;

public interface IApartmentRepository
{
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    void AddApartment(Apartment apartment, CancellationToken cancellationToken = default);
}