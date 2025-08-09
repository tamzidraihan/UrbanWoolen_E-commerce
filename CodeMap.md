# Code Map

Root: D:\Projects\UrbanWoolen_FINAL_CLEAN
Generated: 2025-08-08 02:31:45

## Controllers
- AdminController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\AdminController.cs)
  - Actions:
    - Index  []  ()  -> IActionResult
- CartController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\CartController.cs)
  - Actions:
    - Index  []  ()  -> IActionResult
    - AddToCart  []  ()  -> IActionResult
    - Remove  [HTTPPOST]  ()  -> IActionResult
    - Checkout  [HTTPGET]  ()  -> IActionResult
    - Checkout  [HTTPPOST]  ()  -> Task<IActionResult>
- HomeController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\HomeController.cs)
  - Actions:
    - Index  []  ()  -> Task<IActionResult>
    - Privacy  []  ()  -> IActionResult
- OrderController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\OrderController.cs)
  - Actions:
    - AllOrders  []  ()  -> Task<IActionResult>
    - UpdateStatus  [HTTPPOST]  ()  -> Task<IActionResult>
    - MyOrders  []  ()  -> Task<IActionResult>
- PaymentController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\PaymentController.cs)
  - Actions:
    - Success  []  ()  -> IActionResult
    - Fail  []  ()  -> IActionResult
    - Cancel  []  ()  -> IActionResult
    - IPN  [HTTPPOST]  ()  -> Task<IActionResult>
- ProductController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\ProductController.cs)
  - Actions:
    - Index  []  ()  -> Task<IActionResult>
    - Details  []  ()  -> Task<IActionResult>
    - Create  []  ()  -> IActionResult
    - Create  [HTTPPOST]  ()  -> Task<IActionResult>
    - Edit  []  ()  -> Task<IActionResult>
    - Edit  [HTTPPOST]  ()  -> Task<IActionResult>
    - Delete  []  ()  -> Task<IActionResult>
    - DeleteConfirmed  []  ()  -> Task<IActionResult>
    - Inventory  []  ()  -> Task<IActionResult>
    - Dashboard  []  ()  -> Task<IActionResult>
- ReviewController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\ReviewController.cs)
  - Actions:
    - Add  [HTTPPOST]  ()  -> Task<IActionResult>
- StoreController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\StoreController.cs)
  - Actions:
    - Index  []  ()  -> Task<IActionResult>
    - Details  []  ()  -> Task<IActionResult>
- WishlistController  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Controllers\WishlistController.cs)
  - Actions:
    - Index  []  ()  -> Task<IActionResult>
    - Add  [HTTPPOST]  ()  -> Task<IActionResult>
    - Remove  [HTTPPOST]  ()  -> Task<IActionResult>

## Models
- AdminOrderViewModel
- CartItem
- CheckoutViewModel
- DashboardViewModel
- EmailOtpVerification
- ErrorViewModel
- Order
- OrderItem
- Product
- Review
- WishlistItem

## Services
- Implementations:
  - SslCommerzService
- DI Registrations:
  - Transient  IEmailSender -> SmtpEmailSender  (D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Program.cs)

## Migrations
- Path: D:\Projects\UrbanWoolen_FINAL_CLEAN\UrbanWoolen\Migrations
- Count: 15
- Latest: ApplicationDbContextModelSnapshot.cs

