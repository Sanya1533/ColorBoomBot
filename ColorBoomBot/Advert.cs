using System;

namespace ConsoleApp5
{
    [Serializable]
    class Advert
    {
        private string name = null;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name == null)
                    name = value;
            }
        }

        public AdvertMessage Message { get; set; } = null;

        public TimeSpan? Period { get; set; } = null;

        public int Count { get; set; } = 0;

        public int? MaxCount { get; set; } = null;

        public bool IsCreating
        {
            get
            {
                return Name == null || Message == null || Period == null || MaxCount == null;
            }
        }

        public bool IsActive { get; set; }
    }
}
