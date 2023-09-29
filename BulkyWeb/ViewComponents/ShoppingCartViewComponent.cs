using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
        // create Components folder in shared views
        // create ShoppingCart folder inside it
        // create default html page
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(claim != null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
                {
                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == claim.Value).Count());

                }
               
                return View(HttpContext.Session.GetInt32(SD.SessionCart));
                
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);

            }
        }
    }
}
