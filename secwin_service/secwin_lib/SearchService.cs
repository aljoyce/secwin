using Microsoft.Extensions.Logging;
using System.Web;

namespace secwin_lib
{
    public class SearchService
    {
        private static readonly HttpClient _httpClient = new();

        private static readonly ILogger Log = LoggerSingleton.Instance.Logger;

        public SearchService()
        {

        }

        public static async Task<string> DoSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return "BAD SEARCH TERM";

            try
            {
                string encodedSearchTerm = HttpUtility.UrlEncode(searchTerm);
                string searchUrl = $"https://www.google.com/search?q={encodedSearchTerm}";

                Log.LogInformation("Searching for {searchUrl}", searchUrl);

                // TODO - Need to use google API here as these requests will be blocked
                // HttpResponseMessage response = await _httpClient.GetAsync(searchUrl);

                // if (!response.IsSuccessStatusCode)
                //     throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");

                // string content = await response.Content.ReadAsStringAsync();

                await Task.Delay(1000);

                return "SUCCESSFUL SEARCH";
            }
            catch (HttpRequestException ex)
            {
                Log.LogError(ex, "HTTP request failed");
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Unexpected error");
            }

            return "ERROR WITH SEARCH";
        }
    }
}