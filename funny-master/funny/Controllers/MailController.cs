using funny.Models;
using Microsoft.AspNetCore.Mvc;
using UsersProxy;

namespace funny.Controllers;

[ApiController]
[Route("mail")]
public class MailController : ControllerBase
{
    private readonly IUsersProxy _users;
    private readonly ILogger<MailController> _logger;

    public MailController(IUsersProxy users, ILogger<MailController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [HttpPost("Send")]
    public async Task<IActionResult> Send(ConnectDialog request, CancellationToken cancellationToken)
    {
        if (!await _users.AcceptRequestAsync(request.Name, request.Phone, cancellationToken))
            return StatusCode(403, "вы не обслуживаетесь");

        _logger.LogInformation("Обратная связь: {Name}, {Phone}, задачи: {Tasks}",
            request.Name, request.Phone, string.Join(", ", request.RequiredTask ?? []));
        return Ok(new { message = "Заявка принята" });
    }
}
