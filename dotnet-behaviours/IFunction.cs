using System;

namespace dotnet_behaviours
{
    public interface IFunction<F, L, G>
    {
        G apply(F var1, L var2);
        bool equals(Object var1, Object var2);
    }
}
