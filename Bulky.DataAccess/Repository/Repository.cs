using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> Set;
        public Repository(ApplicationDbContext db)
        {
            _db = db;
            this.Set = _db.Set<T>();
            //this.Set = _db.Categories;
            //_db.Products.Include(u => u.Category); based on foreign key relation
        }
        public void Add(T entity)
        {
            this.Set.Add(entity);
        }

        public T Get(Expression<Func<T, bool>> filter, string? properties = null, bool tracked = false)
        {
            IQueryable<T> query;
            if (tracked)
            {
                query = this.Set;
            }
            else
            {
                query = this.Set.AsNoTracking();
            }
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(properties))
            {
                foreach (var prop in properties.
                    Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(prop);
                }
            }

            
            return query.FirstOrDefault();
            //Category? category = _db.Categories.Where(u=>u.Id==id).FirstOrDefault();

        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter, string? properties = null)
        {
            IQueryable<T> query = this.Set;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(properties))
            {
                foreach (var prop in properties.
                    Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries))
                {
                    query= query.Include(prop);
                }
            }

            return query.ToList();
        }

        public void Remove(T entity)
        {
            this.Set.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            this.Set.RemoveRange(entity);

        }
    }
}
