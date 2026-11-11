using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Tools;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace Module04.Demo05.Agents;

public static class TicketReaderAgent
{
    public static AIAgent Create(ResponsesClient responsesClient, string model) => responsesClient.AsAIAgent(
        model: model,
        name: "TicketReaderAgent",
        description: "Reads a concert ticket PDF and returns verified event details.",
        instructions: """
            You read concert tickets. When the user provides a PDF URL, download it and extract its text.
            Never guess details that are absent from the PDF. Return exactly one JSON object with Artist,
            VenueName, City, EventDate, and Seat. Do not book anything and do not ask questions.
            """,
        tools:
        [
            AIFunctionFactory.Create(TicketReadingFunctions.DownloadFile, "download_file"),
            AIFunctionFactory.Create(TicketReadingFunctions.ExtractPdfText, "extract_pdf_text")
        ]);
}
