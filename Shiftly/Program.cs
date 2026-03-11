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

// GET: All Users
app.MapGet("/GetAllGebruikers", async (ShiftlyDbContext db) =>
{
    var gebruikers = await db.Gebruikers.Select(pbl => new
    {
        idGebruiker = pbl.IdGebruiker,
        VoorNaamGebruiker = pbl.VoorNaamGebruiker,
        NaamGebruiker = pbl.NaamGebruiker,
        EmailGebruiker = pbl.EmailGebruiker,
        IsStudent = pbl.IsStudent,
        WachtwoordGebruiker = pbl.WachtwoordGebruiker,
        IsActief = pbl.IsActief,
        createdAt = pbl.CreatedAt,
        updatedat = pbl.UpdatedAt
    }).ToListAsync();
    return Results.Ok(gebruikers);
});

// GET: All Shifts From User
app.MapGet("/GetAllShiftsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var items = await db.Shifts
        .Include(s => s.FkGebruikerAfdelingNavigation)
        .Where(s => s.FkGebruikerAfdelingNavigation.FkGebruiker == userId)
        .Select(s => new
        {
            ShiftId = s.IdShift,
            StartDateTime = s.StartDateTime,
            EindDateTime = s.EindDateTime,
            Functie = s.Functie,
            PauzeInMinuten = s.PauzeInMinuten,
            Opmerking = s.Opmerking,
            Uurloon = s.FkGebruikerAfdelingNavigation.Uurloon,
            AfdelingId = s.FkGebruikerAfdelingNavigation.FkAfdeling
        })
        .ToListAsync();

    return Results.Ok(items);
});


app.Run();