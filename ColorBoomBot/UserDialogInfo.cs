using System.Collections.Generic;
using Telegram.Bot.Types;

namespace ConsoleApp5
{
    class UserDialogInfo
    {
        public UserDialogInfo(int userId)
        {
            UserId = userId;
        }

        public int UserId { get; }

        public string Choose { get; set; }

        public UserStep? DialogStep { get; set; } = UserStep.Color;

        public List<int> StepMessages { get; set; } = new List<int>();

        public int AfterColorMessage { get; set; }
    }
}
