# Lab 4.2 - Ingesting documents into a vector database to allow semantic search

In this lab you will learn how to build an advanced Retrieval-Augmented Generation (RAG) system using KernelMemory to chunk and ingest documents into a vector database. Unlike the previous lab where we injected complete documents as context, this approach automatically breaks down large documents into smaller, searchable chunks and stores them in a vector database. This enables semantic search capabilities where the system can find the most relevant pieces of information based on the meaning of the user's question, rather than exact keyword matches. The LLM then uses these retrieved chunks to generate accurate, context-aware responses based on your domain knowledge.

KernelMemory provides a flexible architecture that supports various vector database backends. In this lab, we'll use a FileSystem Vector DB for simplicity, but the same code can be easily configured to work with production-grade solutions like Azure AI Search, Azure Cosmos DB, or Qdrant without changing your application logic.

## Ingestion with KernelMemory

### Steps

#### 1. Open the solution 

In the **Solution Explorer** in your codespace by right-clicking the solution file and choose `Open Solution`

#### 3. Add a new function called IngestDocuments
We need to have a new method that will handle the ingestion of documents into the vector database. In the `ChatWithRag.cs` file, add a new method called `IngestDocuments`.

Use the following code

```csharp
public async Task IngestDocuments(IConfiguration config)
{
    var directory = "../../../../datasets/venue-policies";

    var memoryConnector = GetLocalKernelMemory(config);

    foreach (var file in GetFileListOfPolicyDocuments(directory))
    {
        var fullfilename = Path.Combine(directory, file);
        var importResult = await memoryConnector.ImportDocumentAsync(filePath: fullfilename, documentId: file);
        Console.WriteLine($"Imported file {file} with result: {importResult}");
    }
}
```
You can see that based on the configuration, we create a `KernelMemory` connector. Then we loop through all the files in the specified directory and call the `ImportDocumentAsync` method to ingest each document into the vector database. 

#### 4. Add the GetLocalKernelMemory method
Now we need to add the `GetLocalKernelMemory` method that will create the KernelMemory connector to the vector database. We will host the vector database on the file system for now. This method is explained step by step. You can find the whole implementation at the end of this section. Bring in the neccessary namespaces at the top of the file:

1. Create the TextModel Configuration 

```csharp
// 1. Configure text generation service
var textGenerationConfig = new OpenAIConfig
{
    Endpoint = config["OpenAI:EndPoint"]!,
    APIKey = config["OpenAI:ApiKey"]!,
    TextModel = config["OpenAI:Model"]!,
};
```

The method starts by extracting the API credentials from the configuration. It creates an `OpenAIConfig` object specifically for text generation, specifying the endpoint URL, API key, and the deployment name of the language model that will be used for generating responses and processing user queries.

1. Create the EmbeddingModel Configuration 

```csharp
// 2. Configure embedding generation service
var openAiEmbeddingsConfig = new OpenAIConfig
{
    APIKey = config["OpenAI:ApiKey"]!,
    Endpoint = config["OpenAI:EndPoint"]!,
    EmbeddingModel = config["OpenAI:EmbeddingModel"]!,
};
```

This creates a separate `OpenAIConfig` object specifically for the embedding service. This uses the "text-embedding-3-small" model to convert text documents and queries into vector representations that enable semantic similarity searches in the vector database.

3. Initialize the KernelMemory

```csharp
var kernelMemoryBuilder = new KernelMemoryBuilder()
    // 3. Configure file storage backend
    .WithSimpleFileStorage(new SimpleFileStorageConfig
    {
        Directory = "kernel-memory/km-file-storage",
        StorageType = FileSystemTypes.Disk
    })
```
Initializes a `KernelMemoryBuilder` instance to start configuring the KernelMemory system. The first step is to set up the file storage backend using local disk storage. This backend will store the original document files in the specified directory on the local filesystem.

4. Configure text database backend

```csharp
// 4. Configure text database backend
.WithSimpleTextDb(new SimpleTextDbConfig
{
    Directory = "kernel-memory/km-text-db",
    StorageType = FileSystemTypes.Disk
})
```
Now add the text database backend configuration. This backend stores the textual content of the documents in a structured format that allows for efficient retrieval and management of text data.

5. Configure vector database backend
```csharp
// 5. Configure vector database backend
.WithSimpleVectorDb(new SimpleVectorDbConfig
{
    Directory = "kernel-memory/km-vector-db",
    StorageType = FileSystemTypes.Disk
})
```
Sets up the vector database backend using local disk storage. This backend is responsible for storing the vector representations of document chunks, enabling semantic search capabilities based on vector similarity.

6. Integrate AI services and build the memory instance

```csharp
// 6. Integrate AI services
.WithOpenAITextEmbeddingGeneration(openAiEmbeddingsConfig)
.WithOpenAITextGeneration(textGenerationConfig);

return kernelMemoryBuilder.Build();
```

Connects both the embedding generation service and text generation service to the KernelMemory system. The embedding service automatically converts documents into searchable vectors during ingestion, while the text generation service generates responses based on retrieved context.

Then finalize the configuration and returns a fully functional KernelMemory instance that can ingest documents, perform semantic searches, and integrate with language models for RAG operations. 

<details>
<summary>Complete GetLocalKernelMemory Method Code</summary>

```csharp
private IKernelMemory GetLocalKernelMemory(IConfiguration config)
{
    // 1. Configure text generation service
    var textGenerationConfig = new OpenAIConfig
    {
        Endpoint = config["OpenAI:EndPoint"]!,
        APIKey = config["OpenAI:ApiKey"]!,
        TextModel = config["OpenAI:Model"]!,
    };

    // 2. Configure embedding generation service
    var openAiEmbeddingsConfig = new OpenAIConfig
    {
        APIKey = config["OpenAI:ApiKey"]!,
        Endpoint = config["OpenAI:EndPoint"]!,
        EmbeddingModel = config["OpenAI:EmbeddingModel"]!,
    };

    // 3-6. Build comprehensive KernelMemory system
    var kernelMemoryBuilder = new KernelMemoryBuilder()
        // 3. Configure file storage backend
        .WithSimpleFileStorage(new SimpleFileStorageConfig
        {
            Directory = "kernel-memory/km-file-storage",
            StorageType = FileSystemTypes.Disk
        })
        // 4. Configure text database backend
        .WithSimpleTextDb(new SimpleTextDbConfig
        {
            Directory = "kernel-memory/km-text-db",
            StorageType = FileSystemTypes.Disk
        })
        // 5. Configure vector database backend
        .WithSimpleVectorDb(new SimpleVectorDbConfig
        {
            Directory = "kernel-memory/km-vector-db",
            StorageType = FileSystemTypes.Disk
        })
        // 6. Integrate AI services
        .WithOpenAITextEmbeddingGeneration(openAiEmbeddingsConfig)
        .WithOpenAITextGeneration(textGenerationConfig);

    return kernelMemoryBuilder.Build();
}
```

</details>

#### 5. Add a helper method to get the list of files
Add the following method to get the list of files from the directory

```csharp
private IEnumerable<string> GetFileListOfPolicyDocuments(string directory)
{
    return Directory.GetFiles(directory, "*.pdf").Select(f => Path.GetFileName(f));
}
```

#### 6. Call the IngestDocuments method
Finally, we need to call the IngestDocuments method from Program.cs to ingest the documents when we run the application.

```csharp
await new ChatWithRag().IngestDocuments(config);
```

#### 7. Run the application
Now run the application. This will ingest all the documents from the specified directory into the vector database. In the solutition explorer you will see a new folder called `kernel-memory` with the files stored in the vector database. This can also be found in `bin/debug` depending on your build configuration.

Check the `km-file-storage` folder to see the ingested documents. Each document will be chunked into smaller pieces and stored in the `km-vector-db` folder as vectors.

In the `km-vector-db` folder you will see files that contain the vector representations of the document chunks. These vectors enable semantic search capabilities in the RAG system.

## The End 
This concludes the ingestion part of the lab. In the next part, we will modify the chat method to perform semantic search using the vector database.