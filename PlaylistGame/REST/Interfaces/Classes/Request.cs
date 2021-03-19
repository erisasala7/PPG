using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlaylistGame
{
    public class Request : IRequest
    {
        public const string MethodPost = "POST";
        public const string MethodGet = "GET";
        public const string MethodPut = "PUT";
        public const string MethodDelete = "DELETE";
        private readonly string _rawRequest;
        public string Body;
        public string ctype;
        public object loc;
        public string payload;
        public string token;

        public Request(string rawRequest)
        {
            _rawRequest = rawRequest;
            SplitRawRequest();
        }

        public string Protocol { get; private set; }

        public string UserAuthorization
        {
            get
            {
                if (Headers.ContainsKey("authorization")) return Headers["authorization"];
                return "";
            }
        }

        public bool IsValid
        {
            get
            {
                if (Method.Length != 0 && Url != null) return true;
                return false;
            }
        }

        public string Method { get; private set; }

        public IUrl Url { get; private set; }

        public IDictionary<string, string> Headers { get; private set; }

        public string UserAgent
        {
            get
            {
                if (Headers.ContainsKey("user-agent")) return Headers["user-agent"];
                return "";
            }
        }

        public int HeaderCount => Headers.Count;

        public int ContentLength
        {
            get
            {
                if (Headers.ContainsKey("content-length")) return int.Parse(Headers["content-length"]);

                return 0;
            }
        }

        public string ContentType
        {
            get
            {
                if (Headers.ContainsKey("content-type"))
                    return Headers["content-type"];

                return "";
            }
        }

        public Stream ContentStream { get; private set; }

        public string ContentString { get; private set; }

        public byte[] ContentBytes { get; private set; }

        private void SplitRawRequest()
        {
            var requestLines =
                _rawRequest.Split(Environment
                    .NewLine); //in this case we need to keep empty lines to know where the body starts
            var bodyStartIndex =
                Array.IndexOf(requestLines,
                    string.Empty); //IndexOf returns first appearance --> first empty line marks start of body
            var requestStart = requestLines[0];
            var headerLines = new string[bodyStartIndex - 1];
            var bodyLines = new string[requestLines.Length - bodyStartIndex - 1];

            //copies the header part into the header array, and the body part in the body array skipping the empty line between
            Array.Copy(requestLines, 1, headerLines, 0, bodyStartIndex - 1);
            Array.Copy(requestLines, bodyStartIndex + 1, bodyLines, 0, requestLines.Length - bodyStartIndex - 1);

            //extract method and url form the headers array (index 0)
            var methodAndUrl = requestStart.Split(" ", 3);
            if (methodAndUrl.Length < 3)
                throw new InvalidDataException(
                    "Method/Url Could not be Parsed!"); //something went wrong, request cannot be used

            //store in the respective variables
            string[] validMethods = {"GET", "POST", "PUT", "PATCH", "DELETE"}; //defines which methods are valid

            Method = ((IList) validMethods).Contains(methodAndUrl[0]) ? methodAndUrl[0] : "";
            Url = new Url(methodAndUrl[1]);
            Protocol = methodAndUrl[2];
            Headers = ExtractHeaders(headerLines);

            if (!bodyLines[0].Equals(string.Empty)
            ) //when no body is provided, the bodyLines array contains 1 element with an empty string
            {
                //get string
                ContentString = string.Join(Environment.NewLine, bodyLines);

                //get byte array from string:
                ContentBytes = Encoding.UTF8.GetBytes(ContentString);

                //get stream from byte array: 
                ContentStream = new MemoryStream(ContentBytes);
            }
        }

        private IDictionary<string, string> ExtractHeaders(string[] headers)
        {
            IDictionary<string, string> headerDict = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                var keyValue = header.Split(": ");
                headerDict.Add(keyValue[0].ToLower(), keyValue[1]);
            }

            return headerDict; //if the input array is empty, we return an empty dictionary, which is okay
        }
    }
}