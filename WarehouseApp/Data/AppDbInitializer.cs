using WarehouseApp.Helpers;
using WarehouseApp.Models;

namespace WarehouseApp.Data
{
    public static class AppDbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHelper.HashPassword("admin123"), // ✅ хешуємо пароль
                    Role = "Admin"
                };

                var user = new User
                {
                    Username = "user",
                    PasswordHash = PasswordHelper.HashPassword("user123"), // ✅ хешуємо пароль
                    Role = "User"
                };

                context.Users.AddRange(admin, user);
                context.SaveChanges();
            }
        }
    }
}
