﻿using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using InventoryWeb.Models;
using InventoryBusinessLogic;
using InventoryBusinessLogic.Entity;

namespace InventoryWeb.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private static string loggedinUsername="";

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        //POST: /Account/MobileLogin
        [HttpPost]
        [AllowAnonymous]
        public async Task<String> MobileLogin()
        {
            UserBusinessLogic userlogic = new UserBusinessLogic();
            string username= "";
            string password = "";
            username = Request.Form.Get("username");
            password = Request.Form.Get("password");

            string userid = userlogic.getUserByUsername(username).Id;
            string name = userlogic.getUserByUsername(username).Name;
            string dept = userlogic.getUserByUsername(username).DepartmentID;
            string roles = userlogic.getUserByUsername(username).UserType;
            string res = "/" + userid + "/" +name + "/" + dept;
            var result = await SignInManager.PasswordSignInAsync(username, password, true, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    if (roles.Equals("DeptHead"))
                    {
                        return "DeptHead"+res;
                    }
                    else if (roles.Equals("Store Clerk"))
                    {
                        return "StoreClerk" + res;
                    }
                    else
                    {
                        return "Fail";
                    }
                case SignInStatus.LockedOut:
                    return "LockedOut";
                case SignInStatus.RequiresVerification:
                    return "RequiresVerification";
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return "Invalid login attempt";
            }
        }
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    var user = await UserManager.FindAsync(model.UserName, model.Password);
                    loggedinUsername = model.UserName;
                    var roles = await UserManager.GetRolesAsync(user.Id);

                    if (roles.Contains("InterimDepHead"))
                    {
                        DateTime today = DateTime.Now.Date;
                        Inventory i = new Inventory();
                        AspNetUsers user1 = i.AspNetUsers.Where(x => x.Id == user.Id).First<AspNetUsers>();
                        Department dep = i.Department.Where(x => x.DepartmentID == user1.DepartmentID).First<Department>();
                        if (today > dep.DepartmentHeadEndDate)
                        {
                            AspNetUserRoles role1 = i.AspNetUserRoles.Where(p => p.UserId == user1.Id).First();
                            AspNetUsers dephead = i.AspNetUsers.Where(p => p.UserType == "DeptHead" && p.DepartmentID == user1.DepartmentID).First();
                            dep.DepartmentHead = dephead.Id;
                            user1.UserType = "DeptStaff";
                            i.AspNetUserRoles.Remove(role1);
                            AspNetUserRoles userrole = new AspNetUserRoles();
                            userrole.UserId = role1.UserId;
                            userrole.RoleId = "4";
                            i.AspNetUserRoles.Add(userrole);
                            i.SaveChanges();
                            
                            roles = await UserManager.GetRolesAsync(user.Id);

                            if (roles.Contains("DeptStaff"))
                            {
                                return RedirectToAction("RaiseRequest", "Staff");
                            }



                        }
                        else
                        {
                            return RedirectToAction("ApproveOrReject", "DepManager");
                        }
                    }
                    

                    if (roles.Contains("StoreClerk"))
                    {
                        return RedirectToAction("RaiseRequest", "StoreClerk");
                    }
                    else if (roles.Contains("DeptRep"))
                    {
                        return RedirectToAction("StationaryRequest", "DepRepresentative");
                    }
                    else if (roles.Contains("DeptHead"))
                    {
                        DateTime today = DateTime.Now.Date;
                        string interimhead = "";
                        UserBusinessLogic BL = new UserBusinessLogic();
                        Inventory i = new Inventory();
                        AspNetUsers dephead = i.AspNetUsers.Where(x => x.Id == user.Id).First<AspNetUsers>();
                        ViewBag.depHead = BL.appointNewDepHead(dephead.Id);
                        Department dep = i.Department.Where(x => x.DepartmentID == dephead.DepartmentID).First<Department>();

                        for (int t = 0; t < ViewBag.depHead.Count; t++)
                        {
                            if (ViewBag.depHead[t].UserType == "InterimDepHead")
                            {

                                interimhead = ViewBag.depHead[t].UserType;
                                break;
                            }

                        }
                        
                        if (interimhead != "" && today> dep.DepartmentHeadEndDate)
                        {
                            AspNetUsers interim = i.AspNetUsers.Where(x => x.UserType == "InterimDepHead" && x.DepartmentID == dephead.DepartmentID).First<AspNetUsers>();
                            AspNetUserRoles role1 = i.AspNetUserRoles.Where(p => p.UserId == interim.Id).First();

                            interim.UserType = "DeptStaff";
                            dep.DepartmentHead = dephead.Id;
                            i.AspNetUserRoles.Remove(role1);
                            AspNetUserRoles userrole = new AspNetUserRoles();
                            userrole.UserId = role1.UserId;
                            userrole.RoleId = "4";
                            i.AspNetUserRoles.Add(userrole);
                            i.SaveChanges();
                            roles = await UserManager.GetRolesAsync(user.Id);
                            return RedirectToAction("ApproveOrReject", "DepManager");
                            
                        }
                        else { return RedirectToAction("ApproveOrReject", "DepManager"); }
                       
                    }
                    else if (roles.Contains("StoreSupervisor"))
                    {
                        return RedirectToAction("ViewInventory", "StoreSupervisor");
                    }

                    else if (roles.Contains("DeptStaff"))
                    {
                        return RedirectToAction("RaiseRequest", "Staff");
                    }
                    //else if (roles.Contains("DeptStaff"))
                    //{
                    //    return RedirectToAction("ViewInventory", "DepStaff");
                    //}
                    else if (roles.Contains("StoreManager"))
                    {
                        return RedirectToAction("ViewAdjustmentVoucherManager", "StoreManager");
                    }

                    else
                    {
                        return RedirectToLocal(returnUrl);
                    }
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            Inventory i = new Inventory();
            UserBusinessLogic userlogic = new UserBusinessLogic();
            string dept = userlogic.getUserByUsername(loggedinUsername).DepartmentID;
            if (dept.Substring(0, 4) == "STOR")
            {

                ViewBag.Roles = new SelectList(i.AspNetRoles.Where(x=>x.Name.Substring(0,4)=="Stor").ToList(), "Name", "Name");
            }
            else
            {
                ViewBag.Roles = new SelectList(i.AspNetRoles.Where(u => u.Name.Contains("DeptStaff"))
                                     .ToList(), "Name", "Name");
            }
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                   // await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                    await this.UserManager.AddToRoleAsync(user.Id, model.UserRoles);
                    Inventory i = new Inventory();
                    UserBusinessLogic userlogic = new UserBusinessLogic();
                    string dept = userlogic.getUserByUsername(loggedinUsername).DepartmentID;
                    string userType = model.UserRoles;

                    AspNetUsers newUser = i.AspNetUsers.Where(x => x.Id == user.Id).First();
                    newUser.DepartmentID = dept;
                    newUser.UserType = userType;

                    i.SaveChanges();

                   return RedirectToAction("ApproveOrReject", "DepManager");
                }
                Inventory inv = new Inventory();
                UserBusinessLogic userlogic1 = new UserBusinessLogic();
                string dept1 = userlogic1.getUserByUsername(loggedinUsername).DepartmentID;
                if (dept1.Substring(0, 4) == "STOR")
                {

                    ViewBag.Roles = new SelectList(inv.AspNetRoles.Where(x => x.Name.Substring(0, 4) == "Stor").ToList(), "Name", "Name");
                }
                else
                {
                    ViewBag.Roles = new SelectList(inv.AspNetRoles.Where(u => u.Name.Contains("DeptStaff"))
                                         .ToList(), "Name", "Name");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}