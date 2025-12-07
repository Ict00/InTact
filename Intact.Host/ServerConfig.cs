using InTactCommon.Objects;

namespace Intact.Host;

public record ServerConfig(int Port, List<ServerChatInfo> Chats, List<UserToken> UserTokens);