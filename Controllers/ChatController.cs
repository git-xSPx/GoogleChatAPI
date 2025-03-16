using GoogleChatAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GoogleChatAPI.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Статичний список для зберігання повідомлень
        private static List<ChatMessage> messages = new List<ChatMessage>();

        // Збереження останнього отриманого spaceId та email відправника
        private static string lastSpaceId = "";
        private static string lastSenderEmail = "";

        // Змінні для роботи з токеном
        // Початковий токен (якщо є) та час його закінчення. Якщо токен невідомий, tokenExpirationTime буде в минулому.
        private static string currentAccessToken = "";
        private static DateTime tokenExpirationTime = DateTime.UtcNow;

        // GET: Chat/Index - перегляд отриманих повідомлень та форми відповіді
        public IActionResult Index()
        {
            return View(messages);
        }

        // POST: Chat/Receive - обробка вхідного webhook від Google Chat
        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            string json;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            var chatData = JsonConvert.DeserializeObject<GoogleChatMessage>(json);
            if (chatData != null && chatData.Message != null)
            {
                // Зберігаємо spaceId та email відправника для подальшої відповіді
                lastSpaceId = (chatData.Message.Space?.Name ?? "").Replace("spaces/", "") ;
                lastSenderEmail = chatData.Message.Sender?.Email ?? "Невідомий";

                messages.Add(new ChatMessage
                {
                    Sender = lastSenderEmail,
                    Text = chatData.Message.Text,
                    Timestamp = DateTime.Now
                });

                // Ехо-відповідь Google Chat (опціонально)
                var responsePayload = new { text = $"Ти щойно написав: \"{chatData.Message.Text}\"" };
                return Json(responsePayload);
            }

            return Ok();
        }

        // POST: Chat/SendResponse - відправлення відповіді користувачеві через Google Chat API
        [HttpPost]
        public async Task<IActionResult> SendResponse(string responseText)
        {
            if (string.IsNullOrEmpty(lastSpaceId))
            {
                ModelState.AddModelError("", "Немає збереженого spaceId. Спочатку отримайте повідомлення від користувача.");
                return View("Index", messages);
            }

            try
            {
                // Забезпечуємо, що access token дійсний (якщо потрібно, оновлюємо його)
                string validToken = await EnsureAccessTokenValidAsync();
                await SendMessageToChat(lastSpaceId, responseText, validToken);
                TempData["Success"] = "Повідомлення успішно надіслано.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при відправленні повідомлення: {ex.Message}");
                return View("Index", messages);
            }

            // Повертаємося на Index, де відображається список повідомлень та форма для відповіді
            return RedirectToAction("Index");
        }

        // Приватний метод для надсилання повідомлення через Google Chat API
        private async Task SendMessageToChat(string spaceId, string messageText, string accessToken)
        {
            string url = $"https://chat.googleapis.com/v1/spaces/{spaceId}/messages";
            var payload = new { text = messageText };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
                }
            }
        }

        // Метод, який перевіряє, чи access token ще дійсний. Якщо ні, викликає RefreshAccessTokenAsync.
        private async Task<string> EnsureAccessTokenValidAsync()
        {
            if (DateTime.UtcNow >= tokenExpirationTime)
            {
                currentAccessToken = await RefreshAccessTokenAsync();
            }
            return currentAccessToken;
        }

        // Метод для оновлення access token через refresh token
        private async Task<string> RefreshAccessTokenAsync()
        {
            using (var client = new HttpClient())
            {
                var requestParams = new Dictionary<string, string>
                {
                    { "client_id", _configuration["CLIENT_ID"] },
                    { "client_secret", _configuration["CLIENT_SECRET"] },
                    { "refresh_token", _configuration["REFRESH_TOKEN"] },
                    { "grant_type", "refresh_token" }
                };

                var requestContent = new FormUrlEncodedContent(requestParams);
                var response = await client.PostAsync("https://oauth2.googleapis.com/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(content);
                    string newAccessToken = jsonResponse.access_token;
                    int expiresIn = jsonResponse.expires_in;
                    // Встановлюємо час закінчення токена з буфером у 60 секунд
                    tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn - 60);
                    return newAccessToken;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Помилка оновлення токена: {response.StatusCode} - {errorContent}");
                }
            }
        }
    }
}
