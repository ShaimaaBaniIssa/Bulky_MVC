using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> products = _unitOfWork.Product.GetAll(properties: "Category").ToList();
            //Convert Each Category object to SelectListItem

            return View(products);
        }
        public IActionResult Upsert(int? id) // update and insert
        {

            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category
                .GetAll().Select(u => new SelectListItem  // Projection
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0) //create
            {
                return View(productVM);
            }
            else //update
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }

            //ViewBag.CategoryList = CategoryList;

        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVm, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    //image name         random name              the file extension
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    // the location 
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productVm.Product.ImageUrl))
                    {
                        // remove the old image
                        // trimStart ==> to remove \
                        var oldImage =
                            Path.Combine(wwwRootPath, productVm.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImage))
                        {
                            System.IO.File.Delete(oldImage);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {

                        file.CopyTo(fileStream); ///copy the file in the new location
                    }
                    productVm.Product.ImageUrl = @"\images\product\" + fileName;

                }
                if (productVm.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVm.Product);
                    TempData["success"] = "Product created successfully";

                }
                else
                {
                    _unitOfWork.Product.Update(productVm.Product);
                    TempData["success"] = "Product updated successfully";

                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {

                productVm.CategoryList = _unitOfWork.Category
                .GetAll().Select(u => new SelectListItem  // Projection
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVm);
            }
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> products = _unitOfWork.Product.GetAll(properties: "Category").ToList();
            return Json(new
            {
                data = products
            });

        }
        //[HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product product = _unitOfWork.Product.Get(c => c.Id == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            // delete image from images folder
            var oldImage =
                            Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImage))
            {
                System.IO.File.Delete(oldImage);
            }
            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }
        #endregion


    }
}
