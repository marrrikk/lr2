namespace UsersProxy;

public interface IUsersProxy
{
    Task<bool> AcceptRequestAsync(string name, string phone, CancellationToken cancellationToken = default);
}
