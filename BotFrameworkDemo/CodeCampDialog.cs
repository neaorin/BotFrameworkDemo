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

                                var options = new SessionSelectionOptions() { CandidateSpeakers = speakers.ToArray() };
                                var sessionDialog = new FormDialog<SessionSelectionOptions>(
                                    options,
                                    SessionSelectionOptions.BuildForm,
                                    FormOptions.PromptInStart
                                    );

                                context.Call(sessionDialog, SearchFormComplete);

                                break;
                        }

                        break;

                    default:
                        await context.PostAsync("Sorry, I can't understand what type of session you're looking for.");
                        context.Wait(MessageReceived);
                        break;
                }
            }
        }

        private async Task SearchFormComplete(IDialogContext context, IAwaitable<SessionSelectionOptions> result)
        {
            var state = await result;
            await SessionSelectionOptions.SearchSessionsAndPostResults(context, state);
            context.Wait(MessageReceived);
        }

        internal static IDialog<SessionSelectionOptions> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(SessionSelectionOptions.BuildForm));
        }

    }

    public enum SpeakerNames { Sorin, Gigi, Alex };

    [Serializable]
    public class SessionSelectionOptions
    {
        public Speaker[] CandidateSpeakers { get; set; }

        public string SpeakerName;
        public string Topic;

        public static IForm<SessionSelectionOptions> BuildForm()
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

        public static async Task SearchSessionsAndPostResults(IDialogContext context, SessionSelectionOptions state)
        {
            string message = "Sorry, I can't understand what type of session you're looking for.";
            // perform the session search
            var sessions = CodeCamp.FindSessions(state.SpeakerName, state.Topic);
            if (sessions.Count() > 0)
            {
                message = $"I've found the following sessions:";
                foreach (var session in sessions)
                    message += $"\n{session}";
            }
            else
            {
                message = $"Sorry, I can't find any sessions of interest for you.";
            }
            await context.PostAsync(message);
        }

    }
}
