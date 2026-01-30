using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace modulerag;

public class ChatWithRag
{
    public async Task RAG_with_single_prompt(IChatClient chatClient)
    {
        var question =
        """
        I booked tickets for a concert tonight in venue AFAS Live!.
        I have this small black backpack, not big like for school, more like the mini
        festival type 😅. it just fits my wallet, a hoodie and a bottle of water.
        Is this allowed?
        """;

        //var policyContext = "";
        //var policyContext = File.ReadAllText("../../../../datasets/venue-policies/AFAS_Live.md");

        var venue = await GetVenueFromQuestion(chatClient, question);
        var policyContext = await GetVenuePolicyFileContents(chatClient, venue);

        //await GetResponseOnQuestionSimple(chatClient, question, policyContext);
        await GetResponseOnQuestion(chatClient, question, policyContext);
    }

    private async Task<string> GetVenuePolicyFileContents(IChatClient chatClient, string venueName)
    {
        //Get a list of files from the venue policy repository
        var directory = "../../../../datasets/venue-policies";
        var fileList = string.Join("\n", System.IO.Directory.GetFiles(directory, "*.md").Select(f => System.IO.Path.GetFileName(f)));

        var systemPrompt = "You are an expert at finding the correct file based on a user question.";
        var fileListPrompt = $"The following is a list of files available:\n{fileList}";
        var fileQuestion = $"Which file contains the venue policy for the venue named '{venueName}'?";

        var messages = new List<ChatMessage>();

        messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        messages.Add(new ChatMessage(ChatRole.User, fileListPrompt));
        messages.Add(new ChatMessage(ChatRole.User, fileQuestion));

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(SelectedFile));

        var chatOptions = new ChatOptions()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                                schema: schema,
                                schemaName: nameof(SelectedFile),
                                schemaDescription: "An object containing the full file name of a file")
        };

        AIAgent agent = chatClient.AsAIAgent(
            new ChatClientAgentOptions() { ChatOptions = chatOptions });


        var response = await agent.RunAsync(messages);
        var fileResult = response.Deserialize<SelectedFile>(JsonSerializerOptions.Web);
        
        var fullFileName = Path.Combine(directory, fileResult.File);

        if (File.Exists(fullFileName))
        {
            using (var file = File.OpenText(fullFileName))
            {
                return await file.ReadToEndAsync();
            }
        }

        return "No Policy information found";
    }

    private async Task<string> GetVenueFromQuestion(IChatClient chatClient, string question)
    {
        var messages = new List<ChatMessage>();

        messages.Add(new ChatMessage(ChatRole.System, "You are a helpful asistant that finds the name of a venue from a question."));
        messages.Add(new ChatMessage(ChatRole.System, "Always get the information from the question. Never search the web or use internal knowledge!"));
        messages.Add(new ChatMessage(ChatRole.User, question));

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(SelectedVenue));

        var chatOptions = new ChatOptions()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                                schema: schema,
                                schemaName: nameof(SelectedVenue),
                                schemaDescription: "An object containing the name of a venue")
        };

        AIAgent agent = chatClient.AsAIAgent(
            new ChatClientAgentOptions() { ChatOptions = chatOptions });

        var response = await agent.RunAsync(messages);
        var selectedVenue = response.Deserialize<SelectedVenue>(JsonSerializerOptions.Web);
        return selectedVenue!.VenueName;
    }

    private async Task GetResponseOnQuestion(IChatClient chatClient, string question, string policyContext)
    {
        AIAgent agent = chatClient.AsAIAgent();

        var messages = new List<ChatMessage>();
        messages.Add(new ChatMessage(ChatRole.System, "You are a helpful assistant that answers questions from people that go to a concert and have questions about the venue.")); ;
        messages.Add(new ChatMessage(ChatRole.System, "Always use the policy information provided in the prompt"));
        messages.Add(new ChatMessage(ChatRole.System, $"### Venue Policy\n {policyContext}"));

        messages.Add(new ChatMessage(ChatRole.User, question));

        var questionResponse = agent.RunStreamingAsync(messages);
        await foreach (var response in questionResponse)
        {
            Console.Write(response);
        }
    }

    private async Task GetResponseOnQuestionSimple(IChatClient chatClient, string question, string policyContext)
    {
        AIAgent agent = chatClient.AsAIAgent();

        var userMessage = new ChatMessage(ChatRole.User, question);

        var questionResponse = agent.RunStreamingAsync(userMessage);
        await foreach (var response in questionResponse)
        {
            Console.Write(response);
        }
    }
}
