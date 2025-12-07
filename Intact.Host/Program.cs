using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Intact.Host;

ServerConfig a = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText("./config/config.json"), new  JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
});

var server = new ServerNode(a.Port, "Secret", a.UserTokens, [], a.Chats);

server.Start();