using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlaylistGame
{
    public class Url : IUrl
    {
        private string _fragment;
        private string[] _segments;

        public Url(string rawUrl)
        {
            RawUrl = rawUrl;
            Parameter = new Dictionary<string, string>();
            SplitUrl(); //instantly gets parameters and path
        }

        public string RawUrl { get; private set; }

        public string Path { get; private set; }

        public IDictionary<string, string> Parameter { get; }

        public int ParameterCount => Parameter.Count;

        public string[] Segments
        {
            get
            {
                if (_segments != null) return _segments;

                return new string[] { }; //return empty array
            }
        }

        public string FileName
        {
            get
            {
                var fileStringRegEx = @"^\w*\.\w*$";
                var fileRegex = new Regex(fileStringRegEx);
                if (fileRegex.IsMatch(_segments.Last()))
                    return _segments.Last();
                return "";
            }
        }

        public string Extension
        {
            get
            {
                if (!string.IsNullOrEmpty(FileName))
                {
                    var parts = FileName.Split(".");
                    var sb = new StringBuilder("."); //dot needs to be included
                    sb.Append(parts[
                        1]); //we only want the part behind the dot (which is always there in a valid filename)
                    return sb.ToString();
                }

                return ""; //return empty string if no filename in the url
            }
        }

        public string Fragment
        {
            get
            {
                if (!string.IsNullOrEmpty(_fragment))
                    return _fragment;
                return "";
            }
        }

        private void SplitUrl()
        {
            //check for fragment first, because everything after the first # is a fragment
            if (RawUrl.Contains('#'))
            {
                var parts = RawUrl.Split("#", 2, StringSplitOptions.RemoveEmptyEntries);
                RawUrl = parts[0]; //everything before the # belongs to the url
                _fragment = parts[1]; //everytihng after the # belongs to the fragment
            }

            //a valid url can be split into two parts
            var extracted = RawUrl.Split("?", 2, StringSplitOptions.RemoveEmptyEntries);
            if (!string.IsNullOrEmpty(extracted[0]))
            {
                //set the path
                var segmentsRaw = new ArrayList();
                foreach (var segment in extracted[0].Split("/", StringSplitOptions.RemoveEmptyEntries))
                    segmentsRaw.Add(segment); //adds everything to the arraylist

                var sb = new StringBuilder();
                if (segmentsRaw.Count > 0)
                    foreach (string rawSegment in segmentsRaw.ToArray())
                    {
                        sb.Append("/");
                        sb.Append(rawSegment);
                    }
                else sb.Append("/"); //no segments means root was requested


                Path = sb.ToString();
                _segments = sb.ToString().Split("/", StringSplitOptions.RemoveEmptyEntries);
            }

            if (extracted.Length > 1 && !string.IsNullOrEmpty(extracted[1]))
            {
                //these should be the parameters, which are separated by "&"
                var keyValuePairs = extracted[1].Split("&", StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in keyValuePairs)
                {
                    //name=flo
                    //age=20
                    //the key value pairs are seperated by "="
                    var keyValue =
                        pair.Split("=", 2, StringSplitOptions.RemoveEmptyEntries); //should only be 2 items
                    Parameter.Add(keyValue[0], keyValue[1]);
                }
            }
        }
    }
}