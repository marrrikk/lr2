using funny.Data;
using funny.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsersProxy;

namespace funny.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StrangeDataController : ControllerBase
    {
        private static readonly List<ConnectDialogColaVM> _ColaOrdersDbInMemory = new List<ConnectDialogColaVM>();
        private static readonly List<ConnectDialogPizzaVM> _PizzaOrdersDbInMemory = new List<ConnectDialogPizzaVM>();
        private static long _ColaCurrentId = 1;
        private static long _PizzaCurrentId = 1;

        private readonly ILogger<StrangeDataController> _logger;
        private readonly AppDbContext _context;
        private readonly IUsersProxy _users;

        public StrangeDataController(ILogger<StrangeDataController> logger, AppDbContext context, IUsersProxy users)
        {
            _logger = logger;
            _context = context;
            _users = users;
        }

        [HttpPost("SendCola")]
        public async Task<IActionResult> SendCola([FromBody] ConnectDialogColaVM colaVM, CancellationToken cancellationToken)
        {
            if (colaVM == null)
                return BadRequest("Данные заказа не могут быть пустыми.");

            if (!await _users.AcceptRequestAsync(colaVM.Name, colaVM.Phone, cancellationToken))
                return StatusCode(403, "вы не обслуживаетесь");

            _logger.LogInformation("Получен заказ колы. Вкус: {Tasty}, Объем: {Volume}, Имя: {Name}, Телефон: {Phone}",
                colaVM.Tasty, colaVM.Volume, colaVM.Name, colaVM.Phone);

            colaVM.Id = _ColaCurrentId++;
            _ColaOrdersDbInMemory.Add(colaVM);

            return Ok(colaVM.Id);
        }

        [HttpGet("GetAllCola")]
        public IActionResult GetAllCola()
        {
            return Ok(_ColaOrdersDbInMemory);
        }

        [HttpGet("GetCola/{id}")]
        public IActionResult GetCola(long id)
        {
            var cola = _ColaOrdersDbInMemory.FirstOrDefault(c => c.Id == id);
            if (cola == null) return NotFound("Заказ колы не найден.");
            return Ok(cola);
        }

        [HttpPut("UpdateCola/{id}")]
        public IActionResult UpdateCola(long id, [FromBody] ConnectDialogColaVM updatedCola)
        {
            var cola = _ColaOrdersDbInMemory.FirstOrDefault(c => c.Id == id);
            if (cola == null) return NotFound("Заказ колы не найден.");

            cola.Tasty = updatedCola.Tasty;
            cola.Volume = updatedCola.Volume;
            cola.Name = updatedCola.Name;
            cola.Phone = updatedCola.Phone;

            _logger.LogInformation("Обновлен заказ колы с ID {Id}", id);
            return Ok(cola);
        }

        [HttpDelete("DeleteCola/{id}")]
        public IActionResult DeleteCola(long id)
        {
            var cola = _ColaOrdersDbInMemory.FirstOrDefault(c => c.Id == id);
            if (cola == null) return NotFound("Заказ колы не найден.");

            _ColaOrdersDbInMemory.Remove(cola);
            _logger.LogInformation("Удален заказ колы с ID {Id}", id);
            return Ok(new { message = $"Заказ колы с ID {id} успешно удален." });
        }

        [HttpPost("SendPizza")]
        public async Task<IActionResult> SendPizza([FromBody] ConnectDialogPizzaVM pizzaVM, CancellationToken cancellationToken)
        {
            if (pizzaVM == null)
                return BadRequest("Данные заказа не могут быть пустыми.");

            if (!await _users.AcceptRequestAsync(pizzaVM.Name, pizzaVM.Phone, cancellationToken))
                return StatusCode(403, "вы не обслуживаетесь");

            _logger.LogInformation("Получен заказ пиццы. Размер: {Size}, Опции: {Options}, Толщина: {Thickness}",
                pizzaVM.Size, pizzaVM.Options, pizzaVM.Thickness);

            pizzaVM.Id = _PizzaCurrentId++;
            _PizzaOrdersDbInMemory.Add(pizzaVM);

            return Ok(pizzaVM.Id);
        }

        [HttpGet("GetAllPizza")]
        public IActionResult GetAllPizza()
        {
            return Ok(_PizzaOrdersDbInMemory);
        }

        [HttpGet("GetPizza/{id}")]
        public IActionResult GetPizza(long id)
        {
            var pizza = _PizzaOrdersDbInMemory.FirstOrDefault(p => p.Id == id);
            if (pizza == null) return NotFound("Заказ пиццы не найден.");
            return Ok(pizza);
        }

        [HttpPut("UpdatePizza/{id}")]
        public IActionResult UpdatePizza(long id, [FromBody] ConnectDialogPizzaVM updatedPizza)
        {
            var pizza = _PizzaOrdersDbInMemory.FirstOrDefault(p => p.Id == id);
            if (pizza == null) return NotFound("Заказ пиццы не найден.");

            pizza.Size = updatedPizza.Size;
            pizza.Options = updatedPizza.Options;
            pizza.Thickness = updatedPizza.Thickness;

            _logger.LogInformation("Обновлен заказ пиццы с ID {Id}", id);
            return Ok(pizza);
        }

        [HttpDelete("DeletePizza/{id}")]
        public IActionResult DeletePizza(long id)
        {
            var pizza = _PizzaOrdersDbInMemory.FirstOrDefault(p => p.Id == id);
            if (pizza == null) return NotFound("Заказ пиццы не найден.");

            _PizzaOrdersDbInMemory.Remove(pizza);
            _logger.LogInformation("Удален заказ пиццы с ID {Id}", id);
            return Ok(new { message = $"Заказ пиццы с ID {id} успешно удален." });
        }

        [HttpPost("CreateService")]
        public async Task<IActionResult> CreateService([FromBody] DigitalService service)
        {
            if (service == null) return BadRequest("Данные услуги пусты.");

            _context.DigitalServices.Add(service);
            await _context.SaveChangesAsync();
            return Ok(service);
        }

        [HttpGet("GetAllServicesJson")]
        public async Task<IActionResult> GetAllServicesJson()
        {
            var services = await _context.DigitalServices.ToListAsync();
            return Ok(services);
        }

        [HttpGet("GetService/{id}")]
        public async Task<IActionResult> GetService(long id)
        {
            var service = await _context.DigitalServices.FindAsync(id);
            if (service == null) return NotFound("Услуга не найдена.");
            return Ok(service);
        }

        [HttpPut("UpdateService/{id}")]
        public async Task<IActionResult> UpdateService(long id, [FromBody] DigitalService updatedService)
        {
            var service = await _context.DigitalServices.FindAsync(id);
            if (service == null) return NotFound("Услуга не найдена.");

            service.Name = updatedService.Name;
            service.Description = updatedService.Description;
            service.Price = updatedService.Price;

            await _context.SaveChangesAsync();
            return Ok(service);
        }

        [HttpDelete("DeleteService/{id}")]
        public async Task<IActionResult> DeleteService(long id)
        {
            var service = await _context.DigitalServices.FindAsync(id);
            if (service == null) return NotFound("Услуга не найдена.");

            _context.DigitalServices.Remove(service);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Услуга с ID {id} успешно удалена." });
        }

        [HttpGet("GetDigitalList")]
        public async Task<IActionResult> GetDigitalList([FromQuery] DigitalListRequest request)
        {
            try
            {
                IQueryable<DigitalService> query = _context.DigitalServices;

                if (!string.IsNullOrWhiteSpace(request.Filter))
                {
                    string filterPattern = $"%{request.Filter.Trim()}%";
                    query = query.Where(s => EF.Functions.Like(s.Name, filterPattern)
                                          || EF.Functions.Like(s.Description, filterPattern));
                }

                query = request.Sorted?.ToLower() switch
                {
                    "descending" => query.OrderByDescending(s => s.Name),
                    "ascending" => query.OrderBy(s => s.Name),
                    _ => query.OrderBy(s => s.Name)
                };

                var services = await query.ToListAsync();

                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<ul class='digital-services-list'>");

                if (services.Count == 0)
                {
                    htmlBuilder.Append("<li class='no-data'>Услуги не найдены</li>");
                }
                else
                {
                    foreach (var service in services)
                    {
                        htmlBuilder.Append($"<li class='service-item' data-id='{service.Id}'>");
                        htmlBuilder.Append($"  <h3 class='service-name'>{service.Name}</h3>");
                        htmlBuilder.Append($"  <p class='service-description'>{service.Description}</p>");
                        htmlBuilder.Append($"  <span class='service-price'>Цена: {service.Price.ToString("F2", CultureInfo.InvariantCulture)} руб.</span>");
                        htmlBuilder.Append("</li>");
                    }
                }

                htmlBuilder.Append("</ul>");

                return Content(htmlBuilder.ToString(), "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации HTML списка услуг");
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost("HelpPolice")]
        public IActionResult HelpPolice()
        {
            string userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Поступил запрос на вызов полиции. User-Agent: {UserAgent}", userAgent);
            bool isChrome = userAgent.Contains("Chrome")
                            && !userAgent.Contains("Edg")
                            && !userAgent.Contains("OPR");
            if (isChrome)
            {
                _logger.LogWarning("Пользователь использует Chrome. Полиция выехала!");
                return Ok(new { message = "Помощь уже в пути. Полиция выехала к пользователю Chrome!" });
            }
            else { _logger.LogInformation("В помощи отказано: пользователь сидит не через Chrome."); return BadRequest(new { message = "Отказано в помощи. Мы помогаем только пользователям Chrome." }); }
        }
    }
}
