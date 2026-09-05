using System.Text.Json;
using Microsoft.Agents.AI;
using System.ClientModel;
using OpenAI.Chat;

namespace modulerag;

public class ChatWithRag
{
    readonly string systemPrompt = """
            You are a helpful asistant that finds the name of a venue from a question.
            Always get the information from the question. Never search the web or use internal knowledge!
            You are a helpful assistant that answers questions from people that go to a concert and have questions about the venue
            Always use the policy information provided in the prompt
       """;
    public async Task RAG_with_single_prompt(AIAgent agent, string endpoint, string token, string model)
    {
        var question = 
        """
        I booked tickets for a concert tonight in venue AFAS Live!.
        I have this small black backpack, not big like for school, more like the mini
        festival type 😅. it just fits my wallet, a hoodie and a bottle of water.
        Is this allowed?
        """;

        //var policyContext ="";
        //var policyContext = await GetVenuePolicyFileContentsChatClient(agent, endpoint, token, model, "AFAS Live!");
        var policyContext = await GetVenuePolicyFileContentsAgentFramework(agent, "AFAS Live!");
        
        //var policyContext = File.ReadAllText("../../../../datasets/venue-policies/AFAS_Live.md");
        
        await GetResponseOnQuestionSimple(agent, systemPrompt, question, policyContext);
        //await GetResponseOnQuestion(agent, question, policyContext);
    }

    private async Task<string> GetVenuePolicyFileContentsChatClient(AIAgent agent, string endpoint, string token, string model,  string venueName)
    {
        //Get a list of files from the venue policy repository
        var directory = "../../../../datasets/venue-policies";
        var fileNames = Directory.GetFiles(directory, "*.md")
            .Select(Path.GetFileName)
            .Where(fileName => fileName is not null)
            .ToArray();
        var fileList = string.Join("\n", fileNames);
        
        var systemPrompt = "You select the single filename that contains the venue policy requested by the user. Return only a filename from the supplied list.";
        var userPrompt = $"Available files:\n{fileList}\n\nWhich file contains the venue policy for the venue named '{venueName}'?";
        
        var chatClient = new OpenAI.Chat.ChatClient(
            model,
            new ApiKeyCredential(token),
            new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
            });
        var chatMessages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "selected_file",
                jsonSchema: BinaryData.FromString("""
                    {
                      "type": "object",
                      "properties": {
                        "File": { "type": "string" }
                      },
                      "required": ["File"],
                      "additionalProperties": false
                    }
                    """),
                jsonSchemaIsStrict: true)
        };

        var result = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
        var responseText = result.Value.Content[0].Text;
        var fileResult = JsonSerializer.Deserialize<SelectedFile>(responseText)
            ?? throw new InvalidOperationException("The model returned an invalid file selection.");
        var selectedFileName = fileNames.FirstOrDefault(fileName =>
            string.Equals(fileName, fileResult.File, StringComparison.OrdinalIgnoreCase));

        if (selectedFileName is null)
        {
            return "No Policy information found";
        }

        var fullFileName = Path.Combine(directory, selectedFileName);

        if (System.IO.File.Exists(fullFileName))
        {
            using (var file = File.OpenText(fullFileName))
            {
                return await file.ReadToEndAsync();
            }
        }
        
        return "No Policy information found";
    }
 private async Task<string> GetVenuePolicyFileContentsAgentFramework(AIAgent agent,  string venueName)
    {
        //Get a list of files from the venue policy repository
        var directory = "../../../../datasets/venue-policies";
        var fileNames = Directory.GetFiles(directory, "*.md")
            .Select(Path.GetFileName)
            .Where(fileName => fileName is not null)
            .ToArray();
        var fileList = string.Join("\n", fileNames);
        
        var systemPrompt = "You select the single filename that contains the venue policy requested by the user. Return only a filename from the supplied list.";
        var userPrompt = $"Available files:\n{fileList}\n\nWhich file contains the venue policy for the venue named '{venueName}'?";
        
        AgentSession session = await agent.CreateSessionAsync();
        session.SetInMemoryChatHistory(
        [
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userPrompt),
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt)
        ]);

        var result = await agent.RunAsync<SelectedFile>(session);

        var selectedFileName = result.Result.File;
        
        if (selectedFileName is null)
        {
            return "No Policy information found";
        }

        var fullFileName = Path.Combine(directory, selectedFileName);

        if (File.Exists(fullFileName))
        {
            using var file = File.OpenText(fullFileName);
            return await file.ReadToEndAsync();
        }
        
        return "No Policy information found";
    }

    private async Task GetResponseOnQuestionSimple(AIAgent agent, string systemPrompt, string question, string policyContext)
    {
        AgentSession session = await agent.CreateSessionAsync();
        session.SetInMemoryChatHistory(
        [
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, question),
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, policyContext),
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt)
        ]);
        
        var questionResponse = agent.RunStreamingAsync(session);
        await foreach (var response in questionResponse)
        {
            Console.Write(response.Text);
        }
    }
}
