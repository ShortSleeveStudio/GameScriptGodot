using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace GameScript
{
    public class CustomSerializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return Assembly.GetExecutingAssembly().GetType(typeName);
        }
    }
}
