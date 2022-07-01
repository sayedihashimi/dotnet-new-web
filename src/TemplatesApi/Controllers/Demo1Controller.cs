using Microsoft.AspNetCore.Mvc;

namespace TemplatesApi.Controllers;

[Route("api/[controller]")]
public class Demo1Controller : Controller
{
#if  DEBUG
    [HttpPost]
    // just for API testing
    public ActionResult AddTemplate(string identity)
    {
        if (string.IsNullOrEmpty(identity))
        {
            return BadRequest();
        }

        return CreatedAtAction(nameof(AddTemplate), new { id = identity }, $"Created: {identity}");
    }
#endif
}