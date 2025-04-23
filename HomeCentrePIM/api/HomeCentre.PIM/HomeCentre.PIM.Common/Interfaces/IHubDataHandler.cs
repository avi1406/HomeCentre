using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IHubDataHandler
    {
        Task CreateIndex();

        Task AddGroupSubscription(long groupId, long userId, string clientId, string connectionId);

        Task AddUser(long userId, string clientId, string connectionId);

        Task RemoveUser(long userId, string clientId);

        Task RemoveUserExceptClientId(long userId, string clientId);

        Task<IEnumerable<string>> GetGroupSubscriptionConnectionId(long groupId, long userId);

        Task<IEnumerable<string>> GetGroupConnectionIds(long groupId);

        Task<IEnumerable<string>> GetGroupClientIds(long groupId);

        Task<IEnumerable<string>> GetUserConnectionId(long userId);

        Task<IEnumerable<string>> GetUserClientId(long userId);

        Task RemoveGroupSubscription(long groupId, long userId, string clientId);

        Task RemoveGroupSubscriptionExceptClientId(long groupId, long userId, string clientId);

        Task<int> GetGroupSubscriptionCount(long groupId);
    }
}