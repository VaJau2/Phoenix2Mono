using System;
using System.Linq;
using System.Reflection;

public static class DependencyInjection
{
    public static T CreateClass<T>(Type type, IDependencies dependencies) where T : class
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance);

        if (constructors.Length <= 0)
        {
            return Activator.CreateInstance<T>();
        }
        
        var classes = dependencies.GetDictionary();
        
        var constructor = constructors.First();
        var classParameters = constructor.GetParameters();
        var parameters = new object[classParameters.Length];
        
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = classParameters[i].ParameterType.Name;
            parameters[i] = classes.Contains(parameterType)
                ? classes[parameterType]
                : null;
        }

        return (T)constructor.Invoke(parameters);
    }
}
