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

// GET: All Shifts
app.MapGet("/GetAllShifts", async (ShiftlyDbContext db) =>
{
    var shifts = await db.Shifts.Select(pbl => new
    {
        idShift = pbl.IdShift,
        StartDateTime = pbl.StartDateTime,
        EindDateTime = pbl.EindDateTime,
        PauzeInMinuten = pbl.PauzeInMinuten,
        Functie = pbl.Functie,
        Opmerking = pbl.Opmerking
    }).ToListAsync();
    return Results.Ok(shifts);
});

// GET: All Subscriptions
app.MapGet("/GetAllSubscriptions", async (ShiftlyDbContext db) =>
{
    var subscriptions = await db.Abonnements.Select(pbl => new
    {
        idAbonnement = pbl.IdAbonnement,
        NaamAbonnement = pbl.NaamAbonnement,
        OmschrijvingAbonnement = pbl.OmschrijvingAbonnement,
        BedragAbonnement = pbl.BedragAbonnement,
        IsActief = pbl.IsActief
    }).ToListAsync();
    return Results.Ok(subscriptions);
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

// GET: User Using Password And Email 
app.MapGet("/login", async (string email, string password, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers
        .Where(pbl => pbl.EmailGebruiker == email && pbl.WachtwoordGebruiker == password)
        .Select(pbl => new { IdUser = pbl.IdGebruiker, NameUser = pbl.NaamGebruiker, FirstNameUser = pbl.VoorNaamGebruiker })
        .FirstOrDefaultAsync();

    if (user == null)
        return Results.Unauthorized();

    return Results.Ok(user);
});

// POST: Add User
app.MapPost("/AddUser", async (string email, string firstName, string name, string password, bool isStudent, ShiftlyDbContext db) =>
{
    var exists = await db.Gebruikers.AnyAsync(pbl => pbl.EmailGebruiker == email);

    if (exists)
        return Results.Conflict("User already in Database");

    var user = new Gebruiker
    {
        EmailGebruiker = email,
        VoorNaamGebruiker = firstName,
        NaamGebruiker = name,
        WachtwoordGebruiker = password,
        IsStudent = isStudent,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    db.Gebruikers.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"user", user);
});

// POST: Add Subscription

// POST: Add Shift

app.Run();