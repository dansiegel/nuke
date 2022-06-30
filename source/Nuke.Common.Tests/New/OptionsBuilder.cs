// Copyright 2022 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Nuke.Common.Tools.ReSharper;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.Tests;


public class CliOptionsBuilder : OptionsBuilder
{
    internal List<string> Secrets = new();

    internal string GetArguments()
    {
        var builder = new StringBuilder();
        //
        // var properties = InternalOptions.Properties().Select(x => x.Name)
        //     .Select(x => GetType().GetProperty(x))
        //     .Select(x => (Property: x, ));
        // foreach (var property in properties)
        // {
        //
        // }

        return builder.ToString();
    }
}

public static class OptionsBuilderExtensions
{

    internal static T Copy<T>(this T builder, Action<T> modification = null)
    {
        var newOptions = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(builder));
        modification?.Invoke(newOptions);
        return newOptions;
    }

}

[JsonObject(MemberSerialization.OptIn)]
[JsonConverter(typeof(RootPropertyConverter))]
public class OptionsBuilder
{
    [JsonRootProperty]
    protected JObject InternalOptions = new();

    // private readonly JsonSerializer _serializer = new JsonSerializer { Converters = { new Converter() } };

    private static string GetOptionName(LambdaExpression lambdaExpression)
    {
        var member = lambdaExpression.GetMemberInfo();
        return member.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? member.Name;
    }

    internal OptionsBuilder Set<T>(Expression<Func<T>> propertyProvider, T value)
    {
        InternalOptions[GetOptionName(propertyProvider)] = JValue.FromObject(value);
        return this;
    }

    internal OptionsBuilder Remove<T>(Expression<Func<T>> propertyProvider)
    {
        InternalOptions.Property(GetOptionName(propertyProvider))?.Remove();
        return this;
    }

    internal T GetScalar<T>(Expression<Func<object>> optionProvider)
    {
        return InternalOptions[GetOptionName(optionProvider)] is { } token ? token.Value<T>() : default;
    }

    internal T GetComplex<T>(Expression<Func<object>> optionProvider)
    {
        return GetComplex<T>((LambdaExpression)optionProvider);
    }

    private T GetComplex<T>(LambdaExpression optionProvider)
    {
        return InternalOptions[GetOptionName(optionProvider)] is { } token ? token.ToObject<T>() : default;
    }

    #region Dictionary

    private OptionsBuilder UsingDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, Action<Dictionary<TKey, TValue>> action)
    {
        var dictionary = GetComplex<Dictionary<TKey, TValue>>(optionProvider) ?? new Dictionary<TKey, TValue>();
        action.Invoke(dictionary);
        Set(optionProvider, dictionary);
        return this;
    }

    internal OptionsBuilder SetDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary[key] = value);
    }

    internal OptionsBuilder AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Add(key, value));
    }

    internal OptionsBuilder RemoveDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Remove(key));
    }

    internal OptionsBuilder ClearDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Clear());
    }

    #endregion

    #region Lookup

    private OptionsBuilder UsingLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, Action<LookupTable<TKey, TValue>> action)
    {
        var lookup = GetComplex<LookupTable<TKey, TValue>>(optionProvider) ?? new LookupTable<TKey, TValue>();
        action.Invoke(lookup);
        Set(optionProvider, lookup);
        return this;
    }

    internal OptionsBuilder SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, IEnumerable<TValue> values)
    {
        return UsingLookup(optionProvider, lookup => lookup[key] = values);
    }

    internal OptionsBuilder AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, IEnumerable<TValue> value)
    {
        return UsingLookup(optionProvider, lookup => lookup.AddRange(key, value));
    }

    internal OptionsBuilder RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key)
    {
        return UsingLookup(optionProvider, lookup => lookup.Remove(key));
    }

    internal OptionsBuilder RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingLookup(optionProvider, lookup => lookup.Remove(key, value));
    }

    internal OptionsBuilder ClearLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider)
    {
        return UsingLookup(optionProvider, lookup => lookup.Clear());
    }

    #endregion

    #region List

    private OptionsBuilder UsingCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, Action<Collection<T>> action)
    {
        var collection = GetComplex<Collection<T>>(optionProvider) ?? new Collection<T>();
        action.Invoke(collection);
        Set(optionProvider, collection);
        return this;
    }

    internal OptionsBuilder AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, T value)
    {
        return UsingCollection(optionProvider, collection => collection.Add(value));
    }

    internal OptionsBuilder RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, T value)
    {
        return UsingCollection(optionProvider, collection => collection.Remove(value));
    }

    internal OptionsBuilder ClearCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider)
    {
        return UsingCollection(optionProvider, collection => collection.Clear());
    }

    #endregion
}
