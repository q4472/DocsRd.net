using System;
using System.Web.Mvc;

namespace DocsRd.net.Controllers
{
    public class HomeController : Controller
    {
        public Object Index()
        {
            // Заход при отладке. Метод GET. Параметров нет.
            Guid sessionId = new Guid("12345678-1234-1234-1234-123456789012");
            return View(sessionId);
        }
    }
}
