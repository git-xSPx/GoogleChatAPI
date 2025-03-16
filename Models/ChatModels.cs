using System;

namespace GoogleChatAPI.Models
{
    // Модель для збереження повідомлення у додатку
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Модель для десеріалізації JSON payload від Google Chat
    public class GoogleChatMessage
    {
        public MessageContent Message { get; set; }
    }

    public class MessageContent
    {
        public SenderInfo Sender { get; set; }
        public SpaceInfo Space { get; set; }
        public string Text { get; set; }
    }

    public class SenderInfo
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }
    public class SpaceInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
