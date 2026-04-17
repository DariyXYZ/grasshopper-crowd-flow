using System;
using System.Reflection;
using Grasshopper.Kernel.Types;

namespace GrasshopperComponents.Utilities
{
    internal static class GhObjectExtraction
    {
        /// <summary>
        /// Attempts to unwrap a Grasshopper object into the requested type.
        /// Handles GH wrappers, goo instances and simple reflection fallbacks.
        /// </summary>
        /// <typeparam name="T">Expected target type.</typeparam>
        /// <param name="source">Source object to unwrap.</param>
        /// <param name="result">Extracted instance when successful.</param>
        /// <returns><c>true</c> when extraction succeeded, otherwise <c>false</c>.</returns>
        public static bool TryExtract<T>(object? source, out T? result)
            where T : class
        {
            switch (source)
            {
                case null:
                    result = null;
                    return false;

                case T direct:
                    result = direct;
                    return true;

                case GH_ObjectWrapper wrapper:
                    return TryExtract(wrapper.Value, out result);

                case IGH_Goo goo:
                    return TryExtract(goo.ScriptVariable(), out result);
            }

            try
            {
                PropertyInfo? valueProperty = source.GetType().GetProperty("Value");
                if (valueProperty != null)
                {
                    object? reflectionValue = valueProperty.GetValue(source);
                    if (TryExtract(reflectionValue, out result))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore reflection issues and fall back to failure.
            }

            result = null;
            return false;
        }
    }
}
