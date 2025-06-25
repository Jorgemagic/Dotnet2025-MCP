using System;

namespace EvergineMCPServer.Components
{
    public class AIRequestEventArgs : EventArgs
    {
        public string Message { get; }
        public int Progress { get; }

        public AIRequestEventArgs(string message, int progress)
        {
            Message = message;
            Progress = progress;
        }
    }
}
