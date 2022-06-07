using Amazon.Lambda.Core;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Newtonsoft.Json;
using Alexa.NET;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace HelloAlexa;

public class Function
{
    public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
    {
        ILambdaLogger log = context.Logger;
        log.LogLine($"Skill Request Object:" + JsonConvert.SerializeObject(input));

        Session session = input.Session;
        if (session.Attributes == null)
            session.Attributes = new Dictionary<string, object>();

        Type requestType = input.GetRequestType();
        if (input.GetRequestType() == typeof(LaunchRequest))
        {
            string speech = "Welcome! I can tell you the time in different cities.";
            Reprompt rp = new Reprompt("Say what time is it in a city");
            return ResponseBuilder.Ask(speech, rp, session);
        }
        else if (input.GetRequestType() == typeof(SessionEndedRequest))
        {
            return ResponseBuilder.Tell("Goodbye!");
        }
        else if (input.GetRequestType() == typeof(IntentRequest))
        {
            var intentRequest = (IntentRequest)input.Request;
            switch (intentRequest.Intent.Name)
            {
                case "AMAZON.CancelIntent":
                case "AMAZON.StopIntent":
                    return ResponseBuilder.Tell("Goodbye!");
                case "AMAZON.HelpIntent":
                    {
                        Reprompt rp = new Reprompt("What's next?");
                        return ResponseBuilder.Ask("Here's some help. What's next?", rp, session);
                    }
                case "what_time_is_it":
                    {
                        string location = intentRequest.Intent.Slots["location"].Value;
                        DateTime now = DateTime.UtcNow;
                        (string place, int offset, string timezone) localTime = GetLocationOffset(location);
                        string message = $"Right now in {localTime.place} it is {now.AddHours(localTime.offset).ToShortTimeString()} {localTime.timezone}.";
                        return ResponseBuilder.Tell(message, session);
                    }
                default:
                    {
                        log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                        string speech = "I didn't understand - try again?";
                        Reprompt rp = new Reprompt(speech);
                        return ResponseBuilder.Ask(speech, rp, session);
                    }
            }
        }
        return ResponseBuilder.Tell("Goodbye!");
    }

    private (string location, int offset, string timezone) GetLocationOffset(string location)
    {
        switch(location)
        {
            case "Los Angeles":
            case "Seattle":
                return (location, -8, "Pacific Standard Time");
            case "Denver":
            case "Phoenix":
                return (location, -7, "Mountain Standard Time");
            case "Chicago":
            case "Dallas":
                return (location, -6, "Central Standard Time");
            case "Atlanta":
            case "New York":
                return (location, -5, "Eastern Standard Time");
            default:
                return ("Greenwich", 0, "Greenwich Mean Time");
        }
    }
}
