using InkAndRealm.Shared;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Json;
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
    private readonly HttpClient _http;

    public UserState(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
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
                    var response = await TryValidateSessionAsync(user.SessionToken);
                    if (response is null)
                    {
                        // If validation cannot be reached (for example network issues),
                        // preserve the cached user instead of forcing an unexpected logout.
                        CurrentUser = user;
                        Changed?.Invoke();
                        return;
                    }

                    using (response)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var validatedUser = await response.Content.ReadFromJsonAsync<AuthResponse>();
                            if (validatedUser is not null && !string.IsNullOrWhiteSpace(validatedUser.SessionToken))
                            {
                                CurrentUser = validatedUser;
                                await PersistAsync();
                                Changed?.Invoke();
                                return;
                            }

                            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                            return;
                        }

                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                            return;
                        }
                    }

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

    private async Task<HttpResponseMessage?> TryValidateSessionAsync(string sessionToken)
    {
        try
        {
            var requestUrl = $"api/auth/session?token={Uri.EscapeDataString(sessionToken)}";
            return await _http.GetAsync(requestUrl);
        }
        catch
        {
            return null;
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
