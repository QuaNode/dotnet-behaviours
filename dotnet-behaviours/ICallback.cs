using System;

namespace dotnet_behaviours
{
    public interface ICallback<T>
    {
        void callback(T t, Exception e);
    }
}
