using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NewsWebsite.Models.ViewModels;

namespace NewsWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 🔹 Listar usuarios
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "(sin email)",
                    Roles = roles
                });
            }

            return View(userRolesViewModel);
        }

        // 🔹 Editar roles (ya lo tienes)
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList(),
                SelectedRoles = await _userManager.GetRolesAsync(user)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = model.SelectedRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(model.SelectedRoles);

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            return RedirectToAction(nameof(Index));
        }

        // ✅ NUEVO: Crear usuario manualmente
        public IActionResult Create()
        {
            var model = new CreateUserViewModel
            {
                AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
                TempData["Success"] = $"Usuario {model.Email} creado con rol {model.SelectedRole}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            return View(model);
        }
    }
}
