using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BillingService.Api.OpenApi;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ApiSchemaRequiredAttribute : Attribute;

/// <summary>
/// Marca no OpenAPI campos obrigatórios que permanecem nullable no CLR para que
/// o FluentValidation consiga distinguir ausência de zero ou string vazia.
/// </summary>
public sealed class RequiredPropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetCustomAttribute<ApiSchemaRequiredAttribute>() is null)
                continue;

            var explicitName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            var schemaPropertyName = schema.Properties.Keys.FirstOrDefault(name =>
                string.Equals(name, explicitName, StringComparison.Ordinal) ||
                string.Equals(name, camelCaseName, StringComparison.Ordinal) ||
                string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase));

            if (schemaPropertyName is null)
                continue;

            schema.Required.Add(schemaPropertyName);
            schema.Properties[schemaPropertyName].Nullable = false;
        }
    }
}
