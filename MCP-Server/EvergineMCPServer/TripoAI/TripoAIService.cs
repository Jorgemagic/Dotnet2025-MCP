using Evergine.Framework.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EvergineMCPServer.TripoAI
{
    public class TripoAIService : Service
    {
        public event EventHandler<string> InfoEvent;

        public string api_key;

        public TripoAIService()
        {            
            this.api_key = System.Configuration.ConfigurationManager.AppSettings["tripo-key"];
        }
        
        public async Task<string> RequestADraftModel(string promptText, string negativePrompt = default)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            string taskID = string.Empty;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "text_to_model");
            parameters.Add("model_version", "Turbo-v1.0-20250506");
            parameters.Add("prompt", promptText);
            if (!string.IsNullOrEmpty(negativePrompt))
            {
                parameters.Add("negative_prompt", negativePrompt);
            }

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        taskID = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            }
            ;

            return taskID;
        }

        public async Task<TripoResponse> GetTaskStatus(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            TripoResponse tripoResponse = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                var result = await client.GetAsync($"https://api.tripo3d.ai/v2/openapi/task/{task_id}");

                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                }
            }

            return tripoResponse;
        }        
    }
}
