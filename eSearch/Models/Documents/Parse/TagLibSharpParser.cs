using eSearch.Interop;
using J2N;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TagLib;
using TagLib.IFD;
using TagLib.Image;
using TagLib.Xmp;

namespace eSearch.Models.Documents.Parse
{
    public class TagLibSharpParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { 
                    "mkv", "ogv", "avi", "wmv", "asf", "mp4", "m4v", "mpeg", "mpg", "mpe", "mpv", "mpg", "m2v",  // Video
                    "aa", "aax", "aac", "aiff", "ape", "dsf", "flac", "m4a", "m4b", "m4p", "mp3", "mpc", "mpp", "ogg", "oga", "wav", "wma", "wv", "webm", // Audio
                    "bmp", "gif", "jpeg", "jpg", "pbm", "pgm", "ppm", "pnm", "pcx", "png", "tiff", "tif", "dng", "svg" // Image
                }; 
            }
        }

        private string[] VideoExtensions
        {
            get { return new string[] {"mkv", "ogv", "avi", "wmv", "asf", "mp4", "m4v", "mpeg", "mpg", "mpe", "mpv", "mpg", "m2v", }; } // Video 
        }

        private string[] AudioExtensions
        {
            get
            {
                return new string[] { "aa", "aax", "aac", "aiff", "ape", "dsf", "flac", "m4a", "m4b", "m4p", "mp3", "mpc", "mpp", "ogg", "oga", "wav", "wma", "wv", "webm" };
            }
        }

        private string[] ImageExtensions
        {
            get
            {
                return new string[] { "bmp", "gif", "jpeg", "jpg", "pbm", "pgm", "ppm", "pnm", "pcx", "png", "tiff", "dng", "svg" };
            }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            string extension = Path.GetExtension(filePath).ToLower()
                .Substring(1); // Remove the period.
            var tFile = TagLib.File.Create(filePath);

            var title = Path.GetFileNameWithoutExtension(filePath);

            List<IMetaData> ParsedMetadata = new List<IMetaData>();

            var metadataDict = ParseToKeyValuesRecursive("", tFile.Tag);
            foreach(var item in metadataDict)
            {
                string key = item.Key;
                string value = item.Value;
                if (key == "Title") // Do not add title to metadata list. Instead just populate the title field.
                {
                    if (value.ToLower() != "untitled")
                    {
                        title = value;
                    }
                } else
                {
                    ParsedMetadata.Add(new Metadata { Key = key, Value = value });
                }
            }

            if (ImageDimensionsUtils.TryGetImageDimensions(filePath, extension, out var dimensions))
            {
                ParsedMetadata.Add(new Metadata { Key = "Width", Value = dimensions.Item1 + "" });
                ParsedMetadata.Add(new Metadata { Key = "Height", Value = dimensions.Item2 + "" });
            }

            parseResult = new ParseResult
            {
                Title = title,
                Metadata = ParsedMetadata,
                ParserName = "Media Parser (TagLibSharp)",
                TextContent = ""
            };
        }

        /// <summary>
        /// Method to extract metadata out of a TagLib.Tag to a Dictionary.
        /// </summary>
        /// <param name="propertyName">Pass an empty string for the root object, used in recursion only. root object should be TagLib.Tag</param>
        /// <param name="value"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private Dictionary<string, string> ParseToKeyValuesRecursive(string propertyName, object value, Dictionary<string, string> dict = null)
        {
            if (dict == null)  dict = new Dictionary<string, string>();
            if (value == null) return dict;
            // Skip tags like 'JoinedArtists' and 'FirstArtist' etc - Data duplication
            if (propertyName.StartsWith("Joined")) return dict;
            if (propertyName.StartsWith("First")) return dict; 

            switch(value)
            {
                case UInt32 uint32:
                    if (uint32 != 0) dict.Add(propertyName, uint32.ToString());
                    return dict;
                case Boolean booll:
                    dict.Add(propertyName, booll.ToString());
                    return dict;
                case ImageOrientation orientation:
                    string strOrientation = orientation.ToString();
                    dict.Add(propertyName, strOrientation);
                    return dict;
                case DateTime dateTime:
                    string strDate = dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();
                    dict.Add(propertyName, strDate.ToString());
                    return dict;
                case IFDStructure iFDStructure:
                    return dict; // TODO - What's this?
                case IPicture[] iPictures:
                    if (iPictures.Length > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(iPictures[0].Description))
                        {
                            dict.Add("Description", iPictures[0].Description);
                        }
                    }
                    return dict;
                case int intValue:
                    if (intValue != 0) dict.Add(propertyName, intValue.ToString());
                    return dict;
                case TagTypes tagTypes:
                    return dict; // Ignore this
                case double doubleValue:
                    if (doubleValue == 0)       return dict;
                    if (doubleValue.IsNaN())    return dict;
                    dict.Add(propertyName, doubleValue.ToString());
                    return dict;

                case string[] strArray:
                    var str = string.Join(", ", strArray);
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        dict.Add(propertyName, str);
                    }
                    return dict;

                case string strVal:
                    if (!string.IsNullOrWhiteSpace(strVal))
                    {
                        dict.Add(propertyName, strVal.Trim());
                    }
                    return dict;

                case TagLib.Tag tag:
                    foreach (var property in tag.GetType().GetProperties())
                    {
                        var    subPropertyName  = property.Name;
                        object subPropertyValue = property.GetValue(tag, null);
                        dict = ParseToKeyValuesRecursive(string.Join("/", new string[] { propertyName, subPropertyName }).TrimStart('/'), subPropertyValue, dict);
                    }
                    return dict;
                case XmpNode xmpNode:
                    return dict; // TODO Unsupported.
                case Tag[] tags:
                    foreach (var tag in tags)
                    {
                        dict = ParseToKeyValuesRecursive(propertyName, tag, dict);
                    }
                    return dict;

                default:
                    // Unrecognized. 
                    var type = value.GetType();

                    if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        // Generic list handling.
                        if (value is IList list)
                        {
                            if (list.Count > 0)
                            {
                                
                                string csv = String.Join(", ", list);
                                dict.Add(propertyName, csv);
                            }
                        }
                        return dict;
                    }
#if DEBUG
                    Debug.WriteLine("Unsupported type in TagLibSharp: " + value.GetType().Name + " propertyName " + propertyName);
#endif
                    return dict;
            }
        }

        private object ToReadableValue(object value)
        {
            if (value is string[] strings)
            {
                return string.Join(", ", strings);
            }
            return value; // Fall back to the raw object.
        }
    }
}
