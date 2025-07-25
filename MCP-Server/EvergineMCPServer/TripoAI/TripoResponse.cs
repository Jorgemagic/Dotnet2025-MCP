﻿namespace EvergineMCPServer.TripoAI
{
    public class TripoResponse
    {
        public int code { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string task_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public Input input { get; set; }
        public Output output { get; set; }
        public int progress { get; set; }
        public int create_time { get; set; }
        public string prompt { get; set; }
        public Result result { get; set; }
    }

    public class Input
    {
        public string prompt { get; set; }

        public string model_version { get; set; }

        public bool texture { get; set; }
        public bool pbr { get; set; }
        public string texture_alignment { get; set; }        
    }

    public class Output
    {
        public string pbr_model { get; set; }        
    }

    public class Result
    {
        public Model pbr_model { get; set; }
    }

    public class Model
    {
        public string url { get; set; }
        public string type { get; set; }
    }

    public class TripoErrorResponse
    {
        public int code { get; set; }
        public string message { get; set; }
        public string suggestion { get; set; }
    }
}
