using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WorkItems.Api.Data;
using WorkItems.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<IWorkItemsService, WorkItemsService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<WorkItemsDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("WorkItems") ?? "Server=(localdb)\\MSSQLLocalDB;Database=WorkItems;Trusted_Connection=True;TrustServerCertificate=True"));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.MapControllers();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<WorkItemsDbContext>().Database.EnsureCreatedAsync();
app.Run();
public partial class Program { }
