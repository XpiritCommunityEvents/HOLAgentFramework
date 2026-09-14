using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Agents;

public static class TicketReaderAgent
{
    public static AIAgent Create(IChatClient chatClient) => chatClient.AsAIAgent(
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
