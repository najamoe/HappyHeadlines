using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PublisherService.Clients
{
    public class DraftClient
    {
        private readonly HttpClient _httpClient;

        public DraftClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Draft?> GetDraftAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Draft>($"/api/draft/{id}");
        }
    }

    public class Draft
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
    }
}
