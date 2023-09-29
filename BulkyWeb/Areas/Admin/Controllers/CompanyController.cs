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

    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
        }
        public IActionResult Index()
        {
            List<Company> Companys = _unitOfWork.Company.GetAll().ToList();
            //Convert Each Category object to SelectListItem

            return View(Companys);
        }
        public IActionResult Upsert(int? id) // update and insert
        {

           
            if (id == null || id == 0) //create
            {
                return View(new Company());
            }
            else //update
            {
                Company company = _unitOfWork.Company.Get(u => u.Id == id);
                return View(company);
            }

            //ViewBag.CategoryList = CategoryList;

        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                
                if (company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                    TempData["success"] = "Company created successfully";

                }
                else
                {
                    _unitOfWork.Company.Update(company);
                    TempData["success"] = "Company updated successfully";

                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {

                return View(company);
            }
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> Companys = _unitOfWork.Company.GetAll().ToList();
            return Json(new
            {
                data = Companys
            });

        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company company = _unitOfWork.Company.Get(c => c.Id == id);
            if (company == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Company.Remove(company);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }
        #endregion


    }
}
