namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IBaseCRUDService<TModelDTO, TInsert, TUpdate> where TModelDTO : class
    {
        Task<List<TModelDTO>> GetAsync();
        Task<TModelDTO?> GetByIdAsync(int id);
        Task<TModelDTO> InsertAsync(TInsert request);
        Task<TModelDTO?> UpdateAsync(int id, TUpdate request);
        Task<bool> DeleteAsync(int id);
    }
}
