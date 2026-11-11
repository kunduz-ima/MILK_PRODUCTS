using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MILK_PRODUCTS.Models;
using System.Security.Claims;

namespace MILK_PRODUCTS.Controllers
{
    public class AccountController : Controller
    {
        // Контекст базы данных для работы с пользователями
        private readonly DbmsContext _context;
        // Логгер для записи событий
        private readonly ILogger<AccountController> _logger;
        // Конструктор с внедрением зависимостей
        public AccountController(
            DbmsContext context,
            ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }
        // GET: /Account/Login
        // Отображение формы входа
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            // Сохраняем URL для перенаправления после входа
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        // POST: /Account/Login
        // Обработка данных формы входа
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            // Проверка валидности модели
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Поиск пользователя в базе данных
                var user = await _context.Users
                    .Include(u => u.Role) // Включаем информацию о роли
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

                // Если пользователь не найден
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
                    _logger.LogWarning("Invalid login attempt for {Username}", model.Username);
                    return View(model);

                }

                // Аутентификация пользователя
                await SignInUser(user, model.RememberMe);
                _logger.LogInformation("User {Username} logged in.", user.Username);

                // Перенаправление после входа
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                _logger.LogError(ex, "Error during login for {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обработке запроса");
                return View(model);
            }
        }
        // POST: /Account/Logout
        // Выход пользователя из системы
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Завершение сеанса аутентификации
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }
        // GET: /Account/AccessDenied
        // Отображение страницы отказа в доступе
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        // Вспомогательный метод для аутентификации пользователя
        private async Task SignInUser(User user, bool rememberMe = false)
        {
            // Создание claims (утверждений) о пользователе
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()), // ID пользователя
                new Claim(ClaimTypes.Name, user.Username), // Имя пользователя
                new Claim(ClaimTypes.Role, user.Role.RoleName), // Роль пользователя
                // Кастомный Claim для режима "только просмотр" (активен только для директора)
                new Claim("ViewOnly", user.Role.RoleName == "Director" ? "true" : "false")
            };
            // Создание identity на основе claims
            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(30)
            };
            // Выполнение входа в систему
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}

