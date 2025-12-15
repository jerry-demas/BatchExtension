using CBIZ.CCH.BatchExtension.Presentation.BackgroundService;
using CBIZ.CCH.BatchExtension.Application;
using CBIZ.CCH.BatchExtension.API;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.SharedPackages.Mail;
using CBIZ.CCH.BatchExtension.API.Middelware;
using CBIZ.CCH.BatchExtension.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMailService();

builder.AddProgramServiceLayer(builder.Configuration)
       .AddApplicationServiceLayer(builder.Configuration);

builder.Services.AddSingleton<BatchQueue>() 
                .AddScoped<EmailService>()  
                .AddScoped<MailService>()                           
                .AddHostedService<Worker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapBatchEndpoints();


app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();


await app.RunAsync();




