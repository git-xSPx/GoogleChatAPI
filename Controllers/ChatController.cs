using GoogleChatAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using GoogleChatAPI.Models;

namespace GoogleChatAPI.Controllers
{
    public class ChatController : Controller
    {
        // Для демонстрації використовуємо статичний список для зберігання повідомлень
        private static List<ChatMessage> messages = new List<ChatMessage>();

        // GET: Chat/Index
        public IActionResult Index()
        {
            return View(messages);
        }

        // POST: Chat/Receive
        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            string json;
            using (var reader = new StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            var chatData = JsonConvert.DeserializeObject<GoogleChatMessage>(json);
            if (chatData != null && chatData.Message != null)
            {
                messages.Add(new ChatMessage
                {
                    Sender = chatData.Message.Sender != null ? chatData.Message.Sender.Email : "Невідомий",
                    Text = chatData.Message.Text,
                    Timestamp = DateTime.Now
                });
                
                // Створюємо відповідь з тим самим текстом, який отримали
                var responsePayload = new { text = "Ти щойно написав: \"" + chatData.Message.Text  + "\"" };

                // За допомогою методу Json повертаємо JSON-об'єкт
                return Json(responsePayload);
            }

            return Ok();
        }
    }
}
