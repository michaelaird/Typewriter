using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Typewriter.CodeModel;
using Typewriter.CodeModel.Attributes;
using Typewriter.Generation;

namespace Typewriter.TemplateEditor.Lexing
{
    public class Context 
    {
        public Context(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public readonly Dictionary<string, Identifier> identifiers = new Dictionary<string, Identifier>();
        
        public Identifier GetIdentifier(string name)
        {
            Identifier i;
            return name != null && identifiers.TryGetValue(name, out i) ? i : null;
        }

        public ICollection<Identifier> Identifiers
        {
            get { return identifiers.Values.OrderBy(i => i.Name).ToArray(); }
        }
    }

    public class ContextSpan
    {
        public ContextSpan(int start, int end, Context context)
        {
            Start = start;
            End = end;
            Context = context;
        }

        public int Start { get; private set; }
        public int End { get; private set; }
        public Context Context { get; private set; }
    }

    public static class Contexts
    {
        private static readonly Type extensions = typeof(Extensions);
        private static readonly Dictionary<string, Context> items = new Dictionary<string, Context>();

        public static Context Find(string name)
        {
            return items[name];
        }

        static Contexts()
        {
            var contexts = typeof(ContextAttribute).Assembly.GetTypes().Where(t => t.GetCustomAttribute<ContextAttribute>() != null);

            foreach (var c in contexts)
            {
                var name = c.GetCustomAttribute<ContextAttribute>().Name;
                var item = new Context(name);

                var properties = c.GetProperties().Where(p => p.GetCustomAttribute<PropertyAttribute>() != null);
                var inherited = c.GetInterfaces().SelectMany(i => i.GetProperties().Where(p => p.GetCustomAttribute<PropertyAttribute>() != null));

                foreach (var p in properties.Concat(inherited))
                {
                    var propertyName = p.GetCustomAttribute<PropertyAttribute>().Name;
                    var propertyDescription = p.GetCustomAttribute<PropertyAttribute>().Description.Replace("$context", name.ToLower());
                    var identifier = new Identifier { Name = p.Name, QuickInfo = propertyName + "\r\n" + propertyDescription };
                    var ctx = p.PropertyType.GenericTypeArguments.FirstOrDefault();

                    if (ctx != null && ctx.GetCustomAttribute<ContextAttribute>() != null)
                    {
                        var ctxn = ctx.GetCustomAttribute<ContextAttribute>().Name;
                        identifier.Context = ctxn;
                        identifier.IsCollection = true;
                    }
                    
                    if (p.PropertyType == typeof(bool))
                    {
                        identifier.IsBoolean = true;
                    }
                    else if (p.PropertyType.GetCustomAttribute<ContextAttribute>() != null)
                    {
                        var ctxn = p.PropertyType.GetCustomAttribute<ContextAttribute>().Name;
                        identifier.Context = ctxn;
                        identifier.HasContext = true;
                    }
                    
                    item.identifiers.Add(p.Name, identifier);
                }

                var methods = extensions.GetMethods().Where(m => m.GetCustomAttribute<PropertyAttribute>() != null && m.GetParameters().All(p => p.ParameterType == c));
                var inheritedMethods = c.GetInterfaces().SelectMany(i => extensions.GetMethods().Where(m => m.GetCustomAttribute<PropertyAttribute>() != null && m.GetParameters().All(p => p.ParameterType == i)));
                
                foreach (var m in methods.Concat(inheritedMethods))
                {
                    var methodName = m.GetCustomAttribute<PropertyAttribute>().Name;
                    var methodDescription = m.GetCustomAttribute<PropertyAttribute>().Description.Replace("$context", name.ToLower());
                    var identifier = new Identifier { Name = m.Name, QuickInfo = methodName + "\r\n" + methodDescription };
                    var ctx = m.ReturnType.GenericTypeArguments.FirstOrDefault();

                    if (ctx != null && ctx.GetCustomAttribute<ContextAttribute>() != null)
                    {
                        var ctxn = ctx.GetCustomAttribute<ContextAttribute>().Name;
                        identifier.Context = ctxn;
                        identifier.IsCollection = true;
                    }

                    if (m.ReturnType == typeof(bool))
                    {
                        identifier.IsBoolean = true;
                    }
                    else if (m.ReturnType.GetCustomAttribute<ContextAttribute>() != null)
                    {
                        var ctxn = m.ReturnType.GetCustomAttribute<ContextAttribute>().Name;
                        identifier.Context = ctxn;
                        identifier.HasContext = true;
                    }

                    item.identifiers.Add(m.Name, identifier);
                }

                items.Add(name, item);
            }
        }
    }
}