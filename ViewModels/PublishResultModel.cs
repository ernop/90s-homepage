namespace FusekiC
{

        public class PublishResultModel
        {
            public string Message { get; set; }
            public bool Success { get; set; } = false;
            public int Count { get; set; } = 0;
            public string Details { get; set; } = "";
        }
    }
