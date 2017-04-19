using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BotFrameworkDemo
{
    public class CodeCamp
    {

        static CodeCamp()
        {
            Sessions = new List<Session>();
            Speakers = new List<Speaker>();

            string json = null;
            var confScheduleUrl = ConfigurationManager.AppSettings["ConferenceScheduleUrl"];

            if (string.IsNullOrEmpty(confScheduleUrl))
            {
                json = File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/codecamp.json"));
            }
            else
            {
                using (var client = new HttpClient())
                {
                    var uri = new Uri(confScheduleUrl);
                    var response = Task.Run(() => client.GetAsync(uri)).Result;
                    json = Task.Run(() => response.Content.ReadAsStringAsync()).Result;
                }
            }
            
            var jobj = JObject.Parse(json);

            Info = JsonConvert.DeserializeObject<ConferenceInfo>(json);

            foreach (var sessionItem in jobj["schedules"][0]["sessions"])
                Sessions.Add(JsonConvert.DeserializeObject<Session>(sessionItem.ToString()));
            foreach (var speakerItem in jobj["speakers"])
                Speakers.Add(JsonConvert.DeserializeObject<Speaker>(speakerItem.ToString()));

            json = File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/topics.json"));
            Topics = JsonConvert.DeserializeObject<List<Topic>>(json);

            // add id for each session - Title for now
            foreach (var session in Sessions)
                session.Id = session.Title;
        }

        public static ConferenceInfo Info { get; }

        public static List<Session> Sessions { get; }

        public static List<Speaker> Speakers { get; }

        public static List<Topic> Topics { get; }

        public static IEnumerable<Session> FindSessions(string[] speakerNames = null, string[] topics = null, string companyName = null, LevelTypes level = LevelTypes.Any)
        {
            var localSpeakerNames = speakerNames;
            if (!String.IsNullOrWhiteSpace(companyName))
            {
                localSpeakerNames = Speakers.Where(k => k.Company.ContainsIgnoreCase(companyName)).Select(k => k.Name).ToArray();
            }
            var topicsRegex = topics?.GetWholeWordsRegex();

            return CodeCamp.Sessions
                .Where(s =>
                    localSpeakerNames == null 
                    || localSpeakerNames.Length == 0
                    || s.Speakers.ContainIgnoreCase(localSpeakerNames))

                .Where(s =>
                    topics == null 
                    || (topics.ContainIgnoreCase("misc") && s.AllTracks == true) 
                    || s.Title.RegexMatch(topicsRegex) 
                    || s.Description.RegexMatch(topicsRegex))

                .Where(s => 
                    level == LevelTypes.None 
                    || level == LevelTypes.Any 
                    || s.Level.ContainsIgnoreCase(level.ToString()))

                .OrderBy(s => s.StartTime)
                .Take(10);
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

        [JsonIgnore]
        public string Id { get; set; }

        public string ToShortDisplayString()
        {
            return String.Format("{0}{1}{2} ({3}{4}{5})",
                Title.HtmlDecode().Bold(),
                Speakers?.Length > 0 ? " -- " : String.Empty,
                Speakers.ToCsvString(),
                Track.Accent(),
                Track != null ? "," : String.Empty,
                StartTime.ToString("HH:mm").Accent()
                );
        }

        public string ToLongDisplayString()
        {
            return String.Format(@"**Title**: {0}

**Speakers**: {1}

**Venue**: {2}, {3} - {4}

**Description**: {5}
",
                Title.HtmlDecode(),
                Speakers.ToCsvString(),
                Track,
                StartTime.ToString("HH:mm"),
                EndTime.ToString("HH:mm"),
                Description
                );
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

    [Serializable]
    public class Topic
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("terms")]
        public string[] Terms;
    }

    [Serializable]
    public enum LevelTypes
    {
        None,
        Any,
        Beginner,
        Intermediate,
        Experienced,
        Advanced
    }

    [Serializable]
    public class ConferenceInfo
    {
        [JsonProperty("refid")]
        public int ConferenceId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("venue")]
        public ConferenceVenue Venue { get; set; }

        public string ToShortDisplayString()
        {
            return String.Format("**{0}**, happening {1} at {2}",
                Title,
                StartDate.Date == EndDate.Date ?
                    String.Format("on **{0}**", StartDate.Date.ToLongDateString()) :
                    String.Format("between **{0}** and **{1}**", StartDate.Date.ToLongDateString(), EndDate.Date.ToLongDateString()),
                Venue?.Name
                );
        }
    }

    [Serializable]
    public class ConferenceVenue
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    public static class StringExtensionMethods
    {
        public static bool ContainsIgnoreCase(this string containerString, string str)
        {
            return containerString != null && str != null &&
                containerString.ToLowerInvariant().Contains(str.ToLowerInvariant());
        }

        public static bool ContainsIgnoreCase(this string containerString, params string[] str)
        {
            if (containerString == null || str == null)
                return false;
            foreach (var s in str)
            {
                if (containerString.ContainsIgnoreCase(s))
                    return true;
            }
            return false;
        }

        public static bool RegexMatch(this string containerString, string regex)
        {
            if (containerString == null || regex == null)
                return false;

            return Regex.IsMatch(containerString, regex, RegexOptions.IgnoreCase);
        }

        public static bool ContainIgnoreCase(this IEnumerable<string> containerStrings, string str)
        {
            return containerStrings != null && str != null &&
                containerStrings.Where(s => s.ContainsIgnoreCase(str)).FirstOrDefault() != null;
        }

        public static bool ContainIgnoreCase(this IEnumerable<string> containerStrings, params string[] str)
        {
            if (containerStrings == null || str == null)
                return false;
            foreach (var s in str)
            {
                if (containerStrings.ContainIgnoreCase(s))
                    return true;
            }
            return false;
        }

        public static string ToCsvString(this IEnumerable<string> strings)
        {
            if (strings == null)
                return null;
         
            var buf = new StringBuilder();
            foreach (var s in strings)
                buf.AppendFormat("{0}{1}", buf.Length == 0 ? String.Empty : ", ", s);

            return buf.ToString();
        }

        public static string HtmlDecode(this string str)
        {
            return str?.Replace("&amp;", "&");
        }

        public static string Bold(this string str)
        {
            return str != null ? $"**{str}**" : null;
        }

        public static string Accent(this string str)
        {
            return str != null ? $"*{str}*" : null;
        }

        public static string GetWholeWordsRegex(this string[] str)
        {
            if (str == null || str.Length == 0)
                return String.Empty;

            StringBuilder buf = new StringBuilder();
            foreach (var s in str)
                buf.AppendFormat("{0}{1}", buf.Length == 0 ? String.Empty : "|", s);

            return @"\b(" + buf.ToString() + @")\b";
        }

    }
}