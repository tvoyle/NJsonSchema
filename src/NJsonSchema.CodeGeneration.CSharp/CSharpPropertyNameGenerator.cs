//-----------------------------------------------------------------------
// <copyright file="CSharpPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates the property name for a given CSharp <see cref="JsonSchemaProperty"/>.</summary>
    public class CSharpPropertyNameGenerator : IPropertyNameGenerator
    {
        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonSchemaProperty property)
        {
            if (property.ExtensionData != null && property.ExtensionData.ContainsKey("name"))
                return (string) property.ExtensionData["name"];
            return ConversionUtilities.ConvertToUpperCamelCase(property.Name
                    .Replace("\"", string.Empty)
                    .Replace("@", string.Empty)
                    .Replace("?", string.Empty)
                    .Replace("$", string.Empty)
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty)
                    .Replace("(", "_")
                    .Replace(")", string.Empty)
                    .Replace(".", "-")
                    .Replace("=", "-")
                    .Replace("+", "plus"), true)
                .Replace("*", "Star")
                .Replace(":", "_")
                .Replace("-", "_")
                .Replace("#", "_")
                .Replace("&", "And");
        }
    }
}
