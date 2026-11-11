using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MILK_PRODUCTS.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Проверяет, находится ли пользователь в режиме "только просмотр" (директор).
        /// </summary>
        protected bool IsViewOnly()
        {
            return User.HasClaim(c => c.Type == "ViewOnly" && c.Value == "true");
        }

        /// <summary>
        /// Проверяет, можно ли выполнять изменяющие действия.
        /// Возвращает null, если разрешено, иначе IActionResult (редирект с предупреждением).
        /// </summary>
        protected IActionResult CheckEditPermission()
        {
            if (!IsViewOnly()) return null; // не директор — можно

            // Директор: проверяем, не находится ли он в контроллере зарплат
            var controllerName = ControllerContext.ActionDescriptor.ControllerName;
            if (controllerName.Equals("Salaries", StringComparison.OrdinalIgnoreCase))
            {
                return null; // зарплату редактировать можно
            }

            // Во всех остальных контроллерах — запрет на изменения
            TempData["Warning"] = "У вас только права на просмотр. Изменение данных недоступно.";
            return RedirectToAction("Index");
        }
    }
}