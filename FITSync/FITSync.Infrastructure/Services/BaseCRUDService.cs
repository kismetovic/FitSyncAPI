using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public abstract class BaseCRUDService<TModel, TModelDTO, TInsert, TUpdate> : IBaseCRUDService<TModelDTO, TInsert, TUpdate>
        where TModel : class
        where TModelDTO : class
    {
        protected readonly IBaseRepository<TModel> _repository;
        protected readonly IMapper _mapper;

        public virtual Task BeforeInsert(TModel db, TInsert insert) => Task.CompletedTask;
        public virtual Task AfterInsert(TModel db, TInsert insert) => Task.CompletedTask;
        public virtual Task BeforeUpdate(TModel db, TUpdate update) => Task.CompletedTask;
        public virtual Task AfterUpdate(TModel db, TUpdate update) => Task.CompletedTask;
        public virtual Task BeforeDelete(TModel db) => Task.CompletedTask;

        protected BaseCRUDService(IBaseRepository<TModel> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<List<TModelDTO>> GetAsync()
        {
            var entities = await _repository.GetAsync();
            return _mapper.Map<List<TModelDTO>>(entities);
        }

        /// <summary>Paged read shared by every list endpoint.</summary>
        public virtual async Task<PagedResult<TModelDTO>> GetPagedAsync(PagedRequest paging, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _repository.GetPagedAsync(paging.Skip, paging.Take, cancellationToken);
            return PagedResult<TModelDTO>.Create(_mapper.Map<List<TModelDTO>>(items), paging.Page, paging.PageSize, total);
        }

        public virtual async Task<TModelDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<TModelDTO>(entity);
        }

        public virtual async Task<TModelDTO> InsertAsync(TInsert model)
        {
            var entity = _mapper.Map<TModel>(model);

            await BeforeInsert(entity, model);

            var insertedEntity = await _repository.InsertAsync(entity);

            await AfterInsert(insertedEntity, model);

            return _mapper.Map<TModelDTO>(insertedEntity);
        }

        /// <summary>
        /// Update no longer swallows exceptions and returns null. A missing row is a
        /// NotFoundException so the API can answer 404; anything else propagates and is
        /// handled by the global handler, instead of being reported as a generic failure.
        /// </summary>
        public virtual async Task<TModelDTO?> UpdateAsync(int id, TUpdate model)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return null;

            _mapper.Map(model, entity);

            await BeforeUpdate(entity, model);

            await _repository.UpdateAsync(entity);

            await AfterUpdate(entity, model);

            return _mapper.Map<TModelDTO>(entity);
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Entity with ID {id} was not found.");

            await BeforeDelete(entity);

            await _repository.DeleteAsync(entity);

            return true;
        }
    }
}
