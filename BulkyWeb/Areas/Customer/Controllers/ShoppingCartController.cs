using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area(SD.Role_Customer)]
	[Authorize]
	public class ShoppingCartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty] // will automatically will populate with new values from view
		public ShoppingCartVM ShoppingCartVM { get; set; }
		public ShoppingCartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{

				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				properties: "Product"),
				OrderHeader = new()
			};
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPricePassedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(ShoppingCartVM);
		}
		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{

				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				properties: "Product"),
				OrderHeader = new()
			};
			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
				.Get(u => u.Id == userId);
			// populate the defaults
			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetName;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;


			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPricePassedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(ShoppingCartVM);
		}
		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			// populate the shoppingcartlist again because when we are posting we might lose some 
			// data because we don't have everything in summary view inputs

			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				properties: "Product");

			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
			ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

			//ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
			//	.Get(u => u.Id == userId); Exception --> can't add navigation property 
			// the value of ApplicationUser generate from AspNetUsers table

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser
				.Get(u => u.Id == userId);

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPricePassedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				// regular user
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			}
			else
			{
				// it is a company user
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
			}

			_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			// add order details to db
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				_unitOfWork.OrderDetail.Add(new()
				{
					ProductId = cart.ProductId,
					// because we have save the OrderHeader , it will populate an OrderHeaderId
					OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count,
				});
				_unitOfWork.Save();
			}
			// 
			//////////////////////// PAYMENT /////////////////////////
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//regular customer
				//stripe logic
				var domain = "https://localhost:7025/";
				// from stripe document
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain+ $"customer/shoppingCart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/shoppingCart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};


				foreach(var item in ShoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100), // 20.50 *100 = 2050
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);

				_unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id,
					session.Id,
					session.PaymentIntentId);
				// session.PaymentIntentId --> Null until the payment is Successfull
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303); // redirecting to new url

			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
		}
		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id,
				properties: "ApplicationUser");
			if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService(); // built in class in Stripe
				Session session = service.Get(orderHeader.SessionId);
				if(session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentId(id,
					session.Id,
					session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateOrderStatus(id, SD.StatusApproved
						, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
				HttpContext.Session.Clear();
				// remove shopping cart
				List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(
					u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
				_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
				_unitOfWork.Save();

			}

			return View(id);
		}
		public IActionResult Plus(int cartId)
		{
			var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
			cartFromDb.Count++;
			_unitOfWork.ShoppingCart.Update(cartFromDb);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Minus(int cartId)
		{
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);

            if (cartFromDb.Count <= 1)
			{
                HttpContext.Session.SetInt32(SD.SessionCart,
                 _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
               
            }
			else
			{
				cartFromDb.Count--;
				_unitOfWork.ShoppingCart.Update(cartFromDb);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId ,tracked:true);
			//// decrease shoppingCart Count
            HttpContext.Session.SetInt32(SD.SessionCart,
                 _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            
			_unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}

		private double GetPricePassedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count <= 50)
			{
				return shoppingCart.Product.Price;
			}
			else
			{
				if (shoppingCart.Count <= 100)
				{
					return shoppingCart.Product.Price50;

				}
				else
				{
					return shoppingCart.Product.Price100;

				}
			}

		}
	}
}
