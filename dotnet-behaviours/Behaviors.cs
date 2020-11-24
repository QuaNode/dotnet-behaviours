using System.Collections.Generic;
using System;

namespace dotnet_behaviours
{
    public class Behaviors
    {
        Dictionary<string, object> behavioursJSON = null;
        Dictionary<string, object> parameters;
        private readonly IGetURLFunction getURL = null;

        protected Behaviors(IGetURLFunction getURL)
        {
            this.getURL = getURL;
        }

        public Behaviors(string baseUri, Dictionary<string, object> defaults, IExceptionCallback cb)
        {
            parameters = Utility.getDataFromSharedPreference();
            if (defaults != null) parameters.Clone(defaults);
            this.getURL = new GetURLFunction(baseUri);
            initiateBehaviour(cb);
        }

        private async void initiateBehaviour(IExceptionCallback cb)
        {
            HttpConnectionEstablishment httpConnectionEstablishment = new HttpConnectionEstablishment(this.getURL);
            try
            {
                behavioursJSON = await httpConnectionEstablishment.execute("/behaviours", "Get", null, null);
            }
            catch(Exception ex)
            {
                cb.callback(ex);
            }
        }

        public IFunction<Dictionary<string, object>, IBehaviorCallback<Dictionary<string, object>>, object> getBehaviour(string behaviourName)
        {
            if (behaviourName == null) throw new Exception("Invalid behaviour name");

            if (behavioursJSON == null) throw new Exception("Behaviors is not ready yet");

            Dictionary<string, object> behaviour = (Dictionary<string, object>) behavioursJSON["behaviourName"];
            if(behaviour == null) throw new Exception("This behaviour does not exist");

            return (IFunction<Dictionary<string, object>, IBehaviorCallback<Dictionary<string, object>>, object>) new Function(getURL, behaviourName, behaviour, parameters);
        }
    }
}
