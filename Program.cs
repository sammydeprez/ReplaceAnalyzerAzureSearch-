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
        private const string AzureSearchName = "<<AzureSearchName>>";
        private const string AzureSearchApiKey = "<<AzureSearchApiKey>>";
        private const string IndexName = "<<AzureSearchIndex>>";
        private const string NewAnalyzer = "<<Analyzer>>"; //Ex. nl.microsoft


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
