using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
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
