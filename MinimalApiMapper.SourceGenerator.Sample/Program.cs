using MinimalApiMapper.Generated;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddAuthentication().AddBearerToken();
builder.Services.AddAuthorization();

builder.Services.AddApiGroups();

var app = builder.Build();

app.MapApiGroups();

await app.RunAsync();

