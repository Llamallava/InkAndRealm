using InkAndRealm.Shared;

namespace InkAndRealm.Client.State;

public sealed class UserState
{
    public AuthResponse? CurrentUser { get; private set; }
    public event Action? Changed;

    public void SetUser(AuthResponse? user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }
}
