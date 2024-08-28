using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace TasksManagement.API.Common.Extensions;
public static class JsonExtensions
{
    public static string ToJsonWithCamelCase(this object value)
        => JsonConvert.SerializeObject(value, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
}
