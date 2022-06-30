// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nuke.Common.Utilities
{
    public class JsonRootPropertyAttribute : Attribute
    {
    }
    public class RootPropertyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var rootProperty = GetRootProperty(value.GetType());
            var options = JToken.FromObject(rootProperty.GetValue(value).NotNull());
            options.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            existingValue = Activator.CreateInstance(objectType);
            var rootProperty = GetRootProperty(objectType);
            var jobject = JObject.Load(reader);
            rootProperty.SetValue(existingValue, jobject.ToObject(rootProperty.GetMemberType()));
            return existingValue;
        }

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return GetRootProperty(objectType) != null;
        }

        private MemberInfo GetRootProperty(Type objectType)
        {
            return objectType.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.GetCustomAttribute<JsonRootPropertyAttribute>() != null);
        }
    }

    public static partial class JObjectExtensions
    {
        public static JEnumerable<T> GetChildren<T>(this JObject jobject, string name)
            where T : JToken
        {
            return jobject.GetPropertyValue<JArray>(name).Children<T>();
        }

        public static JEnumerable<JObject> GetChildren(this JObject jobject, string name)
        {
            return jobject.GetChildren<JObject>(name);
        }
    }
}
