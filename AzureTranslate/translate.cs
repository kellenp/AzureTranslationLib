// This is a simple namespace you can use to connect and query the microsoft translation service through Azure Marketplace
// 
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Web;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace AzureTranslate {


    public static class Translation {


        //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
        //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
        private static string _azure_client_id = System.Configuration.ConfigurationManager.AppSettings["AzureClientID"];
        public static string azure_client_id {
            get { return _azure_client_id; }
            set { _azure_client_id = value; } 
        }

        private static string _azure_client_secret = System.Configuration.ConfigurationManager.AppSettings["AzureClientSecret"];
        public static string azure_client_secret {
            get { return _azure_client_secret; }
            set { _azure_client_secret = value; } 
        }

        // Generate the header value / auth token we need to provide for access validation to the translation service
        private static string GenerateAuthToken(string client_id = null, string client_secret = null) {

            AdmAccessToken admToken;
            string headerValue;

            // Checks if azure login info is overriden
            client_id = client_id == null ? azure_client_id : client_id;
            client_secret = client_secret == null ? azure_client_secret : client_secret;

            AdmAuthentication admAuth = new AdmAuthentication(client_id, client_secret);
            admToken = admAuth.GetAccessToken();
            DateTime tokenReceived = DateTime.Now;
            // Create a header with the access_token property of the returned token
            headerValue = "Bearer " + admToken.access_token;

            // Return header
            return headerValue;

        }
        
        public static String Execute(String text, String lang, String client_id = null, String client_secret = null) {

            return TranslateMethod(
                GenerateAuthToken(client_id, client_secret),
                text,
                lang
            );

        }

        private static String TranslateMethod(string authToken, string text, string lang) {
            // Add TranslatorService as a service reference, Address:http://api.microsofttranslator.com/V2/Soap.svc
            TranslatorService.LanguageServiceClient client = new TranslatorService.LanguageServiceClient();
            //Set Authorization header before sending the request
            HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
            httpRequestProperty.Method = "POST";
            httpRequestProperty.Headers.Add("Authorization", authToken);

            // Creates a block within which an OperationContext object is in scope.
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel)) {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                //Keep appId parameter blank as we are sending access token in authorization header.
                return client.Translate("", text, "en", lang, "text/html", "general");
            }
        }


        // Translate an array of strings to a given language.
        // Pass an integer by reference (i.e. error_count) to track how many (if any) translations fail.
        // Returns a list of strings in the same order as the original array.
        public static List<string> TranslateArray(string[] texts, string lang, ref int error_count, string client_id = null, string client_secret = null) {

            string authToken = GenerateAuthToken();

            TranslatorService.LanguageServiceClient client = new TranslatorService.LanguageServiceClient();
            //Set Authorization header before sending the request
            HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
            httpRequestProperty.Method = "POST";
            httpRequestProperty.Headers.Add("Authorization", authToken);

            // Define options for all text strings
            TranslatorService.TranslateOptions options = new TranslatorService.TranslateOptions();
            options.ContentType = "text/html";

            // Track output, whether we get a valid translation or fallback to the original string on error.
            List<string> translations = new List<string>();

            // Creates a block within which an OperationContext object is in scope.
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel)) {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                //Keep appId parameter blank as we are sending access token in authorization header.
                //return client.Translate("", text, "en", lang, "text/html", "general");

                TranslatorService.TranslateArrayResponse[] results = client.TranslateArray(
                    "",
                    texts,
                    "en", // from English
                    lang, // to given language
                    options // options for entire array of strings
                );

                // Iterate translation output
                int i;
                for (i = 0; i < results.Length; i += 1)
                {
                    // Convenience
                    TranslatorService.TranslateArrayResponse result = results[i];

                    // Error check
                    if (result.Error != null)
                    {
                        // Increment by-reference error counter
                        error_count += 1;

                        // Fall back to original string
                        translations.Add(texts[i]);
                    }

                    else
                    {
                        // Add translated output
                        translations.Add(result.TranslatedText);
                    }
                }
            }

            // Return translations
            return translations;
        }

    }

    [DataContract]
    public class AdmAccessToken {
        [DataMember]
        public string access_token { get; set; }
        [DataMember]
        public string token_type { get; set; }
        [DataMember]
        public string expires_in { get; set; }
        [DataMember]
        public string scope { get; set; }
    }

    public class AdmAuthentication {
        public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private string clientId;
        private string cientSecret;
        private string request;

        public AdmAuthentication(string clientId, string clientSecret) {
            this.clientId = clientId;
            this.cientSecret = clientSecret;
            //If clientid or client secret has special characters, encode before sending request
            this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
        }

        public AdmAccessToken GetAccessToken() {
            return HttpPost(DatamarketAccessUri, this.request);
        }

        private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails) {
            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
            webRequest.ContentLength = bytes.Length;
            using (Stream outputStream = webRequest.GetRequestStream()) {
                outputStream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse webResponse = webRequest.GetResponse()) {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
                //Get deserialized object from JSON stream
                AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
                return token;
            }
        }
    }
}
