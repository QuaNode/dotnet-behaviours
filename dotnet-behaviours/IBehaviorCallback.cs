namespace dotnet_behaviours
{
    public interface IBehaviorCallback<T>
    {
        void callback(T t, BehaviorError e);
    }
}
