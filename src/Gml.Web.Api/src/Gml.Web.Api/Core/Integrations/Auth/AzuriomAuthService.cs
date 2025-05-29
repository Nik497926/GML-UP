using System.Text;
using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AzuriomAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : IPlatformAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<AuthResult> Auth(string login, string password, string? totp = null, bool isSlim = false)
    {
        var authService = (await gmlManager.Integrations.GetActiveAuthService())!.Endpoint;

        var baseUri = new Uri(authService);

        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/api/auth/authenticate";

        var dto = JsonConvert.SerializeObject(new
        {
            email = login,
            password,
            code = totp ?? string.Empty,
            isSlim = isSlim
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result =
            await _httpClient.PostAsync(endpoint, content);

        var resultContent = await result.Content.ReadAsStringAsync();

        // Проверка на необходимость 2FA
        if (resultContent.Contains("\"status\":\"pending\"") && resultContent.Contains("\"reason\":\"2fa\""))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = "Введите код из приложения 2FA",
                TwoFactorEnabled = true
            };
        }

        var model = JsonConvert.DeserializeObject<AzuriomAuthResult>(resultContent);

        if (!result.IsSuccessStatusCode && resultContent.Contains("invalid_credentials", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = $"Неверный логин или пароль."
            };
        }

        if (model is null || model.Banned || !result.IsSuccessStatusCode || (!result.IsSuccessStatusCode && resultContent.Contains("banned", StringComparison.OrdinalIgnoreCase)))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = $"Пользователь заблокирован."
            };
        }

        return new AuthResult
        {
            Login = login,
            IsSuccess = result.IsSuccessStatusCode
        };
    }
}
