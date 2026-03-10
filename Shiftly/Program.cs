using Microsoft.EntityFrameworkCore;
using Shiftly.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString =
"Server=localhost;Database=Shiflty;User=root;Password=1234;";
builder.Services.AddDbContext<ShiftlyDbContext>(options =>
options.UseMySql(connectionString,
ServerVersion.AutoDetect(connectionString)));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//GET: all users
app.MapGet("/GetAllGebruikers", async (ShiftlyDbContext db) =>
{
    var gebruikers = await db.Gebruikers.Select(pbl => new
    {
        idGebruiker = pbl.IdGebruiker,
        VoorNaamGebruiker = pbl.VoorNaamGebruiker,
        NaamGebruiker = pbl.NaamGebruiker,
        EmailGebruiker = pbl.EmailGebruiker,
        IsStudent = pbl.IsStudent
    }).ToListAsync();
    return Results.Ok(gebruikers);
});

app.Run();