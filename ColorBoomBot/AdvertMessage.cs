using System;
using Telegram.Bot.Types;

namespace ConsoleApp5
{
    [Serializable]
    class AdvertMessage
    {
        public string Text { get; }

        public string FileId { get; } = null;

        public AdvertMessage(Message message)
        {
            Text = message.Text;
            if (message.Photo != null)
            {
                int maxSize = -1;
                foreach (var photo in message.Photo)
                {
                    if (photo.FileSize > maxSize)
                    {
                        maxSize = photo.FileSize;
                        FileId = photo.FileId;
                        Text = message.Caption;
                    }
                }
            }
        }
    }
}
