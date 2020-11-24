using System;

namespace dotnet_behaviours
{
    public class BehaviorError : Exception
    {
        string message;

        public BehaviorError(string message) : base(message)
        {
            this.message = message;
        }

        public String getMessage()
        {
            return message;
        }
    }
}
