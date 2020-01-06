using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace FusekiC.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet("/account/login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("/account/logout")]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            return Redirect("../../");
        }

        [AllowAnonymous]
        [HttpPost("/account/login")]
        public IActionResult Login(string username, string password)
        {
            using (var db = new FusekiContext())
            {
                var model = new LoginModel();
                model.Username = username;
                model.Password = password;
                model.Error = "";
                if (string.IsNullOrEmpty(model.Username))
                {
                    model.Error = "No username";
                    return View(model);
                }
                if (string.IsNullOrEmpty(model.Password))
                {
                    model.Error = "No password";
                    return View(model);
                }
                var s = new UTF8Encoding().GetBytes(model.Password);
                byte[] hashedSubmittedPassword = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(s);

                var user = db.Users.FirstOrDefault(el => el.Username == model.Username);
                if (user == null)
                {
                    model.Error = "Error";
                    return View(model);
                }

                if (user.Password.SequenceEqual(hashedSubmittedPassword))
                {
                    var claims = new List<Claim>
                        {
                            new Claim("user", model.Username),
                            new Claim("role", "Member")
                        };

                    HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", "user", "role")));
                    return RedirectToAction("List", "Admin");
                }
                else
                {
                    //wrong password
                    model.Error = "Error";
                    return View(model);
                }
            }
        }
    }
}

