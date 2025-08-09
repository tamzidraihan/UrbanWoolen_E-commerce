using Microsoft.AspNetCore.Mvc;
using UrbanWoolen.Data;
using UrbanWoolen.Models;

namespace UrbanWoolen.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Success()
        {
            TempData["CartMessage"] = "Payment Successful!";
            return RedirectToAction("MyOrders", "Order");
        }

        public IActionResult Fail()
        {
            TempData["CartMessage"] = "Payment Failed. Please try again.";
            return RedirectToAction("Index", "Cart");
        }

        public IActionResult Cancel()
        {
            TempData["CartMessage"] = "Payment Canceled.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        public async Task<IActionResult> IPN()
        {
            var form = await Request.ReadFormAsync();

            var status = form["status"];
            var tranId = form["tran_id"];
            var valId = form["val_id"];
            var amount = form["amount"];
            var verifyKey = form["verify_key"];

            Console.WriteLine("Received IPN for Transaction: " + tranId);
            Console.WriteLine("Status: " + status);
            Console.WriteLine("Validation ID: " + valId);

            // Optional: validate val_id with SSLCommerz validation API
            // (Only required if you want to double-confirm)

            // Update order in DB
            var order = _context.Orders.FirstOrDefault(o => ("ORDER" + o.Id) == tranId);
            if (order != null && status == "VALID")
            {
                order.Status = OrderStatus.Confirmed; // or Paid
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

    }
}
