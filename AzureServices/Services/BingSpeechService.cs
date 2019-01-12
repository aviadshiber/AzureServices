using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using AzureServices.Utils;
using System;

namespace AzureServices {
    public class BingSpeechService : IBingSpeechService
    {
        IAuthenticationService authenticationService;
        readonly string operatingSystem;
        HttpClient httpClient;
        readonly string speechRecognitionEndPoint;
        string RecognitionMode { get; set; }
        string Language { get; set; }
        /// <summary>
        /// Creates a bing speech service to transfrom speech to text.
        /// </summary>
        /// <param name="authService">authentication Service</param>
        /// <param name="os">platform to run on</param>
        /// <param name="speechRecognitionEndPoint">the end point service</param>
        /// <param name="recognitionMode">Recognition mode can be interactive,conversation,dictation see https://docs.microsoft.com/en-us/azure/cognitive-services/speech/getstarted/getstartedrest for more info</param>
        /// <param name="language">supported languages https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/supportedlanguages </param>
        public BingSpeechService(IAuthenticationService authService, string os, string speechRecognitionEndPoint= "https://westus.stt.speech.microsoft.com/speech/recognition/",string recognitionMode= "dictation",string language= "en-us")
        {
            this.speechRecognitionEndPoint = speechRecognitionEndPoint;
            authenticationService = authService;
            operatingSystem = os;
            RecognitionMode = recognitionMode;
            Language = language;
        }

        public async Task<SpeechResult> RecognizeSpeechAsync(string filename)
        {
            if (string.IsNullOrWhiteSpace(authenticationService.GetAccessToken()))
            {
                System.Diagnostics.Debug.WriteLine("Got access token trying to init authentication service");
                await authenticationService.InitializeAsync();
            }

            // Read audio file to a stream
            var file = await PCLStorage.FileSystem.Current.LocalStorage.GetFileAsync(filename);
            var fileStream = await file.OpenAsync(PCLStorage.FileAccess.Read);

            // Send audio stream to Bing and deserialize the response
            string requestUri = GenerateRequestUri(speechRecognitionEndPoint);
            string accessToken = authenticationService.GetAccessToken();
            var response = await SendRequestAsync(fileStream, requestUri, accessToken, Constants.AudioContentType);
            System.Diagnostics.Debug.WriteLine($"json repose:{response}");
            var speechResult = JsonConvert.DeserializeObject<SpeechResult>(response);
            try {
                fileStream.Dispose();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return speechResult;
        }

        string GenerateRequestUri(string speechEndpoint)
        {
            // build a request URL, more info:
            // https://docs.microsoft.com/en-us/azure/cognitive-services/speech/getstarted/getstartedrest
            string requestUri = speechEndpoint;
            requestUri += RecognitionMode+"/cognitiveservices/v1?";
            requestUri += @"language="+Language;
            requestUri += @"&format=simple";
            System.Diagnostics.Debug.WriteLine($"Uri generated {requestUri.ToString()}");
            return requestUri;
        }

        async Task<string> SendRequestAsync(Stream fileStream, string url, string bearerToken, string contentType)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var content = new StreamContent(fileStream);
            content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            System.Diagnostics.Debug.WriteLine($"Sending POST request to {url}");
            var response = await httpClient.PostAsync(url, content);
            System.Diagnostics.Debug.WriteLine("Response status code:"+response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
