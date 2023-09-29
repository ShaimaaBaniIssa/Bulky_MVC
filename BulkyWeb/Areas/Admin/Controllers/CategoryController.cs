using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        // ask dependency injection to provide an implementation of ICategoryRepository
        // register this service to dependency injection {program.cs}
        public CategoryController(IUnitOfWork unitOfWork)
        {

            _unitOfWork = unitOfWork;

        }
        public IActionResult Index()
        {

            List<Category> categoryList = _unitOfWork.Category.GetAll().ToList();
            // retrieve all rows in Category Table


            //pass this object to the view
            return View(categoryList);
            // looks into Category folder in Views to find index.html 
            // also look into shared folder
        }
        public IActionResult Create() { return View(); }
        [HttpPost]
        public IActionResult Create(Category category)
        {
            //Custom Validation
            //if (category.Name ==  category.DisplayOrder.ToString())
            //{
            //    ModelState.AddModelError("Name", "The Display order cannot exactly match the Name");

            //}
            //if (category.Name.ToLower() == "test")
            //{
            //    ModelState.AddModelError("", "Test is invalid value");
            //    // add into validation summary
            //}
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(category);
                _unitOfWork.Save();
                TempData["success"] = "Category created successfully";

                ////return RedirectToAction("Index","Category");
                return RedirectToAction("Index");
            }
            return View();


        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? category = _unitOfWork.Category.Get(c => c.Id == id);

            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                //based on the Id 
                _unitOfWork.Category.Update(category);
                _unitOfWork.Save();
                TempData["success"] = "Category updated successfully";

                ////return RedirectToAction("Index","Category");
                return RedirectToAction("Index");
            }
            return View();
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? category = _unitOfWork.Category.Get(c => c.Id == id);

            return View(category);
        }
        [HttpPost]
        public IActionResult Delete(Category category)
        {
            if (category == null) { return NotFound(); }
            _unitOfWork.Category.Remove(category);
            _unitOfWork.Save();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }


    }
}
