using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Account;
using CmsShoppingCart.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CmsShoppingCart.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }
        // GET: /account/login
        [HttpGet]
        public ActionResult Login()
        {
            //Confirm user is not logged in
            string username = User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");
            //return view


            //
            return View();
        }
        // POST: /account/login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            //check model state
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            //check if the user is valid
            bool isValid = false;

            using(Db db = new Db())
            {
                if(db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }
            }
            if(!isValid)
            {
                ModelState.AddModelError("", "Invalid Username or password.");
                return View(model);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
            }
            return View();
        }
        // GET: /account/create-account
        [HttpGet]
        [ActionName("create-account")]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }
        // POST: /account/create-account
        [HttpPost]
        [ActionName("create-account")]
        public ActionResult CreateAccount(UserVM model)
        {
            //check model state
            if(!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            //check if passwords match
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View("CreateAccount", model);
            }

            using(Db db = new Db())
            {
                //Make sure username is unique
                if(db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", "Username: " + model.Username + " is taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                //Create userDTO
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Username = model.Username,
                    Password = model.Password               
                };

                //Add the DTO
                db.Users.Add(userDTO);

                //Save 
                db.SaveChanges();

                //Add to UserRolesDTO
                int id = userDTO.Id;

                UserRoleDTO userRolesDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };
                db.UserRoles.Add(userRolesDTO);
                db.SaveChanges();
            }

            //Create a TemData Message
            TempData["SM"] = "You are now registered and can login.";

            //Redirect
            return Redirect("~/account/login");
        }
        // GET: /account/Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            //Get the username
            string username = User.Identity.Name;

            //Declare the model
            UserNavPartialVM model;

            using(Db db = new Db())
            {
                //Get the User
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                //Build the model
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }
            //Return Partial View with model
            return PartialView(model);
        }
        // GET: /account/user-profile
        [HttpGet]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile()
        {
            //Get Username
            string username = User.Identity.Name;

            //Declare model
            UserProfileVM model;

            using (Db db = new Db())
            {
                //Get User
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                //Build model
                model = new UserProfileVM(dto);
            }
            //Return view with model
            return View("UserProfile", model);
        }
        // POST: /account/user-profile
        [HttpPost]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile(UserProfileVM model)
        {
            //check model state
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            //check if passwords match
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords do not match");
                    return View("UserProfile", model);
                }
            }

            using (Db db = new Db())
            {
                //Get Username
                string username = User.Identity.Name;

                //Make sure username is unique
                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.Username == username))
                {
                    ModelState.AddModelError("", "Username: " + model.Username + " already exists.");
                    model.Username = "";
                    return View("UserProfile", model);
                }

                //Edit DTO
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                //Save
                db.SaveChanges();
            }
            //Set TempData message
            TempData["SM"] = "You have edited your profile!";

            //Redirect
            return Redirect("~/account/user-profile");
        }
        // GET: /account/Orders
        [Authorize(Roles ="User")]
        public ActionResult Orders()
        {
            //Init List of OrdersForUserVM
            List<OrdersForUserVM> ordersForUser = new List<OrdersForUserVM>();

            using(Db db = new Db())
            {
                //et UserId
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;

                //Init List of OrderVM
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x)).ToList();

                //Loop trou list of OrderVM
                foreach (var order in orders)
                {
                    //Init Product Dict
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    //Declare Toatl
                    decimal total = 0m;

                    //Init List of OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsDTO = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //Loop Trou List of OrdersForUserVM
                    foreach (var orderDetails in orderDetailsDTO)
                    {
                        //et Product
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();
                        //et Product price
                        decimal price = product.Price;

                        //et Product name
                        string productName = product.Name;

                        //Add to product dictionary
                        productsAndQty.Add(productName, orderDetails.Quantity);

                        //et total 
                        total += orderDetails.Quantity * price;
                    }
                    //Add to OrdersForUserVM list
                    ordersForUser.Add(new OrdersForUserVM
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }
            //Return view wit List of OrdersForUserVM
            return View(ordersForUser);
        }
    }
}