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
            string message = $"I am sorry, I did not understand you. Why don't you try typing 'find a session'?";
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

        private async Task SearchFormComplete(IDialogContext context, IAwaitable<SessionSelectionOptions> result)
        {
            var state = await result;
            await SessionSelectionOptions.SearchSessionsAndPostResults(context, state);
            context.Wait(MessageReceived);
        }

        public static string WelcomeMessage = @"**Hi!** I'm the **CodeCamp Bot (beta)**, it's nice to meet you :)

I can tell you all about what's going on at our CodeCamp event in **Iasi** on **October 22nd**. 

Here are some examples of things you can ask me: 
* *When is **Florin Cardasim** speaking?*
* *Are there any sessions on **React**?*
* *Is anybody from **Microsoft** doing a session?*
* *Just help me choose some sessions!*

You can also type **help** to see this message again.

To find out more about me, type **about**.";

        public static string AboutMessage = @"I'm the **CodeCamp Bot v0.11 (beta)**

I'm built using Microsoft's [Bot Framework](https://dev.botframework.com/).  

[LUIS](https://www.luis.ai/) helps me get better and better at being able to talk to people.

The [Azure Cloud](https://azure.microsoft.com/) gives me the juice I need to keep going.

My source code is [on GitHub](https://github.com/neaorin/BotFrameworkDemo). You can report any issues [here](https://github.com/neaorin/BotFrameworkDemo/issues).

*-- Sorin Peste (sorinpe at microsoft dot com)*
";

    }

    public enum SpeakerNames { Sorin, Gigi, Alex };

    [Serializable]
    public class SessionSelectionOptions
    {
        public Speaker[] CandidateSpeakers { get; set; }

        public string SpeakerName;
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
                            .SetActive((state) => state.Topic != null)
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
            var topic = CodeCamp.Topics.Where(t => t.Name.ContainsIgnoreCase(state.Topic)).FirstOrDefault();

            // perform the session search
            var sessions = CodeCamp.FindSessions(state.SpeakerName, topic?.Terms, state.Level);
            if (sessions.Count() > 0)
            {
                message = $"I've found the following sessions:\n";
                foreach (var session in sessions)
                    message += $"* {session.ToDisplayString()}\n";
            }
            else
            {
                message = $"Sorry, I can't find any sessions of interest for you.";
            }
            await context.PostAsync(message);
        }

    }
}
