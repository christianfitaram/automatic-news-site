using Microsoft.AspNetCore.Identity;

namespace NewsWebsite.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames = { "Admin", "Editor", "Suscriptor" };

            // 🔹 Crear roles si no existen
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 🔹 Crear usuario admin
            string adminEmail = "admin@news.com";
            string adminPass = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPass);

                if (result.Succeeded)
                {
                    // Solo asignar el rol si el usuario se creó correctamente
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    Console.WriteLine("Error creating admin user:");
                    foreach (var error in result.Errors)
                        Console.WriteLine($" - {error.Description}");
                }
            }
            else
            {
                // Asegurar que tenga el rol Admin
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
