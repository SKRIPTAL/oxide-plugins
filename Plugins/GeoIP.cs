using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("GeoIP", "Wulf/lukespragg", "0.1.3", ResourceId = 1365)]
    [Description("Provides an API to obtain IP address information from a local database")]

    class GeoIP : CovalencePlugin
    {
        const string auth = "?client_id=&client_secret="; // For development

        Dictionary<string, BlockEntry> octetBlocks = new Dictionary<string, BlockEntry>();
        JsonSerializerSettings jsonSettings;
        DynamicConfigFile locationConfig;
        LocationData locationData;
        DynamicConfigFile blockConfig;
        BlockData blockData;

        class LocationData
        {
            public string hash { get; set; }
            public List<LocationEntry> locations { get; set; } = new List<LocationEntry>();
        }

        class LocationEntry
        {
            public int geoname_id { get; set; }
            public string locale_code { get; set; }
            public string continent_code { get; set; }
            public string continent_name { get; set; }
            public string country_iso_code { get; set; }
            public string country_name { get; set; }
        }

        class BlockData
        {
            public string hash { get; set; } = string.Empty;
            public List<BlockEntry> ipv4 { get; set; } = new List<BlockEntry>();
        }

        class BlockEntry
        {
            public string network { get; set; }
            public int geoname_id { get; set; }
            //public int registered_country_geoname_id { get; set; }
            //public int represented_country_geoname_id { get; set; }
            //public int is_anonymous_proxy { get; set; }
            //public int is_satellite_provider { get; set; }
        }

        void Init()
        {
            jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new KeyValuesConverter());

            LoadSavedData();
            UpdateLocationData();
            UpdateBlockData();
        }

        void LoadSavedData()
        {
            locationConfig = Interface.Oxide.DataFileSystem.GetFile("GeoIP-Locations");
            locationData = locationConfig.ReadObject<LocationData>();
            blockConfig = Interface.Oxide.DataFileSystem.GetFile("GeoIP-Blocks");
            blockData = blockConfig.ReadObject<BlockData>();
        }

        void UpdateLocationData()
        {
            var check_url = "https://api.github.com/repos/lukespragg/geoip-csv/contents/geoip-locations-en.csv" + auth;
            var download_url = "https://raw.githubusercontent.com/lukespragg/geoip-csv/master/geoip-locations-en.csv";
            var headers = new Dictionary<string, string> {["User-Agent"] = "Oxide-Awesomesauce" };

            webrequest.EnqueueGet(check_url, (api_code, api_response) =>
            {
                if (api_code != 200 || api_response == null)
                {
                    Puts("Checking for locations EN update failed! (" + api_code + ")");
                    #if DEBUG
                    Puts(api_response);
                    #endif
                    return;
                }

                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(api_response, jsonSettings);
                var latest_sha = (string)json["sha"];
                var current_sha = locationData.hash;

                if (latest_sha == current_sha)
                {
                    Puts("Using latest locations EN data, commit: " + current_sha.Substring(0, 7));
                    return;
                }

                Puts("Updating locations EN data to commit " + latest_sha.Substring(0, 7) + "...");
                locationData.hash = latest_sha;

                webrequest.EnqueueGet(download_url, (code, response) =>
                {
                    if (code != 200 || response == null)
                    {
                        timer.Once(30f, UpdateLocationData);
                        return;
                    }

                    locationData.locations = JsonConvert.DeserializeObject<List<LocationEntry>>(CsvToJson(response));
                    locationConfig.WriteObject(locationData);

                    Puts("Locations EN data updated successfully!");
                }, this, null, 300f);
            }, this, headers);
        }

        void CacheBlockData()
        {
            foreach (var block in blockData.ipv4)
            {
                var octets = block.network.Split('.');
                octetBlocks[octets[0] + "." + octets[1]] = block;
            }
        }

        void UpdateBlockData()
        {
            var check_url = "https://api.github.com/repos/lukespragg/geoip-json/git/trees/master" + auth;
            var download_url = "https://raw.githubusercontent.com/lukespragg/geoip-json/master/geoip-blocks-ipv4.csv";
            var headers = new Dictionary<string, string> {["User-Agent"] = "Oxide-Awesomesauce" };

            webrequest.EnqueueGet(check_url, (api_code, api_response) =>
            {
                if (api_code != 200 || api_response == null)
                {
                    Puts("Checking for blocks IPv4 update failed! (" + api_code + ")");
                    #if DEBUG
                    Puts(api_response);
                    #endif
                    return;
                }

                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(api_response, jsonSettings);
                var latest_sha = string.Empty;
                var current_sha = blockData.hash;

                foreach (Dictionary<string, object> file in json["tree"] as List<object>)
                    if ((string)file["path"] == "geoip-blocks-ipv4.csv")
                        latest_sha = file["sha"].ToString();

                if (latest_sha == current_sha)
                {
                    CacheBlockData();
                    Puts("Using latest blocks IPv4 data, commit: " + current_sha.Substring(0, 7));
                    return;
                }

                Puts("Updating blocks IPv4 data to commit " + latest_sha.Substring(0, 7) + "...");
                blockData.hash = latest_sha;

                webrequest.EnqueueGet(download_url, (code, response) =>
                {
                    if (code != 200 || response == null) timer.Once(30f, UpdateBlockData);

                    blockData.ipv4 = JsonConvert.DeserializeObject<List<BlockEntry>>(CsvToJson(response));
                    blockConfig.WriteObject(blockData);
                    CacheBlockData();

                    Puts("Blocks IPv4 data updated successfully!");
                }, this, headers, 300f);
            }, this, headers);
        }

        LocationEntry GetLocationData(string ip)
        {
            var values = ip.Split('.');

            BlockEntry block;
            if (octetBlocks.TryGetValue(values[0] + "." + values[1], out block))
            {
                if (block == null)
                {
                    Puts("Blocks data is not loaded yet!");
                    return null;
                }

                return locationData.locations.FirstOrDefault(location => location.geoname_id == block.geoname_id);
            }
            return null;
        }

        string GetCountry(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                var country_name = location.country_name;
                #if DEBUG
                Puts("Country name for IP " + ip + " is " + country_name);
                #endif
                return country_name;
            }
            return "Unknown";
        }

        string GetCountryCode(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                var country_iso_code = location.country_iso_code;
                #if DEBUG
                Puts("Country code for IP " + ip + " is " + country_iso_code);
                #endif
                return country_iso_code;
            }
            return "Unknown";
        }

        string GetContinent(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                var continent_name = location.continent_name;
                #if DEBUG
                Puts("Continent name for IP " + ip + " is " + continent_name);
                #endif
                return continent_name;
            }
            return "Unknown";
        }

        string GetContinentCode(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                var continent_code = location.continent_code;
                #if DEBUG
                Puts("Continent code for IP " + ip + " is " + continent_code);
                #endif
                return continent_code;
            }
            return "Unknown";
        }

        string GetLocale(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                var locale_code = location.locale_code;
                #if DEBUG
                Puts("Locale for IP " + ip + " is " + locale_code);
                #endif
                return locale_code;
            }
            return "Unknown";
        }

        /// <summary>
        /// Converts a CSV string to a JSON array format.
        /// </summary>
        /// <remarks>First line in CSV must be a header with field name columns.</remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        string CsvToJson(string value)
        {
            // Get lines
            if (value == null) return null;
            var lines = value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) Puts("Must have header line.");

            // Get headers
            var headers = SplitQuotedLine(lines.First(), ',', false);

            // Build JSON array
            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (var i = 1; i < lines.Length; i++)
            {
                var fields = SplitQuotedLine(lines[i], ',', false);
                if (fields.Length != headers.Length) Puts("Field count must match header count.");
                var jsonElements = headers.Zip(fields, (header, field) => $"{header}: {field}").ToArray();
                var jsonObject = "{" + $"{string.Join(",", jsonElements)}" + "}";
                if (i < lines.Length - 1)
                    jsonObject += ",";
                sb.AppendLine(jsonObject);
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        string[] SplitQuotedLine(string value, char separator, bool quotes)
        {
            // Use the "quotes" bool if you need to keep/strip the quotes or something...
            var s = new StringBuilder();
            var regex = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");
            foreach (Match m in regex.Matches(value))
            {
                s.Append(m.Value);
            }
            return s.ToString();
        }
    }
}
