using CmsShoppingCart.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace CmsShoppingCart
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_AuthenticateRequest()
        {
            //Check if user is logged in
            if(User == null) { return; }
            //Get username
            string username = Context.User.Identity.Name;

            //Declare Array of Roles
            string[] roles = null;

            using(Db db = new Db())
            {
                //Populate Roles
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                roles = db.UserRoles.Where(x => x.UserId == dto.Id).Select(x => x.Role.Name).ToArray();
            }
            //Build IPrincipal Object
            IIdentity UserIdentity = new GenericIdentity(username);
            IPrincipal newuserObj = new GenericPrincipal(UserIdentity, roles);

            //Update Context.User
            Context.User = newuserObj;
        }
    }
}
