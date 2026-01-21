using InkAndRealm.Shared;
using Microsoft.JSInterop;
using System.Text.Json;

namespace InkAndRealm.Client.State;

public sealed class UserState
{
    private const string StorageKey = "inkAndRealm.auth";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IJSRuntime _js;

    public UserState(IJSRuntime js)
    {
        _js = js;
    }

    public AuthResponse? CurrentUser { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (CurrentUser is not null)
        {
            return;
        }

        try
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var user = JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions);
                if (user is not null && !string.IsNullOrWhiteSpace(user.SessionToken))
                {
                    CurrentUser = user;
                    Changed?.Invoke();
                }
            }
        }
        catch
        {
            // Ignore storage errors and continue without persisted auth.
        }
    }

    public async Task SetUserAsync(AuthResponse? user)
    {
        CurrentUser = user;
        await PersistAsync();
        Changed?.Invoke();
    }

    private async Task PersistAsync()
    {
        if (CurrentUser is null)
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            return;
        }

        var payload = JsonSerializer.Serialize(CurrentUser, JsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, payload);
    }
}
