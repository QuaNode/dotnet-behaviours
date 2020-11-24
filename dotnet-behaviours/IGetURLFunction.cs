using System;

namespace dotnet_behaviours
{
    public interface IGetURLFunction
    {
        Uri apply(string path);
        bool equals(object var1);
    }
}
