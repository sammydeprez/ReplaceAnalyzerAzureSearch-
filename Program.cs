using System;
using System.Threading.Tasks;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.Search;
using System.Net.Http;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ReplaceAnalyzer
{
    class Program
    {
        // Configurable names
        private const string AzureSearchName = "qnamakermulti-asp4eu44uas446y";
        private const string AzureSearchApiKey = "01387A9217A46BFDD4C6D831EE2B0C4B";
        private const string IndexName = "af15a1b1-b2f4-426f-9352-e04d0fa83982";
        private const string NewAnalyzer = "nl.microsoft";


        // Clients
        private static ISearchServiceClient _searchClient;
        private static HttpClient _httpClient = new HttpClient();
        private static string _searchServiceEndpoint;

        private static Index NewIndex;

        static void Main(string[] args)
        {

            _searchClient = new SearchServiceClient(AzureSearchName, new SearchCredentials(AzureSearchApiKey));
            _httpClient.DefaultRequestHeaders.Add("api-key", AzureSearchApiKey);
            _searchServiceEndpoint = String.Format("https://{0}.{1}", AzureSearchName, _searchClient.SearchDnsSuffix);

            bool result = RunAsync().GetAwaiter().GetResult();
            if (!result)
            {
                Console.WriteLine("Something went wrong.");
            }
            else
            {
                Console.WriteLine("All operations were successful.");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        private static async Task<bool> RunAsync()
        {
            bool result = true;
            result = await GetIndex();
            if (!result)
                return result;
            result = ReplaceAnalyzer();
            if (!result)
                return result;
            result = await DeleteIndexingResources();
            if (!result)
                return result;
            result = await CreateIndex();
            if (!result)
                return result;
            return result;
        }
        private static async Task<bool> DeleteIndexingResources()
        {
            Console.WriteLine("Deleting Index if they exist...");
            try
            {
                await _searchClient.Indexes.DeleteAsync(IndexName);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error deleting resources: {0}", ex.Message);

                return false;
            }
            return true;
        }

        private static async Task<bool> CreateIndex()
        {
            Console.WriteLine("Creating Index...");
            try
            {
                using (StreamReader r = new StreamReader("index.json"))
                {
                    string json = JsonConvert.SerializeObject(NewIndex);
                    string uri = String.Format("{0}/indexes/{1}?api-version=2017-11-11-Preview", _searchServiceEndpoint, IndexName);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PutAsync(uri, content);

                    string responseText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Create Index response: \n{0}", responseText);

                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error creating index: {0}", ex.Message);

                return false;
            }
            return true;
        }
        private static async Task<bool> GetIndex()
        {
            Console.WriteLine("Getting Index...");
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_searchServiceEndpoint}/indexes/{IndexName}?api-version=2017-11-11-Preview");

                string responseText = await response.Content.ReadAsStringAsync();
                NewIndex = JsonConvert.DeserializeObject<Index>(responseText);

                Console.WriteLine("Get Index response: \n{0}", responseText);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}", ex.Message);
                return false;
            }
            return true;
        }
        private static bool ReplaceAnalyzer()
        {
            foreach (var field in NewIndex.Fields)
            {
                if (!(field.Analyzer is null))
                {
                    field.Analyzer = NewAnalyzer;
                }
            }
            return true;
        }
    }
}
