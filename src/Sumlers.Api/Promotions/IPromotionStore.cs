namespace Sumlers.Api.Promotions;

public interface IPromotionStore
{
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken);
    Task<Promotion> SaveAsync(Promotion promotion, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
