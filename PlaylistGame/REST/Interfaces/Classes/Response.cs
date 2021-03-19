using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlaylistGame
{
    public class Response : IResponse
    {
        private string _content;
        private string _status;
        private int _statusCode;
        private string[] additionalHeader;
        private string additionalPayload;
        private Request req;

        private Status.Status_Code status;

        public Response()
        {
            Headers = new Dictionary<string, string>();
            Headers.Add("Server", "BIF-SWE1-Server"); //default value if nothing else is set
            Headers.Add("Content-Length", "0"); //default value if no body is set
        }

        public Response(Request request, Status.Status_Code Status, string[] AdditionalHeader = null,
            string AdditionalPayload = null)
        {
            req = request;
            status = Status;
            additionalHeader = AdditionalHeader;
            additionalPayload = AdditionalPayload;
        }

        public IDictionary<string, string> Headers { get; }

        public int ContentLength => int.Parse(Headers["Content-Length"]);

        public string ContentType
        {
            get => Headers.ContainsKey("Content-Type") ? Headers["Content-Type"] : "";
            set
            {
                //check if it already exists first, because we only want to update it then
                if (!Headers.ContainsKey("Content-Type")) Headers.Add("Content-Type", value);
                else Headers["Content-Type"] = value;
            }
        }

        public int StatusCode
        {
            get
            {
                if (_statusCode == 0) throw new Exception("Status code was never set!");

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
                if (string.IsNullOrEmpty(_status)) throw new Exception("No status code set!");

                return _status;
            }
        }

        public void AddHeader(string header, string value)
        {
            if (Headers.ContainsKey(header)) Headers[header] = value;
            else Headers.Add(header, value);
        }

        public string ServerHeader
        {
            get => Headers["Server"];
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Headers["Server"] = value;
                else
                    throw new Exception("No value when setting the 'Server' header!");
            }
        }

        public void SetContent(string content)
        {
            _content = content;
            Headers["Content-Length"] = $"{_content.Length}";
        }

        public void SetContent(byte[] content)
        {
            _content = Encoding.UTF8.GetString(content);
            Headers["Content-Length"] = $"{_content.Length}";
        }

        public void SetContent(Stream stream)
        {
            var reader = new StreamReader(stream);
            _content = reader.ReadToEnd();
            Headers["Content-Length"] = $"{_content.Length}";
        }

        public void Send(Stream network)
        {
            var writer = new StreamWriter(network);
            var builder = new StringBuilder();

            builder.Append($"HTTP/1.1 {_status}{Environment.NewLine}");
            foreach (var header in Headers) builder.Append($"{header.Key}: {header.Value}{Environment.NewLine}");

            builder.Append(Environment.NewLine); //marks beginning of body
            if (!string.IsNullOrEmpty(_content)) builder.Append(_content);

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