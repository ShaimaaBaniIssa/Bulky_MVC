using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe;


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
            List<Bulky.Models.Product> products = _unitOfWork.Product.GetAll(properties: "Category").ToList();
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
                Product = new Bulky.Models.Product()
            };
            if (id == null || id == 0) // to create new product 
            {
                return View(productVM);
            }
            else //update ==> from edit button
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id,properties:"ProductImages");
                return View(productVM);
            }

            //ViewBag.CategoryList = CategoryList;

        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVm,List<IFormFile> files)

        {
            if (ModelState.IsValid)
            {
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


                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        //image name         random name              the file extension
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVm.Product.Id;
                        // the location 
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            // create folder for each product
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {

                            file.CopyTo(fileStream); ///copy the file in the new location
                        }
                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVm.Product.Id,
                           
                        };
                        if(productVm.Product.ProductImages == null) { 
                            //  create list
                            productVm.Product.ProductImages = new List<ProductImage>();
                        
                        }
                        productVm.Product.ProductImages.Add(productImage);
                        

                    }
                    _unitOfWork.Product.Update(productVm.Product);
                    _unitOfWork.Save();


                }
               
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

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u=>u.Id==imageId);
            int productID = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if(imageToBeDeleted.ImageUrl != null)
                {
                    var oldImage =
                            Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImage))
                    {
                        System.IO.File.Delete(oldImage);
                    }
                }
                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();
                TempData["success"] = "Deleted Successfully!";
            }
            return RedirectToAction(nameof(Upsert), new { id = productID });
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Bulky.Models.Product> products = _unitOfWork.Product.GetAll(properties: "Category").ToList();
            return Json(new
            {
                data = products
            });

        }
        //[HttpDelete]
        public IActionResult Delete(int? id)
        {
            Bulky.Models.Product product = _unitOfWork.Product.Get(c => c.Id == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            
            string productPath = @"images\products\product-" + id;
            
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (!Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);

                }
                Directory.Delete(finalPath);
            }
            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }
        #endregion


    }
}
