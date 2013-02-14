// This is a simple namespace you can use to connect and query the microsoft translation service through Azure Marketplace
// 
using System;
using System.Collections.Generic;
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

    public class Translation {

        public static String Execute(String text, String lang) {

            AdmAccessToken admToken;
            string headerValue;
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 

            //Insert your Client ID and Client Secret below
            AdmAuthentication admAuth = new AdmAuthentication("ClientID", "ClientSecret");
            admToken = admAuth.GetAccessToken();
            DateTime tokenReceived = DateTime.Now;
            // Create a header with the access_token property of the returned token
            headerValue = "Bearer " + admToken.access_token;
            return TranslateMethod(headerValue, text, lang);
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
