using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) 
            : base(db) {
            _db = db;
        
        }
       
       

        public void Update(Product product)
        {
            var productFromDb = _db.Products.FirstOrDefault(u=>u.Id == product.Id);
            if(productFromDb != null)
            {
                productFromDb.ISBN = product.ISBN;
                productFromDb.ListPrice= product.ListPrice;
                productFromDb.Price = product.Price;
                productFromDb.Price100 = product.Price100;
                productFromDb.Price50 = product.Price50;
                productFromDb.Title = product.Title;
                productFromDb.Description = product.Description;
                productFromDb.CategoryId = product.CategoryId;
                productFromDb.Author = product.Author;
                productFromDb.ProductImages = product.ProductImages;
                //  Entity Framework will automatically do all configurations
                //          inside the productImages table

               
            }
        }
    }
}
