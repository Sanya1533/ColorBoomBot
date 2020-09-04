using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp5
{
    class TelegramPhoto
    {
        public TelegramPhoto(string fileId, string extension)
        {
            FileId = fileId;
            Extension = extension;
        }

        public string FileId { get; }

        public string Extension { get; }
    }
}
