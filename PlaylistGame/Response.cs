﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlaylistGame
{
    public class Response : IResponse
    {
  
        private readonly IDictionary<string, string> _headers;
        private int _statusCode;
        private string _status;
        private string _content;

        public Response()
        {
            _headers = new Dictionary<string, string>();
            _headers.Add("Server", "BIF-SWE1-Server"); //default value if nothing else is set
            _headers.Add("Content-Length", "0"); //default value if no body is set
        }

        Status.Status_Code status;
        Request req;
        string[] additionalHeader;
        string additionalPayload;
        public Response(Request request, Status.Status_Code Status, string[] AdditionalHeader=null, string AdditionalPayload = null) {
            req = request;
            status = Status;
            additionalHeader = AdditionalHeader;
            additionalPayload = AdditionalPayload;
        }
        public IDictionary<string, string> Headers => _headers;

        public int ContentLength
        {
            get => Int32.Parse(_headers["Content-Length"]);
        }

        public string ContentType
        {
            get => _headers.ContainsKey("Content-Type") ? _headers["Content-Type"] : "";
            set
            {
                //check if it already exists first, because we only want to update it then
                if (!_headers.ContainsKey("Content-Type")) _headers.Add("Content-Type", value);
                else _headers["Content-Type"] = value;
            }
        }

        public int StatusCode
        {
            get
            {
                if (_statusCode == 0)
                {
                    throw new Exception("Status code was never set!");
                }

                return _statusCode;
            }
            set
            {
                if (0 <= value && value <= 511)
                {
                    //status code is in valid range
                    _statusCode = value;
                    SetStatusFromCode();
                }
                else
                {
                    throw new Exception($"The provided status code '{value}' is not valid!");
                }


            }
        }

        public string Status
        {
            get
            {
                if (String.IsNullOrEmpty(_status))
                {
                    throw new Exception("No status code set!");
                }

                return _status;
            }
        }

        public void AddHeader(string header, string value)
        {
            if (_headers.ContainsKey(header)) _headers[header] = value;
            else _headers.Add(header, value);
        }

        public string ServerHeader
        {
            get => _headers["Server"];
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    _headers["Server"] = value;
                }
                else
                {
                    throw new Exception("No value when setting the 'Server' header!");
                }
            }
        }

        public void SetContent(string content)
        {
            _content = content;
            _headers["Content-Length"] = $"{_content.Length}";
        }

        public void SetContent(byte[] content)
        {
            _content = Encoding.UTF8.GetString(content);
            _headers["Content-Length"] = $"{_content.Length}";
        }

        public void SetContent(Stream stream)
        {
            var reader = new StreamReader(stream);
            _content = reader.ReadToEnd();
            _headers["Content-Length"] = $"{_content.Length}";
        }

        public void Send(Stream network)
        {
            var writer = new StreamWriter(network);
            var builder = new StringBuilder();

            builder.Append($"HTTP/1.1 {_status}{Environment.NewLine}");
            foreach (var header in _headers)
            {
                builder.Append($"{header.Key}: {header.Value}{Environment.NewLine}");
            }

            builder.Append(Environment.NewLine); //marks beginning of body
            if (!String.IsNullOrEmpty(_content))
            {
                builder.Append(_content);
            }

            writer.Write(builder.ToString());
            writer.Flush();
        }

        private void SetStatusFromCode()
        {
            _status = _statusCode switch
            {
                200 => "200 OK",
                202 => "202 Accepted",
                400 => "400 Bad Request",
                401 => "401 Unauthorized",
                403 => "403 Forbidden",
                404 => "404 Not Found",
                405 => "405 Method Not Allowed",
                406 => "406 Not Acceptable",
                408 => "408 Request Timeout",
                411 => "411 Length Required",
                500 => "500 Internal Server Error",
                501 => "501 Not Implemented",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

    
