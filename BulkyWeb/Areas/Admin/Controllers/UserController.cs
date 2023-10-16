using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        [BindProperty]
        public RoleManagementVM roleManagement { get; set; }

        public UserController(IUnitOfWork unitOfWork ,UserManager<IdentityUser> userManager ,
            RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            
        }
        public IActionResult Index()
        {
           
            return View();
        }
        public IActionResult RoleManagement(string userId)
        {

            roleManagement = new RoleManagementVM()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, properties: "Company"),
                RolesList = _roleManager.Roles.Select(i => new SelectListItem { Text = i.Name, Value = i.Name }),
                CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() })

            };



            roleManagement.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
                .GetAwaiter().GetResult().FirstOrDefault();

            return View(roleManagement);

        }
        [HttpPost]
        public IActionResult RoleManagement()
        {
            string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagement.ApplicationUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagement.ApplicationUser.Id);

            if (!(roleManagement.ApplicationUser.Role == oldRole))
            {
                //change the role
                if (roleManagement.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagement.ApplicationUser.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagement.ApplicationUser.Role).GetAwaiter().GetResult();

            }
            else
            {
                if (oldRole == SD.Role_Company && applicationUser.CompanyId != roleManagement.ApplicationUser.CompanyId)
                {
                    // updated the company 
                    applicationUser.CompanyId = roleManagement.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    _unitOfWork.Save();

                }



            }
            return RedirectToAction("Index");

        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> applicationUsers = _unitOfWork.ApplicationUser.GetAll(properties: "Company").ToList();
            
            
            foreach (var applicationUser in applicationUsers)
            {
                
                applicationUser.Role = _userManager.GetRolesAsync(applicationUser).GetAwaiter().GetResult().FirstOrDefault(); // get the role name
                if(applicationUser.CompanyId == null)
                {
                    applicationUser.Company = new()
                    {
                        Name = ""
                    };
                }
            }
            return Json(new
            {
                data = applicationUsers
            });

        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
           var userFromDb = _unitOfWork.ApplicationUser.Get(u=> u.Id == id);    
            if(userFromDb == null)
            {
                return Json(new { success = true, message = "Error while Locking/Unlocking" });

            }
            if(userFromDb.LockoutEnd!=null && userFromDb.LockoutEnd > DateTime.Now)
            {
                // user is currently locked and we need to unlock them
                userFromDb.LockoutEnd = DateTime.Now;
            }
            else
            { 
                //user is currently unlocked 
                // we need to lock them
                userFromDb.LockoutEnd= DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(userFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successful" });

        }
        
        
        
        #endregion


    }
}
