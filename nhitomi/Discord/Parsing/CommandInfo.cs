using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Discord.Parsing
{
    public class CommandInfo
    {
        readonly MethodBase _method;

        public readonly Func<object, object[], Task> InvokeAsync;

        readonly Regex _nameRegex;
        readonly Regex _parameterRegex;
        readonly Regex _optionRegex;

        readonly int _requiredParams;

        const RegexOptions _options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public CommandInfo(MethodInfo method)
        {
            _method = method;

            if (method.ReturnType != typeof(Task) &&
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                throw new ArgumentException($"{method} is not asynchronous.");

            InvokeAsync = (module, args) => (Task) method.Invoke(module, args);

            // build name regex
            var attr = method.GetCustomAttribute<CommandAttribute>();
            if (attr == null)
                throw new ArgumentException($"{method} is not a command.");

            _nameRegex = new Regex(BuildNamePattern(method, attr), _options);

            // build parameter regex
            var bindingExpression = method.GetCustomAttribute<BindingAttribute>()?.Expression ??
                                    $"[{string.Join("] [", method.GetParameters().Select(p => p.Name))}]";

            _parameterRegex = new Regex(BuildParameterPattern(bindingExpression), _options);
            _requiredParams = method.GetParameters().Count(p => !p.IsOptional);

            // build optional parameter regex
            _optionRegex = new Regex(BuildOptionPattern(method), _options);
        }

        static string BuildNamePattern(MemberInfo member, CommandAttribute commandAttr)
        {
            // find module prefixes
            var prefixes = new List<string>();
            var type = member.DeclaringType;

            do
            {
                var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();
                if (moduleAttr == null)
                    throw new ArgumentException($"{type} is not a module.");

                // add if prefixed
                if (moduleAttr.IsPrefixed)
                    prefixes.Add(string.Join('|', moduleAttr.GetNames()));

                // traverse upwards
                type = type.DeclaringType;
            }
            while (type != null);

            // reverse
            prefixes.Reverse();

            var builder = new StringBuilder().Append('^');

            // prepend prefixes
            foreach (var prefix in prefixes)
            {
                builder
                    .Append('(')
                    .Append(prefix)
                    .Append(')')
                    .Append(@"\b\s+");
            }

            // append command name
            builder
                .Append('(')
                .Append(string.Join('|', commandAttr.GetNames()))
                .Append(')')
                .Append(@"\b\s*");

            return builder.ToString();
        }

        static string BuildParameterPattern(string bindingExpression)
        {
            // split into parts
            var parts = bindingExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder().Append('^');

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                // parameter binding
                if (part.StartsWith('[') && part.EndsWith(']'))
                {
                    var name = part.Substring(1, part.Length - 2);

                    if (name.EndsWith('+'))
                        builder
                            .Append(@"(?<")
                            .Append(name.SubstringFromEnd(1))
                            .Append(@">.+)");
                    else
                        builder
                            .Append(@"(?<")
                            .Append(name)
                            .Append(@">\S+");
                }
                else
                {
                    // constant
                    builder
                        .Append('(')
                        .Append(part)
                        .Append(')');
                }

                builder
                    .Append(@"\b\s")
                    .Append(i == parts.Length - 1 ? "*" : "+");
            }

            return builder.ToString();
        }

        static string BuildOptionPattern(MethodBase method)
        {
            var parameters = method.GetParameters().Where(p => p.IsOptional).ToArray();
            var usedNames = new HashSet<string>();

            var builder = new StringBuilder().Append('^');

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                var attr = parameter.GetCustomAttribute<OptionAttribute>() ?? new OptionAttribute(parameter.Name);
                var names = attr.GetNames().Where(usedNames.Add).ToArray();

                if (names.Length == 0)
                    throw new ArgumentException($"{parameter} could not be bound.");

                builder
                    .Append(@"((?<=^|\s)(")
                    .Append(string.Join('|', names))
                    .Append(@")\b\s+(?<")
                    .Append(attr.Name)
                    .Append(@">[^-]+))");

                if (i != parameters.Length - 1)
                    builder.Append('|');
            }

            return builder.ToString();
        }

        public bool TryParse(string str, out object[] args)
        {
            args = null;

            // match name
            var nameMatch = _nameRegex.Match(str);
            if (!nameMatch.Success)
                return false;

            var argDict = new Dictionary<string, string>();

            // split required parameters and options
            var hyphenIndex = str.IndexOf('-');
            var paramStr = hyphenIndex == -1 ? str : str.Substring(0, hyphenIndex);
            var optionStr = hyphenIndex == -1 ? null : str.Substring(hyphenIndex);

            // match parameters
            var paramMatch = _parameterRegex.Match(paramStr);
            var paramGroups = paramMatch.Groups.Where(g => g.Success && g.Name != null).ToArray();

            if (paramGroups.Length != _requiredParams)
                return false;

            foreach (var group in paramGroups)
                argDict[group.Name] = group.Value.Trim();

            // match options
            if (!string.IsNullOrWhiteSpace(optionStr))
            {
                var optionMatches = _optionRegex.Matches(optionStr);

                foreach (var group in optionMatches.SelectMany(m => m.Groups.Where(g => g.Name != null)))
                    argDict[group.Name] = group.Value.Trim();
            }

            // bind values to parameters
            var argList = new List<object>();

            foreach (var parameter in _method.GetParameters())
            {
                // required parameter is missing
                if (!argDict.TryGetValue(parameter.Name.ToLowerInvariant(), out var value) && !parameter.IsOptional)
                    return false;

                // invalid value
                if (!TryParse(parameter, value, out var obj))
                    return false;

                argList.Add(obj);
            }

            args = argList.ToArray();
            return true;
        }

        static bool TryParse(ParameterInfo parameter, string str, out object value)
        {
            if (TryParse(parameter.ParameterType, str, out value))
                return true;

            if (parameter.IsOptional)
            {
                value = parameter.DefaultValue;
                return true;
            }

            return false;
        }

        static bool TryParse(Type type, string str, out object value)
        {
            if (type == typeof(string))
            {
                value = str;
                return true;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(str, out var val))
                {
                    value = val;
                    return true;
                }
            }

            else if (type.IsEnum)
            {
                if (Enum.TryParse(type, str, out var val))
                {
                    value = val;
                    return true;
                }
            }

            else if (type.IsArray)
            {
                var elementType = type.GetElementType();

                var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var values = new object[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (TryParse(elementType, part, out var val))
                    {
                        values[i] = val;
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                value = values;
                return true;
            }

            value = null;
            return false;
        }
    }
}