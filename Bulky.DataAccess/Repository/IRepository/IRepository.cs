using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class //generic Interface
    {
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? properties= null);
        T Get(Expression<Func<T,bool>> filter , string? properties= null,bool tracked = false);
        void Add(T entity);
        //void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);



    }
}
