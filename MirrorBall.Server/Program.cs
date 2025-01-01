using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MirrorBall.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

var hostName = System.Net.Dns.GetHostName();
Console.WriteLine($"Hostname is apparently {hostName}");

var configJson = File.ReadAllText("appsettings.json");

var options = JObject.Parse(configJson)[hostName].ToObject<MirrorOptions>();

Console.WriteLine(JsonConvert.SerializeObject(options, Formatting.Indented));

builder.Services.AddSingleton(Options.Create(options));
builder.Services.AddControllers();

var app = builder.Build();
app.UseStaticFiles();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();
