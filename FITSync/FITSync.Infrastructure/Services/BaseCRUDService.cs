using AutoMapper;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Services
{
    public abstract class BaseCRUDService<TModel, TModelDTO, TInsert, TUpdate> : IBaseCRUDService<TModelDTO, TInsert, TUpdate>
        where TModel : class
        where TModelDTO : class
    {
        protected readonly IBaseRepository<TModel> _repository;
        protected readonly IMapper _mapper;

        public virtual async Task BeforeInsert(TModel db, TInsert insert) { }
        public virtual async Task BeforeUpdate(TModel db, TUpdate insert) { }
        public virtual async Task BeforeImageInsert(TModel db, TInsert insert) { }
        public virtual async Task BeforeDelete(TModel db) { }
        public virtual async Task BeforeImageUpdate(TModel db, TUpdate update) { }

        public BaseCRUDService(IBaseRepository<TModel> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<List<TModelDTO>> GetAsync()
        {
            var entities = await _repository.GetAsync();
            return _mapper.Map<List<TModelDTO>>(entities);
        }

        public virtual async Task<TModelDTO> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                return _mapper.Map<TModelDTO>(entity);
            }
            return null;
        }

        public virtual async Task<TModelDTO> InsertAsync(TInsert model)
        {
            var entity = _mapper.Map<TModel>(model);

            await BeforeImageInsert(entity, model);

            var insertedEntity = await _repository.InsertAsync(entity);

            await BeforeInsert(insertedEntity, model);

            return _mapper.Map<TModelDTO>(insertedEntity);
        }

        public virtual async Task<TModelDTO> UpdateAsync(int id, TUpdate model)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    throw new Exception($"Entity with ID:{id} not found");

                _mapper.Map(model, entity);

                await BeforeImageUpdate(entity, model);

                await _repository.UpdateAsync(entity);

                await BeforeUpdate(entity, model);

                return _mapper.Map<TModelDTO>(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new Exception($"Entity with ID:{id} not found");

            await BeforeDelete(entity);

            await _repository.DeleteAsync(entity);

            return true;
        }
    }
}
