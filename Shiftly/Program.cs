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
    var shifts = await db.Shifts
        .Include(pbl => pbl.FkGebruikerAbbonomentNavigation)
        .Where(pbl => pbl.FkGebruikerAbbonomentNavigation.FkGebruiker == userId)
        .Select(pbl => new
        {
            ShiftId = pbl.IdShift,
            StartDateTime = pbl.StartDateTime,
            EindDateTime = pbl.EindDateTime,
            PauzeInMinuten = pbl.PauzeInMinuten,
            Functie = pbl.Functie,
            Opmerking = pbl.Opmerking,
            Uurloon = pbl.FkGebruikerAbbonomentNavigation.Uurloon,
            AfdelingId = pbl.FkGebruikerAbbonomentNavigation.FkAfdeling
        })
        .ToListAsync();

    return Results.Ok(shifts);
});

// GET: All Subscriptions From User
app.MapGet("/GetAllSubscriptionsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var subscriptions = await db.Gebruikerabonnements
        .Where(ga => ga.FkGebruiker == userId)
        .Include(ga => ga.FkAbonnementNavigation)
        .Select(ga => new
        {
            AbonnementId = ga.FkAbonnement,
            Naam = ga.FkAbonnementNavigation.NaamAbonnement,
            Prijs = ga.FkAbonnementNavigation.BedragAbonnement,
            PeriodeStart = ga.PeriodeStart,
            PeriodeEinde = ga.PeriodeEinde,
            Betaald = ga.Betaald
        })
        .ToListAsync();

    return Results.Ok(subscriptions);
});

app.Run();