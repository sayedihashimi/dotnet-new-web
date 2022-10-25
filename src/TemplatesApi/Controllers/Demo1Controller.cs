using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemplatesShared;

namespace TemplatesApi.Controllers;

[Route("api/[controller]")]
public class Demo1Controller : Controller
{
    [HttpPost]
    public ActionResult AddTemplate([FromBody]string identity)
    {
        if (string.IsNullOrEmpty(identity))
        {
            Console.WriteLine("identity is null in request");
            return BadRequest();
        }

        return CreatedAtAction(nameof(AddTemplate), new { id = identity }, $"Created: {identity}");
    }

    [HttpPut()]
    public ActionResult<Template> UpdateTemplate([FromBody]Template template)
    {
        Console.WriteLine($"Updating template");
        if (template == null)
        {
            return BadRequest();
        }

        template.Author = "new author";
        template.Name = "new name";
        return template;
    }
}
[Route("api/[controller]")]
public class Demo2Controller : Controller{
    [HttpPost]
    public ActionResult AddTemplate([FromForm]string identity)
    {
        if (string.IsNullOrEmpty(identity))
        {
            Console.WriteLine("identity is null in request");
            return BadRequest();
        }

        return CreatedAtAction(nameof(AddTemplate), new { id = identity }, $"Created: {identity}");
    }
}
[Route("api/[controller]")]
public class Demo3Controller : Controller
{
    [HttpPut()]
    public ActionResult<Template> UpdateTemplate([FromForm]Template template)
    {
        Console.WriteLine($"Updating template");
        if (template == null)
        {
            return BadRequest();
        }

        template.Author = "new author";
        template.Name = "new name";
        return template;
    }
}