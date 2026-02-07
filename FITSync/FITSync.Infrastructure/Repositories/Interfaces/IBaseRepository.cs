using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IBaseRepository<TModel> where TModel : class
    {
        Task<List<TModel>> GetAsync();
        Task<TModel> GetByIdAsync(int id);
        Task<TModel> InsertAsync(TModel entity);
        Task<TModel> UpdateAsync(TModel entity);
        Task DeleteAsync(TModel entity);
    }
}
