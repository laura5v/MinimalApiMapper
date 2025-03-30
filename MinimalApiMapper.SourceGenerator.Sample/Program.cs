using MinimalApiMapper.Generated;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddApiGroups();

var app = builder.Build();

app.MapApiGroups();
app.MapGet("/", () => "Hello World!");

await app.RunAsync();

