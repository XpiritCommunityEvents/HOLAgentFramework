using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Agents;

public static class ItineraryDocumentAgent
{
    public static AIAgent Create(IChatClient chatClient) => chatClient.AsAIAgent(
        name: "ItineraryDocumentAgent",
        description: "Turns a completed travel plan into a PDF itinerary.",
        instructions: """
            You are the itinerary document specialist. Receive a JSON object containing a concert ticket,
            hotel, and ride. Call generate_itinerary_pdf once and return only the resulting PDF path.
            Do not book anything and do not ask questions.
            """,
        tools: [AIFunctionFactory.Create(ItineraryDocumentFunctions.GenerateItineraryPdf, "generate_itinerary_pdf")]);
}
