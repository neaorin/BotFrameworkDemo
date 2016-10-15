using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace BotFrameworkDemo
{
    public class CodeCamp
    {
        static CodeCamp()
        {
            Sessions = new List<Session>();
            Speakers = new List<Speaker>();

            var json = File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/codecamp.json"));
            var jobj = JObject.Parse(json);

            foreach (var sessionItem in jobj["schedules"][0]["sessions"])
                Sessions.Add(JsonConvert.DeserializeObject<Session>(sessionItem.ToString()));
            foreach (var speakerItem in jobj["speakers"])
                Speakers.Add(JsonConvert.DeserializeObject<Speaker>(speakerItem.ToString()));

        }

        public static List<Session> Sessions { get; }

        public static List<Speaker> Speakers { get; }

        public static IEnumerable<Session> FindSessions(string speakerName = null, string topic = null)
        {
            return CodeCamp.Sessions
                .Where(s => String.IsNullOrEmpty(speakerName) || s.Speakers.ContainIgnoreCase(speakerName))
                .Where(s => String.IsNullOrEmpty(topic) || s.Title.ContainsIgnoreCase(topic) || s.Description.ContainsIgnoreCase(topic));
        }

        public static IEnumerable<Speaker> FindSpeakers(string speakerName)
        {
            return CodeCamp.Speakers.Where(s => s.Name.ContainsIgnoreCase(speakerName));
        }
    }

    [Serializable]
    public class Session
    {

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("speakers")]
        public string[] Speakers { get; set; }


        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
        [JsonProperty("allTracks")]
        public bool AllTracks { get; set; }
        [JsonProperty("speakingLang")]
        public string SpeakingLang { get; set; }
        [JsonProperty("level")]
        public string Level { get; set; }
        [JsonProperty("track")]
        public string Track { get; set; }

        public override string ToString()
        {
            return $"{Speakers[0]} -- {Title}";
        }
    }

    [Serializable]
    public class Speaker
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("companyWebsiteUrl")]
        public string CompanyWebsiteUrl { get; set; }

        [JsonProperty("bio")]
        public string Bio { get; set; }

        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; }

        [JsonProperty("photoUrl")]
        public string PhotoUrl { get; set; }

    }

    public static class StringExtensionMethods
    {
        public static bool ContainsIgnoreCase(this string containerString, string str)
        {
            return containerString != null && str != null &&
                containerString.ToLowerInvariant().Contains(str.ToLowerInvariant());
        }

        public static bool ContainIgnoreCase(this IEnumerable<string> containerStrings, string str)
        {
            return containerStrings != null && str != null &&
                containerStrings.Where(s => s.ContainsIgnoreCase(str)).FirstOrDefault() != null;
        }
    }
}