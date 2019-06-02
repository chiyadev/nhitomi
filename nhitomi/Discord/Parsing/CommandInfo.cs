using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nhitomi.Discord.Parsing
{
    public class CommandInfo
    {
        public string FullName
        {
            get
            {
                var names = new List<string>
                {
                    _attribute.Name
                };

                var type = _method.DeclaringType;

                do
                {
                    var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();

                    // add if prefixed
                    if (moduleAttr != null && moduleAttr.IsPrefixed)
                        names.Add(moduleAttr.Name);

                    // traverse upwards
                    type = type.DeclaringType;
                }
                while (type != null);

                // reverse
                names.Reverse();

                return string.Join('/', names);
            }
        }

        readonly CommandAttribute _attribute;

        readonly MethodBase _method;
        readonly ParameterInfo[] _parameters;
        readonly Dictionary<string, ParameterInfo> _parameterDict;

        readonly int _requiredParams;

        readonly DependencyFactory<object> _moduleFactory;

        readonly Regex _nameRegex;
        readonly Regex _parameterRegex;
        readonly Regex _optionRegex;

        const RegexOptions _options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public CommandInfo(MethodInfo method)
        {
            _method = method;
            _parameters = method.GetParameters();
            _parameterDict = _parameters.ToDictionary(p => p.Name);
            _requiredParams = _parameters.Count(p => !p.IsOptional);

            _moduleFactory = DependencyUtility.CreateFactory(method.DeclaringType);

            if (method.ReturnType != typeof(Task) &&
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                throw new ArgumentException($"{method} is not asynchronous.");

            // build name regex
            _attribute = method.GetCustomAttribute<CommandAttribute>();

            if (_attribute == null)
                throw new ArgumentException($"{method} is not a command.");

            _nameRegex = new Regex(BuildNamePattern(method, _attribute), _options);

            // build parameter regex
            var bindingExpression = method.GetCustomAttribute<BindingAttribute>()?.Expression;

            if (bindingExpression == null && _parameters.Length != 0)
                bindingExpression = $"[{string.Join("] [", _parameterDict.Keys)}]";

            _parameterRegex = new Regex(BuildParameterPattern(bindingExpression ?? ""), _options);

            // build optional parameter regex
            _optionRegex = new Regex(BuildOptionPattern(_parameters.Where(p => p.IsOptional).ToArray()), _options);
        }

        static string BuildNamePattern(MemberInfo member, CommandAttribute commandAttr)
        {
            // find module prefixes
            var prefixes = new List<string>();
            var type = member.DeclaringType;

            do
            {
                var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();

                // add if prefixed
                if (moduleAttr != null && moduleAttr.IsPrefixed)
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

            if (commandAttr.BindName)
            {
                // append command name
                builder
                    .Append('(')
                    .Append(string.Join('|', commandAttr.GetNames()))
                    .Append(')')
                    .Append(@"($|\s+)");
            }

            return builder.ToString();
        }

        static string BuildParameterPattern(string bindingExpression)
        {
            var builder = new StringBuilder().Append('^');

            // split into parts
            var parts = bindingExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                // parameter binding
                if (part.Length > 2 && part.StartsWith('[') && part.EndsWith(']'))
                {
                    var name = part.Substring(1, part.Length - 2);

                    if (name.EndsWith('+'))
                        builder
                            .Append(@"(?<")
                            .Append(name.Remove(name.Length - 1))
                            .Append(@">.+)");
                    else
                        builder
                            .Append(@"(?<")
                            .Append(name)
                            .Append(@">\S+)");
                }
                else
                {
                    // constant
                    builder
                        .Append('(')
                        .Append(part)
                        .Append(')');
                }

                if (i != parts.Length - 1)
                    builder.Append(@"\b\s+");
            }

            builder.Append(@"($|\s+)");

            return builder.ToString();
        }

        static string BuildOptionPattern(IReadOnlyList<ParameterInfo> parameters)
        {
            var usedNames = new HashSet<string>();

            var builder = new StringBuilder().Append('^');

            for (var i = 0; i < parameters.Count; i++)
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

                if (i != parameters.Count - 1)
                    builder.Append('|');
            }

            return builder.ToString();
        }

        public bool TryParse(string str, out Dictionary<string, object> args)
        {
            args = null;

            // match name
            var nameMatch = _nameRegex.Match(str);
            if (!nameMatch.Success)
                return false;

            str = str.Substring(nameMatch.Index + nameMatch.Length);

            // split required parameters and options
            var hyphenIndex = str.IndexOf('-');
            var paramStr = hyphenIndex == -1 ? str : str.Substring(0, hyphenIndex);
            var optionStr = hyphenIndex == -1 ? null : str.Substring(hyphenIndex);

            var argStrings = new Dictionary<string, string>();

            // match parameters
            var paramMatch = _parameterRegex.Match(paramStr);
            var paramGroups = paramMatch.Groups
                .Where(g => g.Success && _parameterDict.ContainsKey(g.Name))
                .ToArray();

            if (paramGroups.Length != _requiredParams)
                return false;

            // parameters must match exactly
            if (!string.IsNullOrWhiteSpace(paramStr.Remove(paramMatch.Index, paramMatch.Length)))
                return false;

            foreach (var group in paramGroups)
                argStrings[group.Name] = group.Value.Trim();

            // match options
            if (!string.IsNullOrWhiteSpace(optionStr))
            {
                var optionMatches = _optionRegex.Matches(optionStr);

                foreach (var group in optionMatches.SelectMany(m => m.Groups.Where(g => g.Name != null)))
                    argStrings[group.Name] = group.Value.Trim();
            }

            args = new Dictionary<string, object>();

            // parse values
            foreach (var parameter in _parameters)
            {
                // required parameter is missing
                if (!argStrings.TryGetValue(parameter.Name, out var value) && !parameter.IsOptional)
                    return false;

                // parse value
                if (TryParse(parameter, value, out var obj))
                    args[parameter.Name] = obj;

                // couldn't parse value and parameter is required
                else if (!parameter.IsOptional)
                    return false;
            }

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
                if (Enum.TryParse(type, str, true, out var val))
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

        public async Task InvokeAsync(IServiceProvider services, Dictionary<string, object> args)
        {
            // create module
            var module = _moduleFactory(services);

            // convert to argument list
            var argList = new List<object>();

            foreach (var parameter in _parameters)
            {
                if (args.TryGetValue(parameter.Name, out var value))
                {
                    argList.Add(value);
                }
                else
                {
                    // fill missing optional arguments from services
                    var service = services.GetService(parameter.ParameterType);

                    if (service == null)
                        throw new InvalidOperationException($"Could not inject {parameter} of {_method}.");

                    argList.Add(service);
                }
            }

            // invoke asynchronously
            await (dynamic) _method.Invoke(module, argList.ToArray());
        }
    }
}