using System;

namespace dotnet_behaviours
{
    class GetURLFunction : IGetURLFunction
    {
        private readonly string baseUri;

        public GetURLFunction(string baseUri)
        {
            this.baseUri = baseUri;
        }
        public Uri apply(string path)
        {
            Uri uri = new Uri(baseUri + path);
            return uri;
        }

        public bool equals(object var1)
        {
            return false;
        }
    }
}
