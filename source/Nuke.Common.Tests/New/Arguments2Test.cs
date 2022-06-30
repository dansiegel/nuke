// Copyright 2022 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.EventStream;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Nuke.Common.Tests;

public class Settings2Test
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Settings2Test(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void IntegerScalarTest()
    {
        var settings = new DotNetOptions();

        settings = settings.SetInteger(1).Copy();
        settings.Integer.Should().Be(1);

        settings = settings.ResetInteger().Copy();
        settings.Integer.Should().BeNull();
    }

    [Fact]
    public void StringScalarTest()
    {
        var settings = new DotNetOptions();

        settings = settings.SetString("foo").Copy();
        settings.String.Should().Be("foo");

        settings = settings.ResetString().Copy();
        settings.String.Should().BeNull();
    }

    [Fact]
    public void DictionaryTest()
    {
        var settings = new DotNetOptions();
        var properties = new Dictionary<string, object>
        {
            {"foo", "bar"},
            {"baz", "qux"}
        };

        settings = settings.SetProperties(properties).Copy();
        settings.Properties.Should().BeEquivalentTo(properties);

        settings = settings.SetProperty("foo", "buzz").Copy();
        settings.Properties["foo"].Should().Be("buzz");

        settings = settings.RemoveProperty("baz").Copy();
        settings.Properties.Should().NotContainKey("baz");

        Action action = () => settings.AddProperty("foo", "existing");
        action.Should().Throw<Exception>();

        settings = settings.EnableSpecialProperty().Copy();
        settings.Properties["SpecialProperty"].As<bool>().Should().BeTrue();

        settings = settings.ClearProperties().Copy();
        settings.Properties.Should().BeEmpty();

        settings = settings.ResetProperties().Copy();
        settings.Properties.Should().BeNull();
    }

    [Fact]
    public void ListTest()
    {
        var settings = new DotNetOptions();
        var flags = new List<BindingFlags>
        {
            BindingFlags.Static,
            BindingFlags.DeclaredOnly
        };

        settings = settings.SetFlags(flags).Copy();
        settings.Flags.Should().BeEquivalentTo(flags);

        settings = settings.AddFlag(BindingFlags.Instance).Copy();
        settings.Flags.Should().Contain(BindingFlags.Instance);

        settings = settings.RemoveFlag(BindingFlags.Static).Copy();
        settings.Flags.Should().NotContain(BindingFlags.Static);

        settings = settings.ClearFlags().Copy();
        settings.Flags.Should().BeEmpty();

        settings = settings.ResetFlags().Copy();
        settings.Flags.Should().BeNull();
    }

    [Fact]
    public void LookupTest()
    {
        var settings = new DotNetOptions();
        var traits = new LookupTable<string, int>
        {
            ["foo"] = new[] { 1, 2, 3 },
            ["bar"] = new[] { 3, 4, 5 },
        };

        settings = settings.SetTraits(traits).Copy();
        settings.Traits.Should().BeEquivalentTo(traits);

        settings = settings.SetTrait("buzz", 1000).Copy();
        settings.Traits["buzz"].Should().Equal(1000);

        settings = settings.AddTrait("foo", 4, 5).Copy();
        settings.Traits["foo"].Should().Equal(1, 2, 3, 4, 5);

        settings = settings.RemoveTrait("foo", 2).Copy();
        settings.Traits["foo"].Should().Equal(1, 3, 4, 5);

        settings = settings.RemoveTrait("foo").Copy();
        settings.Traits["foo"].Should().BeEmpty();

        settings = settings.SetTrait("buzz", 9).Copy();
        settings.Traits["buzz"].Should().Equal(9);

        settings = settings.ClearTraits().Copy();
        settings.Traits.Should().BeEmpty();

        settings = settings.ResetTraits().Copy();
        settings.Traits.Should().BeNull();
    }

    [Fact]
    public void NestedTest()
    {
        var innerSettings = new DotNetOptions();

        innerSettings = innerSettings.SetInteger(1).Copy();
        innerSettings.Integer.Should().Be(1);

        var settings = new DotNetOptions();
        settings = settings.SetNested(innerSettings).Copy();

        settings.Nested.Integer.Should().Be(1);

        settings = settings.AddNestedList(new DotNetOptions().SetInteger(1));
        settings = settings.AddNestedList(new DotNetOptions().SetInteger(5));
    }

    [Fact]
    public void RenderTest()
    {
        var jobject = new JObject
        {
            ["format"] = "--no-build",
            ["value"] = true
        };

        var settings = new DotNetOptions()
            .SetInteger(5)
            .SetString("spacy value");

        _testOutputHelper.WriteLine(settings.GetArguments());
    }
}

public class DotNetTestResult
{
    public void Deconstruct(out int passed)
    {
        passed = Passed;
        // failed = Failed;
    }

    public static DotNetTestResult Create()
    {
        return null;
    }

    public int Passed { get; set; }
    public int Failed { get; set; }
}

class CliAttribute : Attribute
{
    public string Executable { get; set; }
    public string PackageId { get; set; }
    public bool? RequiresFramework { get; set; }
}

class CommandAttribute : Attribute
{
    public string Arguments { get; set; }
    public Type OptionsType { get; set; } // Allows adding the command
}

[Cli(Executable = "dotnet")]
class DotNetCli
{
    [Command]
    public static (IReadOnlyCollection<Output> Output, int ExitCode) DotNet(string arguments)
    {
        // return Processing.Execute(arguments)
        return (null, 0);
    }

    [Command(Arguments = "restore", OptionsType = typeof(DotNetOptions))]
    public static (IReadOnlyCollection<Output> Output, int ExitCode) Restore(Configure<DotNetOptions> configurator)
    {
        // return Processing.Execute(arguments)
        return (null, 0);
    }
}

class CommandOptionsAttribute : Attribute
{
    public Type CliType { get; set; }
    public string Command { get; set; }
}

class ArgumentAttribute : Attribute
{
    public int? Position { get; set; }
    public string Format { get; set; }
    public string AlternativeFormat { get; set; }
    public bool? IsSecret { get; set; }
    public string FormatterMethod { get; set; }
    public Type FormatterType { get; set; }
    public string CollectionSeparator { get; set; }
    public string Container { get; set; }
}

partial class DotNetOptions
{
    internal partial string Foo(string value) => null;
}

[CommandOptions(CliType = typeof(DotNetCli), Command = nameof(DotNetCli.DotNet))]
public partial class DotNetOptions : CliOptionsBuilder
{
    internal partial string Foo(string value);

    /// <summary>
    /// Foo
    /// </summary>
    [Argument(Format = "--integer {value}")]
    public int? Integer => GetScalar<int?>(() => Integer);
    [Argument(Format = "--string {value}")]
    public string String => GetScalar<string>(() => String);
    [Argument(Format = "--secret {value}")]
    public string Secret => GetScalar<string>(() => Secret);
    [Argument(Format = "/p:{key}={value}")]
    public IReadOnlyDictionary<string, object> Properties => GetComplex<Dictionary<string, object>>(() => Properties);
    [Argument(Format = "--flags {value}", CollectionSeparator = ",")]
    public IReadOnlyCollection<BindingFlags> Flags => GetComplex<Collection<BindingFlags>>(() => Flags);
    public ILookup<string, int> Traits => GetComplex<LookupTable<string, int>>(() => Traits);

    public DotNetOptions Nested => GetComplex<DotNetOptions>(() => Nested);
    public IReadOnlyCollection<DotNetOptions> NestedList => GetComplex<Collection<DotNetOptions>>(() => NestedList);
}

public class OptionsModificatorAttribute : Attribute
{
    public OptionsModificatorAttribute(Type optionsType, string property)
    {
        OptionsType = optionsType;
        Property = property;
    }

    public Type OptionsType { get; }
    public string Property { get; }
}

public static class SettingsExtensions
{
    /// <summary><inheritdoc cref="DotNetOptions.Integer"/></summary>
    [OptionsModificator(typeof(DotNetOptions), nameof(DotNetOptions.Integer))]
    public static T SetInteger<T>(this T o, int? value) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Integer, value));
    public static T ResetInteger<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.Integer));
    public static T SetString<T>(this T o, [Secret] string value) where T : DotNetOptions => o.Copy(b => b.Set(() => o.String, value));
    public static T ResetString<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.String));
    public static T SetSecret<T>(this T o, [Secret] string value) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Secret, value));
    public static T ResetSecret<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.Secret));

    public static T SetProperties<T>(this T o, IDictionary<string, object> dictionary) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Properties, dictionary.AsReadOnly()));
    public static T SetProperty<T>(this T o, string key, object value) where T : DotNetOptions => o.Copy(b => b.SetDictionary(() => o.Properties, key, value));
    public static T AddProperty<T>(this T o, string key, object value) where T : DotNetOptions => o.Copy(b => b.AddDictionary(() => o.Properties, key, value));
    public static T RemoveProperty<T>(this T o, string key) where T : DotNetOptions => o.Copy(b => b.RemoveDictionary(() => o.Properties, key));
    public static T ClearProperties<T>(this T o) where T : DotNetOptions => o.Copy(b => b.ClearDictionary(() => o.Properties));
    public static T ResetProperties<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.Properties));

    /// <summary>Sets <c>SpecialProperty</c> in <see cref="DotNetOptions.Properties"/>.</summary>
    public static T SetSpecialProperty<T>(this T o, string key, object value) where T : DotNetOptions => o.Copy(b => b.SetDictionary(() => o.Properties, "SpecialProperty", value));
    public static T RemoveSpecialProperty<T>(this T o, string key) where T : DotNetOptions => o.Copy(b => b.RemoveDictionary(() => o.Properties, "SpecialProperty"));
    public static T EnableSpecialProperty<T>(this T o) where T : DotNetOptions => o.Copy(b => b.SetDictionary(() => o.Properties, "SpecialProperty", true));
    public static T DisableSpecialProperty<T>(this T o) where T : DotNetOptions => o.Copy(b => b.SetDictionary(() => o.Properties, "SpecialProperty", false));

    public static T SetFlags<T>(this T o, IEnumerable<BindingFlags> collection) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Flags, collection.ToList().AsReadOnly()));
    public static T AddFlag<T>(this T o, BindingFlags value) where T : DotNetOptions => o.Copy(b => b.AddCollection(() => o.Flags, value));
    public static T RemoveFlag<T>(this T o, BindingFlags value) where T : DotNetOptions => o.Copy(b => b.RemoveCollection(() => o.Flags, value));
    public static T ClearFlags<T>(this T o) where T : DotNetOptions => o.Copy(b => b.ClearCollection(() => o.Flags));
    public static T ResetFlags<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.Flags));

    public static T SetNestedList<T>(this T o, IEnumerable<DotNetOptions> collection) where T : DotNetOptions => o.Copy(b => b.Set(() => o.NestedList, collection.ToList().AsReadOnly()));
    public static T AddNestedList<T>(this T o, DotNetOptions value) where T : DotNetOptions => o.Copy(b => b.AddCollection(() => o.NestedList, value));
    public static T RemoveNestedList<T>(this T o, DotNetOptions value) where T : DotNetOptions => o.Copy(b => b.RemoveCollection(() => o.NestedList, value));
    public static T ClearNestedList<T>(this T o) where T : DotNetOptions => o.Copy(b => b.ClearCollection(() => o.NestedList));
    public static T ResetNestedList<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.NestedList));

    public static T SetTraits<T>(this T o, ILookup<string, int> lookup) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Traits, lookup));
    public static T SetTrait<T>(this T o, string key, params int[] values) where T : DotNetOptions => o.Copy(b => b.SetLookup(() => o.Traits, key, values));
    public static T AddTrait<T>(this T o, string key, params int[] values) where T : DotNetOptions => o.Copy(b => b.AddLookup(() => o.Traits, key, values));
    public static T RemoveTrait<T>(this T o, string key, int value) where T : DotNetOptions => o.Copy(b => b.RemoveLookup(() => o.Traits, key, value));
    public static T RemoveTrait<T>(this T o, string key) where T : DotNetOptions => o.Copy(b => b.RemoveLookup(() => o.Traits, key));
    public static T ClearTraits<T>(this T o) where T : DotNetOptions => o.Copy(b => b.ClearLookup(() => o.Traits));
    public static T ResetTraits<T>(this T o) where T : DotNetOptions => o.Copy(b => b.Remove(() => o.Traits));

    public static T SetNested<T>(this T o, DotNetOptions value) where T : DotNetOptions => o.Copy(b => b.Set(() => o.Nested, value));
}

class SecretAttribute : Attribute
{
}

class Processing
{
    public static (IReadOnlyCollection<Output> Output, int ExitCode) Execute(string toolPath, string arguments, int? timeout)
    {
        return ExecuteAsync(toolPath, arguments, captureStandardOutput: true, captureErrorOutput: true, timeout).GetAwaiter().GetResult();
    }

    public static async Task<(IReadOnlyCollection<Output> Output, int ExitCode)> ExecuteAsync(
        string toolPath,
        string arguments,
        bool captureStandardOutput,
        bool captureErrorOutput,
        int? timeout)
    {
        var output = new List<Output>();
        var exitCode = 0;
        var cmd = Cli.Wrap(toolPath)
            .WithArguments(arguments);

        var cancellationTokenSource = timeout.HasValue ? new CancellationTokenSource() : null;
        cancellationTokenSource?.CancelAfter(timeout.Value);
        var cancellationToken = cancellationTokenSource?.Token ?? default(CancellationToken);

        await foreach (var cmdEvent in cmd.ListenAsync(cancellationToken))
        {
            switch (cmdEvent)
            {
                case StandardOutputCommandEvent std when captureStandardOutput:
                    output.Add(new Output { Type = OutputType.Std, Text = std.Text });
                    break;
                case StandardErrorCommandEvent err when captureErrorOutput:
                    output.Add(new Output { Type = OutputType.Std, Text = err.Text });
                    break;
                case ExitedCommandEvent exited:
                    exitCode = exited.ExitCode;
                    break;
            }
        }
        return (output, exitCode);
    }

    public static Command ExecuteCommand()
    {
        return null;
    }
}

// TODO: allow custom tool path using env var

// TODO: use Tool delegate for string based main command
