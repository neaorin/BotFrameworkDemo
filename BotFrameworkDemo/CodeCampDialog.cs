using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BotFrameworkDemo
{
    [LuisModel("542001cd-7816-4c42-a304-d604bc945384", "905a0f41c2334e6992b5ca1e8bce6375")]
    [Serializable]
    public class CodeCampDialog : LuisDialog<string>
    {
        [LuisIntent("None")]
        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"I am sorry, I did not understand you. Why don't you try typing **help** or **find a session**?";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("FindSession")]
        public async Task FindSession(IDialogContext context, LuisResult result)
        {
            var entity = result.Entities.FirstOrDefault();
            if (entity != null)
            {
                switch (entity.Type)
                {
                    case "Speaker":
                        var speakerName = entity.Entity;
                        var speakers = CodeCamp.FindSpeakers(speakerName);
                        switch (speakers.Count())
                        {
                            case 0:
                                await context.PostAsync($"Sorry, I can't find any speakers named {speakerName}.");
                                context.Wait(MessageReceived);
                                break;

                            case 1:
                                await SessionSelectionOptions.SearchSessionsAndPostResults(
                                    context, new SessionSelectionOptions() { SpeakerName = speakerName });
                                context.Wait(MessageReceived);
                                break;

                            default:
                                await context.PostAsync($"I've found {speakers.Count()} speakers named {speakerName}.");

                                var options1 = new SessionSelectionOptions() { CandidateSpeakers = speakers.ToArray() };
                                var sessionDialog1 = new FormDialog<SessionSelectionOptions>(
                                    options1,
                                    SessionSelectionOptions.BuildFormForSpeakerDisambiguation,
                                    FormOptions.PromptInStart
                                    );

                                context.Call(sessionDialog1, SearchFormComplete);

                                break;
                        }

                        break;

                    case "Topic":
                        var topic = entity.Entity;
                        var options = new SessionSelectionOptions() { Topic = topic };
                        var sessionDialog = new FormDialog<SessionSelectionOptions>(
                            options,
                            SessionSelectionOptions.BuildFormForGeneralSearch,
                            FormOptions.PromptInStart
                            );

                        context.Call(sessionDialog, SearchFormComplete);

                        break;

                    case "Company":
                        var company = entity.Entity;
                    
                        await SessionSelectionOptions.SearchSessionsAndPostResults(
                            context, new SessionSelectionOptions() { CompanyName = company });
                        context.Wait(MessageReceived);

                        break;

                    default:
                        await context.PostAsync("Sorry, I can't understand what type of session you're looking for.");
                        context.Wait(MessageReceived);
                        break;
                }
            }
            else
            {
                // general session search
                var options = new SessionSelectionOptions();
                var sessionDialog = new FormDialog<SessionSelectionOptions>(
                    options,
                    SessionSelectionOptions.BuildFormForGeneralSearch,
                    FormOptions.PromptInStart
                    );

                context.Call(sessionDialog, SearchFormComplete);
            }
        }

        [LuisIntent("SessionInfo")]
        public async Task SessionInfo(IDialogContext context, LuisResult result)
        {
            var message = "Sorry, don't know what session you're trying to see.";
            int sessionIndex = -1;

            if (result.Entities != null && result.Entities.Count > 0
                && int.TryParse(result?.Entities[0].Entity, out sessionIndex))
            {                
                string[] sessionsList;

                if (context.ConversationData.TryGetValue("sessionsList", out sessionsList))
                {
                    if (sessionIndex > 0 && sessionIndex <= sessionsList.Count())
                    {
                        var session = CodeCamp.Sessions.FirstOrDefault(s => s.Id == sessionsList[sessionIndex - 1]);
                        if (session != null)
                        {
                            message = session.ToLongDisplayString();
                            context.ConversationData.SetValue("sessionId", session.Id);
                            message += "Type **add** to add this session to your schedule.";
                        }
                    }
                }
            }

            await context.PostAsync(message);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("ListSchedule")]
        public async Task ListSchedule(IDialogContext context, LuisResult result)
        {
            var message = "Currently, I can't find any sessions in your schedule.";
            string[] scheduleSessionsList;

            if (context.UserData.TryGetValue("schedule", out scheduleSessionsList)
                && scheduleSessionsList?.Length > 0)
            {
                message = "Here is your current schedule. Type **remove n** to remove the session at index **n**:\n";
                var sessions = scheduleSessionsList.Select(sl => CodeCamp.Sessions.FirstOrDefault(s => s.Id == sl)).ToArray();

                message += SessionSelectionOptions.GetSessionsListDisplayMessage(sessions);

                // save info about sessions
                context.ConversationData.SetValue("sessionsList", sessions.Select(s => s.Id).ToArray());
            }

            await context.PostAsync(message);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("AddToSchedule")]
        public async Task AddToSchedule(IDialogContext context, LuisResult result)
        {
            var message = "I can't find the session you want me to add to your schedule.";
            int sessionIndex = -1;
            string sessionId = null;
            string[] sessionsList, scheduleSessionsList;

            if (result.Entities != null && result.Entities.Count > 0
                && int.TryParse(result?.Entities[0]?.Entity, out sessionIndex))
            {
                // user wants to add a specific session from a list of sessions
                if (context.ConversationData.TryGetValue("sessionsList", out sessionsList))
                {
                    if (sessionIndex > 0 && sessionIndex <= sessionsList.Count())
                        sessionId = sessionsList[sessionIndex - 1];
                }
            }
            else
            {
                // user didn't specify a session index, we must be looking at a specific session
                context.ConversationData.TryGetValue("sessionId", out sessionId);
            }

            if (sessionId != null)
            {
                if (!context.UserData.TryGetValue("schedule", out scheduleSessionsList))
                {
                    scheduleSessionsList = new string[] { };
                }

                if (!scheduleSessionsList.Contains(sessionId))
                {
                    scheduleSessionsList = scheduleSessionsList.Union(new string[] { sessionId }).ToArray();
                    // reorder the sessions by start time and re-save
                    var sessions = scheduleSessionsList.Select(sl => CodeCamp.Sessions.FirstOrDefault(s => s.Id == sl))
                        .OrderBy(s => s.StartTime).ToArray();
                    scheduleSessionsList = sessions.Select(s => s.Id).ToArray();

                    context.UserData.SetValue("schedule", scheduleSessionsList);

                    await context.PostAsync("I've added the session to your schedule.");
                    await ListSchedule(context, result);
                    return;
                }
                else
                {
                    message = "That session is already in your schedule.";
                }
            }

            await context.PostAsync(message);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("RemoveFromSchedule")]
        public async Task RemoveFromSchedule(IDialogContext context, LuisResult result)
        {
            var message = "I can't find the session you want me to remove. Try specifying its schedule index, like **remove 2**.";
            int sessionIndex = -1;
            string[] scheduleSessionsList;

            if (result.Entities != null && result.Entities.Count > 0
                && int.TryParse(result?.Entities[0]?.Entity, out sessionIndex))
            {
                if (context.UserData.TryGetValue("schedule", out scheduleSessionsList))
                {
                    if (sessionIndex > 0 && sessionIndex <= scheduleSessionsList.Count())
                    {
                        scheduleSessionsList = scheduleSessionsList.Except(
                            new string[] { scheduleSessionsList[sessionIndex - 1] }).ToArray();
                        context.UserData.SetValue("schedule", scheduleSessionsList);

                        await context.PostAsync("I've removed the session from your schedule.");
                        await ListSchedule(context, result);
                        return;
                    }
                }
            }

            await context.PostAsync(message);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(WelcomeMessage);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("About")]
        public async Task About(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(AboutMessage);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Well, hello! Try typing **help** or **find a session**.");

            context.Wait(this.MessageReceived);
        }

        private async Task SearchFormComplete(IDialogContext context, IAwaitable<SessionSelectionOptions> result)
        {
            var state = await result;
            await SessionSelectionOptions.SearchSessionsAndPostResults(context, state);
            context.Wait(MessageReceived);
        }

        public static string WelcomeMessage = @"**Hi!** I'm the **Codecamp Romania Bot (beta)**, it's nice to meet you :)

I can tell you all about what's going on at our next [Codecamp Romania event](http://iasi.codecamp.ro/) in **Iasi** on **October 22nd**. 

Here are some examples of things you can ask me: 

* *When is **Florin Cardasim** speaking?*
* *Are there any sessions on **Agile**?*
* *Is anybody from **Microsoft** doing a session?*
* *Just help me choose some sessions!*

You can also type **help** to see this message again.

To find out more about me, type **about**.";

        public static string AboutMessage = @"I'm the **[Codecamp Romania](http://www.codecamp.ro/) Bot v0.11 (beta)**


I'm built using Microsoft's [Bot Framework](https://dev.botframework.com/).  

[LUIS](https://www.luis.ai/) helps me get better and better at talking to people.

The [Azure Cloud](https://azure.microsoft.com/) provides the juice I need to keep going.

My source code is [on GitHub](https://github.com/neaorin/BotFrameworkDemo). You can report any issues [here](https://github.com/neaorin/BotFrameworkDemo/issues).


*-- Sorin Peste (sorinpe at microsoft dot com)*
";

    }

    [Serializable]
    public class SessionSelectionOptions
    {
        public Speaker[] CandidateSpeakers { get; set; }

        public string SpeakerName;
        public string CompanyName;
        public string Topic;
        public LevelTypes Level;

        public static IForm<SessionSelectionOptions> BuildFormForSpeakerDisambiguation()
        {
            return new FormBuilder<SessionSelectionOptions>()
                .Field(new FieldReflector<SessionSelectionOptions>(nameof(SpeakerName))
                            .SetActive((state) => state.CandidateSpeakers != null && state.CandidateSpeakers.Count() > 0)
                            .SetType(null)                            
                            .SetDefine(async (state, field) =>
                            {
                                foreach (var speaker in state.CandidateSpeakers)
                                    field
                                        .AddDescription(speaker.Name, speaker.Name, speaker.PhotoUrl)
                                        .AddTerms(speaker.Name, speaker.Name);
                                return true;
                            })
                            )
                .Build();
        }

        public static IForm<SessionSelectionOptions> BuildFormForGeneralSearch()
        {
            return new FormBuilder<SessionSelectionOptions>()
                .Field(new FieldReflector<SessionSelectionOptions>(nameof(Topic))
                            .SetActive((state) => String.IsNullOrWhiteSpace(state.Topic))
                            .SetType(null)
                            .SetDefine(async (state, field) =>
                            {
                                foreach (var topic in CodeCamp.Topics)
                                    field
                                        .AddDescription(topic.Name, topic.Name)
                                        .AddTerms(topic.Name, topic.Terms.ToArray());
                                return true;
                            })
                            )
                .Field(new FieldReflector<SessionSelectionOptions>(nameof(Level))
                            .SetActive((state) => !state.Topic.ContainsIgnoreCase("misc"))
                        )
                .Build();
        }

        public static async Task SearchSessionsAndPostResults(IDialogContext context, SessionSelectionOptions state)
        {
            string message = "Sorry, I can't understand what type of session you're looking for.";

            var speakerName = state.SpeakerName == null ? null : new string[] { state.SpeakerName };

            var topic = CodeCamp.Topics.Where(t => t.Name.Equals(state.Topic, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var topicValues = (topic == null ?
                                String.IsNullOrWhiteSpace(state.Topic) ? null : new string[] { state.Topic }
                                : topic.Terms);

            // perform the session search
            var sessions = CodeCamp.FindSessions(speakerName, topicValues, state.CompanyName, state.Level).ToArray();
            if (sessions.Count() > 0)
            {
                message = $"I've found some sessions. To see more details about a session, type its number.\n";
                message += $"Or, type **add n** to add the session at index **n** to your schedule.\n";
                message += GetSessionsListDisplayMessage(sessions);

                // save info about sessions
                context.ConversationData.SetValue("sessionsList", sessions.Select(s => s.Id).ToArray());
            }
            else
            {
                message = $"Sorry, I can't find any sessions of interest for you.";
            }
            await context.PostAsync(message);
        }

        internal static string GetSessionsListDisplayMessage(Session[] sessions)
        {
            var message = "";
            for (int i = 0; i < sessions.Count(); i++)
                message += $"{i + 1}. {sessions[i].ToShortDisplayString()}\n";
            return message;
        }

    }
}
