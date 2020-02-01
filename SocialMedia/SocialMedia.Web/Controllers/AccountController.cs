﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SocialMedia.Models;
using SocialMedia.Models.IdentityModels;

namespace SocialMedia.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IUserClaimsPrincipalFactory<User> _claimsPrincipalFactory;
        private readonly SignInManager<User> _signInManager;

        public AccountController(ILogger<AccountController> logger,
            UserManager<User> userManager,
            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory,
            SignInManager<User> signInManager)
        {
            this._logger = logger;
            this._userManager = userManager;
            this._claimsPrincipalFactory = claimsPrincipalFactory;
            this._signInManager = signInManager;
        }

        //
        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            this._logger.LogInformation(Request.Path);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this._userManager.FindByNameAsync(model.UserName);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = model.UserName,
                        Email = model.Email
                    };


                    await this._userManager.CreateAsync(user, model.Password);

                    this._logger.LogInformation($"{user.UserName} added successfully with Id {user.Id}", user);

                    TempData["Message"] = $"{user.UserName} was registered!";

                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }
        #endregion

        //
        #region Login
        [HttpGet]
        public IActionResult LoginAsync()
        {
            this._logger.LogInformation(Request.Path);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this._userManager.FindByNameAsync(model.UserName);

                if (user != null && await this._userManager.CheckPasswordAsync(user, model.Password))
                {
                    var principal = await this._claimsPrincipalFactory.CreateAsync(user);

                    await HttpContext.SignInAsync("Identity.Application", principal);

                    this._logger.LogInformation($"{user.UserName} successfully signed in", user);

                    TempData["Message"] = $"{user.UserName} successfully signed in";
                    return RedirectToAction("Index", "Home");
                }
                this._logger.LogError("Invalid UserName or Password");
                ModelState.AddModelError("", "Invalid UserName or Password");
            }
            return View();
        }
        #endregion

        //Logout
        public async Task<IActionResult> LogoutAsync()
        {
            await this._signInManager.SignOutAsync();
            _logger.LogInformation($"User logged out.");

            TempData["Message"] = $"{User.Identity.Name} successfully logged out.";
            return RedirectToAction("Index", "Home");
        }

        //Edit user details
        [HttpGet]
        public IActionResult ManageUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ManageUser(User newUserDetails)
        {
            var user = await _userManager.GetUserAsync(User);

            //If there is no user throw exception
            if (user == null)
            {
                return NotFound(
                    $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            user = UpdateUserDetails(user, newUserDetails);

            await this._userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            TempData["Message"] = "Your profile has been updated";

            return View();
        }

        private User UpdateUserDetails(User user, User newUserDetails)
        {
            //TODO: Check all props DOB, Gender, etc...
            //TODO: Separate user details to acc details and personal details
            //TODO: Show current details of the user
            if (user.Email != newUserDetails.Email)
                user.Email = newUserDetails.Email;
            if (user.FirstName != newUserDetails.FirstName)
                user.FirstName = newUserDetails.FirstName;
            if (user.LastName != newUserDetails.LastName)
                user.LastName = newUserDetails.LastName;
            if (user.Country != newUserDetails.Country)
                user.Country = newUserDetails.Country;
            
            return user;
        }
    }
}