using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("github-models-key",
    value: "INSERT_PAT_HERE",
    secret: true);

var openai = builder.AddOpenAI("openai")
                    .WithApiKey(apiKey)
                    .WithEndpoint("https://models.github.ai/inference");

var program = builder.AddProject<demo_04>("demo-04")
                    .WithReference(openai);

builder.Build().Run();
