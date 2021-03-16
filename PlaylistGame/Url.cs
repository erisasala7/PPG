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
        private string _rawUrl;
        private string _path;
        private IDictionary<string, string> _parameter;
        private string[] _segments;
        private string _fragment;

        public Url(string rawUrl)
        {
            this._rawUrl = rawUrl;
            this._parameter = new Dictionary<string, string>();
            this.SplitUrl(); //instantly gets parameters and path
        }

        public string RawUrl
        {
            get => _rawUrl;
        }

        public string Path
        {
            get => _path;
        }

        public IDictionary<string, string> Parameter
        {
            get => _parameter;
        }

        public int ParameterCount => _parameter.Count;

        public string[] Segments
        {
            get
            {
                if (_segments != null)
                {
                    return _segments;
                }

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
                {
                    return _segments.Last();
                }
                else return "";
            }
        }

        public string Extension
        {
            get
            {
                if (!String.IsNullOrEmpty(this.FileName))
                {
                    var parts = this.FileName.Split(".");
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
                if (!String.IsNullOrEmpty(_fragment))
                    return _fragment;
                return "";
            }
        }

        private void SplitUrl()
        {
            //check for fragment first, because everything after the first # is a fragment
            if (_rawUrl.Contains('#'))
            {
                var parts = _rawUrl.Split("#", 2, StringSplitOptions.RemoveEmptyEntries);
                _rawUrl = parts[0]; //everything before the # belongs to the url
                _fragment = parts[1]; //everytihng after the # belongs to the fragment
            }

            //a valid url can be split into two parts
            var extracted = _rawUrl.Split("?", 2, StringSplitOptions.RemoveEmptyEntries);
            if (!String.IsNullOrEmpty(extracted[0]))
            {
                //set the path
                var segmentsRaw = new ArrayList();
                foreach (var segment in extracted[0].Split("/", StringSplitOptions.RemoveEmptyEntries))
                {
                    segmentsRaw.Add(segment); //adds everything to the arraylist
                }

                var sb = new StringBuilder();
                if (segmentsRaw.Count > 0)
                {
                    foreach (string rawSegment in segmentsRaw.ToArray())
                    {
                        sb.Append("/");
                        sb.Append(rawSegment);
                    }
                }
                else sb.Append("/"); //no segments means root was requested


                _path = sb.ToString();
                _segments = sb.ToString().Split("/", StringSplitOptions.RemoveEmptyEntries);
            }

            if (extracted.Length > 1 && !String.IsNullOrEmpty(extracted[1]))
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
                    _parameter.Add(keyValue[0], keyValue[1]);
                }
            }
        }
    }
}
