using Application.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("errors/{code}")]
    [ApiExplorerSettings(IgnoreApi = true)] // لإخفائه من Swagger
    public class ErrorController : ControllerBase
    {
        public IActionResult Error(int code)
        {
            return new ObjectResult(new ApiResponse(code));
        }
    }
}
