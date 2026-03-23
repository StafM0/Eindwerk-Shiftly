using Microsoft.EntityFrameworkCore;
using Shiftly.Models;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Xml.Linq;

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

#region Users
// GET: All Users
app.MapGet("/GetAllUsers", async (ShiftlyDbContext db) =>
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
}).WithTags("Users");

// PUT: Edit User
app.MapPut("/UpdateUser", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(pbl => pbl.IdGebruiker == id);
    if (user == null)
        return Results.NotFound();

    if (updates.TryGetProperty("name", out var name))
        user.NaamGebruiker = name.GetString();

    if (updates.TryGetProperty("firstName", out var firstName))
        user.VoorNaamGebruiker = firstName.GetString();

    if (updates.TryGetProperty("email", out var email))
        user.EmailGebruiker = email.GetString();

    if (updates.TryGetProperty("isStudent", out var isStudent))
        user.IsStudent = isStudent.GetBoolean();

    user.UpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();
    return Results.Ok(user);
}).WithTags("Users");

// PUT: Change Password
app.MapPut("/ChangePassword", async (int userId, string password, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(pbl => pbl.IdGebruiker == userId);
    if (user == null)
        return Results.NotFound();

    user.WachtwoordGebruiker = password;
    user.UpdatedAt = DateTime.Now;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = user.WachtwoordGebruiker });
}).WithTags("Users");

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
}).WithTags("Users");

// DELETE: User
app.MapDelete("/DeleteUser", async (int userId, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers
        .FirstOrDefaultAsync(pbl => pbl.IdGebruiker == userId);

    if (user == null)
        return Results.NotFound();

    db.Gebruikers.Remove(user);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).WithTags("Users");
#endregion

#region Shifts
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
}).WithTags("Shifts");

// PUT: Edit Shift
app.MapPut("/UpdateShift", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var shift = await db.Shifts.FirstOrDefaultAsync(pbl => pbl.IdShift == id);
    if (shift == null)
        return Results.NotFound();

    if (updates.TryGetProperty("startDateTime", out var startDateTime))
        shift.StartDateTime = startDateTime.GetDateTime();

    if (updates.TryGetProperty("endDateTime", out var endDateTime))
        shift.EindDateTime = endDateTime.GetDateTime();

    if (updates.TryGetProperty("function", out var function))
        shift.Functie = function.GetString();

    if (updates.TryGetProperty("pause", out var pause))
        shift.PauzeInMinuten = pause.GetInt32();

    if (updates.TryGetProperty("description", out var description))
        shift.Opmerking = description.GetString();

    if (updates.TryGetProperty("department", out var department))
        shift.FkGebruikerAfdeling = department.GetInt32();

    await db.SaveChangesAsync();
    return Results.Ok(shift);
}).WithTags("Shifts");

// POST: Add Shift
app.MapPost("/AddShift", async (DateTime start, DateTime end, string function, int pause, int fkDepartment, string description, ShiftlyDbContext db) =>
{
    var exists = await db.Shifts.AnyAsync(pbl => pbl.StartDateTime == start);

    if (exists)
        return Results.Conflict("Shift already in Database");

    var shift = new Shift
    {
        StartDateTime = start,
        EindDateTime = end,
        Functie = function,
        PauzeInMinuten = pause,
        FkGebruikerAfdeling = fkDepartment,
        Opmerking = description
    };

    db.Shifts.Add(shift);
    await db.SaveChangesAsync();

    return Results.Created($"subscription", shift);
}).WithTags("Shifts");

// DELETE: Shift
app.MapDelete("/DeleteShift", async (int shiftId, ShiftlyDbContext db) =>
{
    var shift = await db.Shifts
        .FirstOrDefaultAsync(pbl => pbl.IdShift == shiftId);

    if (shift == null)
        return Results.NotFound();

    db.Shifts.Remove(shift);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).WithTags("Shifts");
#endregion

#region Subscriptions
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
}).WithTags("Subscriptions");

// POST: Add Subscription
app.MapPost("/AddSubscription", async (string name, string description, decimal Amount, bool actif, ShiftlyDbContext db) =>
{
    var exists = await db.Abonnements.AnyAsync(pbl => pbl.NaamAbonnement == name);

    if (exists)
        return Results.Conflict("Subscription already in Database");

    var subscription = new Abonnement
    {
        NaamAbonnement = name,
        OmschrijvingAbonnement = description,
        BedragAbonnement = Amount,
        IsActief = actif
    };

    db.Abonnements.Add(subscription);
    await db.SaveChangesAsync();

    return Results.Created($"subscription", subscription);
}).WithTags("Subscriptions");

// DELETE: Subscription
app.MapDelete("/DeleteSubscription", async (int subscriptionId, ShiftlyDbContext db) =>
{
    var subscription = await db.Abonnements
        .FirstOrDefaultAsync(pbl => pbl.IdAbonnement == subscriptionId);

    if (subscription == null)
        return Results.NotFound();

    db.Abonnements.Remove(subscription);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).WithTags("Subscriptions");
#endregion

#region Wishlist Items
// GET: All Wishlist Items
app.MapGet("/GetAllWishListItems", async (ShiftlyDbContext db) =>
{
    var wishlistitem = await db.Wishlistitems.Select(pbl => new
    {
        idwishlist = pbl.IdWishListItem,
        itemName = pbl.ItemNaam,
        itemPrice = pbl.ItemPrijs,
        itemDescription = pbl.ItemOmschrijving,
        itemLink = pbl.ItemLink,
        itemPriority = pbl.Prioriteit,
        itemBought = pbl.Gehaald
    }).ToListAsync();
    return Results.Ok(wishlistitem);
}).WithTags("WishList Items");

// PUT: Edit WishList Item
app.MapPut("/UpdateWishListItem", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FindAsync(id);

    if (item is null)
        return Results.NotFound();

    item.ItemNaam = updates.GetProperty("itemName").GetString();
    item.ItemPrijs = updates.GetProperty("itemPrice").GetDecimal();
    item.ItemOmschrijving = updates.GetProperty("itemDescription").GetString();
    item.ItemLink = updates.GetProperty("itemLink").GetString();
    item.Prioriteit = updates.GetProperty("itemPriority").GetInt32();
    item.Gehaald = updates.GetProperty("itemBought").GetBoolean();

    await db.SaveChangesAsync();
    return Results.Ok(item);
}).WithTags("WishList Items");

// PUT: Change Priority
app.MapPut("/ChangePriority", async (int itemId, int prio, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FirstOrDefaultAsync(pbl => pbl.IdWishListItem == itemId);
    if (item == null)
        return Results.NotFound();

    item.Prioriteit = prio;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = item.Prioriteit });
}).WithTags("WishList Items");

// POST: Add Wishlist Item
app.MapPost("/AddWishlistItem", async (int fk, string name, decimal price, string descrpition, string link, int prio, bool made, ShiftlyDbContext db) =>
{
    var exists = await db.Wishlistitems.AnyAsync(pbl => pbl.ItemLink == link);

    if (exists)
        return Results.Conflict("item already in Database");

    var item = new Wishlistitem
    {
        FkGebruiker = fk,
        ItemNaam = name,
        ItemPrijs = price,
        ItemOmschrijving = descrpition,
        ItemLink = link,
        Prioriteit = prio,
        Gehaald = made
    };

    db.Wishlistitems.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"wishlistitem", item);
}).WithTags("WishList Items");

// DELETE: Wishlist Item
app.MapDelete("/DeleteWishListItem", async (int itemId, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FirstOrDefaultAsync(pbl => pbl.IdWishListItem == itemId);

    if (item == null)
        return Results.NotFound();

    db.Wishlistitems.Remove(item);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).WithTags("WishList Items");
#endregion

#region Other
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
}).WithTags("Other");

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
}).WithTags("Other");

// GET: All Wishlist Items From User
app.MapGet("/GetAllWishListItemsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var wishlistitems = await db.Wishlistitems
        .Where(pbl => pbl.FkGebruiker == userId)
        .Select(pbl => new
        {
            idwishlist = pbl.IdWishListItem,
            itemName = pbl.ItemNaam,
            itemPrice = pbl.ItemPrijs,
            itemDescription = pbl.ItemOmschrijving,
            itemLink = pbl.ItemLink,
            itemPriority = pbl.Prioriteit,
            itemBought = pbl.Gehaald
        })
        .ToListAsync();

    return Results.Ok(wishlistitems);
}).WithTags("Other");

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
}).WithTags("Other");

#endregion

app.Run();