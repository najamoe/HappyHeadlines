using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Profanity
{
    public class ProfanityClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProfanityClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsCleanAsync(string text)
        {
            var client = _httpClientFactory.CreateClient("ProfanityService");

            var json = JsonSerializer.Serialize(new { text });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/profanity/check", content);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"ProfanityService error: {response.StatusCode}");

            var result = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ProfanityCheckResult>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return data?.isClean ?? false;
        }

        private class ProfanityCheckResult
        {
            public bool isClean { get; set; }
        }
    }
}
