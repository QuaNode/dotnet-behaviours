using System;

namespace dotnet_behaviours
{
    public interface IExceptionCallback
    {
        void callback(Exception exception);
    }
}
