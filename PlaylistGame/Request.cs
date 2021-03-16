using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlaylistGame
{
    public class Request :IRequest
    {
         private readonly string _rawRequest;
        private string _method;
        private IUrl _url;
        private string _protocol;
        private IDictionary<string, string> _headers;
        private Stream _contentStream;
        private string _contentString;
        private byte[] _contentBytes;
        public string ctype;
        public string payload;
        public object loc;
        public string Body;
        public string token;
        public const string MethodPost = "POST";
        public const string MethodGet = "GET";
        public const string MethodPut = "PUT";
        public const string MethodDelete = "DELETE";

        public Request(string rawRequest)
        {
            _rawRequest = rawRequest;
            SplitRawRequest();
        }

        public bool IsValid
        {
            get
            {
                if (_method.Length != 0 && _url != null)
                {
                    return true;
                }
                return false;
            }
        }

        public string Method => _method;

        public IUrl Url => _url;

        public string Protocol => _protocol;

        public IDictionary<string, string> Headers => _headers;
        
        public string UserAgent
        {
            get
            {
                if (_headers.ContainsKey("user-agent"))
                {
                    return _headers["user-agent"];
                }
                return "";
            }
        }

        public string UserAuthorization
        {
            get
            {
                if (_headers.ContainsKey("authorization"))
                {
                    return _headers["authorization"];
                }
                return "";
            }
            
        }

        public int HeaderCount => Headers.Count;

        public int ContentLength
        {
            get
            {
                if (_headers.ContainsKey("content-length"))
                {
                    return Int32.Parse(_headers["content-length"]);
                }

                return 0;
            }
        }

        public string ContentType
        {
            get
            {
                if (_headers.ContainsKey("content-type"))
                    return _headers["content-type"];

                return "";
            }
        }

        public Stream ContentStream => _contentStream;

        public string ContentString => _contentString;

        public byte[] ContentBytes => _contentBytes;

        private void SplitRawRequest()
        {
            var requestLines = _rawRequest.Split(Environment.NewLine); //in this case we need to keep empty lines to know where the body starts
            var bodyStartIndex = Array.IndexOf(requestLines, String.Empty); //IndexOf returns first appearance --> first empty line marks start of body
            var requestStart = requestLines[0];
            var headerLines = new string[bodyStartIndex - 1];
            var bodyLines = new string[requestLines.Length - bodyStartIndex - 1];

            //copies the header part into the header array, and the body part in the body array skipping the empty line between
            Array.Copy(requestLines, 1, headerLines, 0, bodyStartIndex - 1);
            Array.Copy(requestLines, bodyStartIndex + 1, bodyLines, 0, requestLines.Length - bodyStartIndex - 1);
            
            //extract method and url form the headers array (index 0)
            var methodAndUrl = requestStart.Split(" ", 3);
            if (methodAndUrl.Length < 3) throw new InvalidDataException("Method/Url Could not be Parsed!"); //something went wrong, request cannot be used

            //store in the respective variables
            string[] validMethods = {"GET", "POST", "PUT", "PATCH", "DELETE"}; //defines which methods are valid

            _method = ((IList) validMethods).Contains(methodAndUrl[0]) ? methodAndUrl[0] : "";
            _url = new Url(methodAndUrl[1]);
            _protocol = methodAndUrl[2];
            _headers = ExtractHeaders(headerLines);

            if (!bodyLines[0].Equals(String.Empty)) //when no body is provided, the bodyLines array contains 1 element with an empty string
            {
                //get string
                _contentString = String.Join(Environment.NewLine, bodyLines);
            
                //get byte array from string:
                _contentBytes = Encoding.UTF8.GetBytes(_contentString);
            
                //get stream from byte array: 
                _contentStream = new MemoryStream(_contentBytes);
            }
        }

        private IDictionary<string, string> ExtractHeaders(string[] headers)
        {
            IDictionary<string, string> headerDict = new Dictionary<string,string>();
            
            foreach(var header in headers)
            {
                var keyValue = header.Split(": ");
                headerDict.Add(keyValue[0].ToLower(), keyValue[1]);
            }
            return headerDict; //if the input array is empty, we return an empty dictionary, which is okay
        }
    }

    
}