﻿//-----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.CSharp.Models;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp code generator.</summary>
    public class CSharpGenerator : GeneratorBase
    {
        private readonly CSharpTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        public CSharpGenerator(object rootObject)
            : this(rootObject, new CSharpGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        public CSharpGenerator(object rootObject, CSharpGeneratorSettings settings)
            : this(rootObject, settings, new CSharpTypeResolver(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public CSharpGenerator(object rootObject, CSharpGeneratorSettings settings, CSharpTypeResolver resolver)
            : base(rootObject, resolver, settings)
        {
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; }

        /// <inheritdoc />
        public override IEnumerable<CodeArtifact> GenerateTypes()
        {
            var baseArtifacts = base.GenerateTypes();
            var artifacts = new List<CodeArtifact>();

            foreach (var r in baseArtifacts)
            {
                if (Regex.Match(r.Code, "OneOf<.+?>") is var m && m.Success)
                {
                    var arity = m.Value.Count(c => c == ',') + 1;
                    var name = $"OneOf`{arity}";
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "OneOfAnonymous", new OneOfTemplateModel(arity, null, Settings));
                    if (!artifacts.Any(a => a.TypeName == name))
                    {
                        artifacts.Add(new CodeArtifact(name, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template));
                    }
                }
            }

            foreach (var r in baseArtifacts)
            {
                if (Regex.Match(r.Code, "OneOf(Base)?<.+?>") is var m && m.Success)
                {
                    var arity = m.Value.Count(c => c == ',') + 1;
                    var name = $"OneOfBase`{arity}";
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "OneOfBase", new OneOfTemplateModel(arity, null, Settings));
                    if (!artifacts.Any(a => a.TypeName == name))
                    {
                        artifacts.Add(new CodeArtifact(name, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template));
                    }
                }
            }

            if (baseArtifacts.Any(r => r.Code.Contains("JsonInheritanceConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceAttribute") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceAttribute", new TemplateModelBase());
                    artifacts.Add(new CodeArtifact("JsonInheritanceAttribute", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }

                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceConverter", new TemplateModelBase());
                    artifacts.Add(new CodeArtifact("JsonInheritanceConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            if (baseArtifacts.Any(r => r.Code.Contains("DateFormatConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("DateFormatConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "DateFormatConverter", new DateFormatConverterTemplateModel(Settings));
                    artifacts.Add(new CodeArtifact("DateFormatConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            if (artifacts.Any(r => r.Code.Contains("OneOfConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("OneOfConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "OneOfConverter", new TemplateModelBase());
                    artifacts.Add(new CodeArtifact("OneOfConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            return baseArtifacts.Concat(artifacts);
        }

        /// <inheritdoc />
        protected override string GenerateFile(IEnumerable<CodeArtifact> artifactCollection)
        {
            var model = new FileTemplateModel
            {
                Namespace = Settings.Namespace ?? string.Empty,
                GenerateNullableReferenceTypes = Settings.GenerateNullableReferenceTypes,
                TypesCode = artifactCollection.Concatenate()
            };

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        protected override CodeArtifact GenerateType(JsonSchema schema, string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                return GenerateEnum(schema, typeName);
            }
            else if (schema.IsUnionType && typeName != null)
            {
                return GenerateNamedOneOf(schema, typeName);
            }
            else
            {
                return GenerateClass(schema, typeName);
            }
        }

        private CodeArtifact GenerateClass(JsonSchema schema, string typeName)
        {
            var model = new ClassTemplateModel(typeName, Settings, _resolver, schema, RootObject);

            RenamePropertyWithSameNameAsClass(typeName, model.Properties);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Class", model);
            return new CodeArtifact(typeName, model.BaseClassName, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }

        private void RenamePropertyWithSameNameAsClass(string typeName, IEnumerable<PropertyModel> properties)
        {
            var propertyWithSameNameAsClass = properties.SingleOrDefault(p => p.PropertyName == typeName);
            if (propertyWithSameNameAsClass != null)
            {
                var number = 1;
                while (properties.Any(p => p.PropertyName == typeName + number))
                {
                    number++;
                }

                propertyWithSameNameAsClass.PropertyName = propertyWithSameNameAsClass.PropertyName + number;
            }
        }

        private CodeArtifact GenerateEnum(JsonSchema schema, string typeName)
        {
            var model = new EnumTemplateModel(typeName, schema, Settings);
            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Enum", model);
            return new CodeArtifact(typeName, CodeArtifactType.Enum, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }

        private CodeArtifact GenerateNamedOneOf(JsonSchema schema, string typeName)
        {
            var namedDetails =
                new NamedOneOfDetails(
                    typeName,
                    !(schema is JsonSchemaProperty) && !string.IsNullOrEmpty(schema.Description),
                    schema.Description,
                    schema.OneOf.Select((t,i) => _resolver.Resolve(t, t.IsNullable(Settings.SchemaType), t.HasTypeNameTitle ? t.Title : $"{typeName}Case{i+1}")).ToList()
                );

            var model = new OneOfTemplateModel(schema.OneOf.Count, namedDetails, Settings);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "OneOfNamed", model);
            return new CodeArtifact(typeName, null, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }
    }
}
