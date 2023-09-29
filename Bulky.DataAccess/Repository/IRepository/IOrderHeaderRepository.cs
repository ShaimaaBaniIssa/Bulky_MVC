using Bulky.Models;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository :IRepository<OrderHeader>
    {
        void Update(OrderHeader orderHeader);
        void UpdateOrderStatus(int id,string orderStatus,string? paymentStatus=null);
		// payment status doesn't change frequently 
		void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId);


	}
}
