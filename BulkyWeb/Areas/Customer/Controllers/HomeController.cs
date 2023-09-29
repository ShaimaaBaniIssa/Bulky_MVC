using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]

    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger ,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            // loged in
            if(claim != null)
            {
                HttpContext.Session.SetInt32(SD.SessionCart,
                   _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count());
            }
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(properties:"Category");
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Details(int productId)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitOfWork.Product.Get(c => c.Id == productId, properties: "Category"),
                Count = 1,
                ProductId = productId
               
            };
           
            return View(shoppingCart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            // user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            ShoppingCart shoppingCartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId
            && u.ProductId == shoppingCart.ProductId);
            if (shoppingCartFromDb != null)
            {
                // shopping cart exists
                shoppingCartFromDb.Count += shoppingCart.Count;
                // even though not calling update , EF is tracking that because we retrieve it
                // and then change the count 

                // id for shoppingCart is zero ,so it will not be updated
                _unitOfWork.ShoppingCart.Update(shoppingCartFromDb);
                _unitOfWork.Save();

               

            }
            else
            {
                //add cart record
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();

                // add shopping cart count to session 
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
            }
            TempData["success"] = "Cart updated successfully";


            //return View(shoppingCart);
            return RedirectToAction("Index");
        }
    }
}
