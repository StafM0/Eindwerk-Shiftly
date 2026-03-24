using Microsoft.EntityFrameworkCore;
using Shiftly.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var connectionString =
"Server=localhost;Database=Shiflty;User=root;Password=1234;";
builder.Services.AddDbContext<ShiftlyDbContext>(options =>
options.UseMySql(connectionString,
ServerVersion.AutoDetect(connectionString)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region Users
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

app.MapGet("/GetUserById", async (int id, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers
        .Where(pbl => pbl.IdGebruiker == id)
        .Select(pbl => new
        {
            firstName = pbl.VoorNaamGebruiker,
            lastName = pbl.NaamGebruiker,
            email = pbl.EmailGebruiker,
            isStudent = pbl.IsStudent
        })
        .FirstOrDefaultAsync();

    if (user == null)
        return Results.NotFound();

    return Results.Ok(user);
}).WithTags("Users");

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

app.MapPut("/ChangePassword", async (int userId, string currentPassword, string newPassword, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(pbl => pbl.IdGebruiker == userId);

    if (user == null)
        return Results.NotFound(new { message = "Gebruiker niet gevonden." });

    if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        return Results.BadRequest(new { message = "Vul huidig en nieuw wachtwoord in." });

    if (user.WachtwoordGebruiker != currentPassword)
        return Results.BadRequest(new { message = "Huidig wachtwoord is onjuist." });

    user.WachtwoordGebruiker = newPassword;
    user.UpdatedAt = DateTime.Now;

    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Wachtwoord succesvol gewijzigd." });
}).WithTags("Users");

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
        shift.Functie = function.GetString() ?? string.Empty;

    if (updates.TryGetProperty("pause", out var pause))
        shift.PauzeInMinuten = pause.GetInt32();

    if (updates.TryGetProperty("description", out var description))
        shift.Opmerking = description.GetString();

    if (updates.TryGetProperty("department", out var department))
        shift.FkGebruikerAfdeling = department.GetInt32();

    await db.SaveChangesAsync();
    return Results.Ok(shift);
}).WithTags("Shifts");

app.MapPost("/AddShift", async (DateTime start, DateTime end, string function, int pause, int fkDepartment, string description, ShiftlyDbContext db) =>
{
    var exists = await db.Shifts.AnyAsync(pbl => pbl.StartDateTime == start && pbl.FkGebruikerAfdeling == fkDepartment);

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
app.MapGet("/GetAllSubscriptions", async (ShiftlyDbContext db) =>
{
    var subscriptions = await db.Abonnements
        .Select(pbl => new
        {
            idAbonnement = pbl.IdAbonnement,
            NaamAbonnement = pbl.NaamAbonnement,
            OmschrijvingAbonnement = pbl.OmschrijvingAbonnement,
            BedragAbonnement = pbl.BedragAbonnement,
            IsActief = pbl.IsActief
        })
        .OrderBy(pbl => pbl.NaamAbonnement)
        .ToListAsync();

    return Results.Ok(subscriptions);
}).WithTags("Subscriptions");

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

    return Results.Created("subscription", subscription);
}).WithTags("Subscriptions");

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

app.MapGet("/GetSubscriptionCatalog", async (ShiftlyDbContext db) =>
{
    var subscriptions = await db.Abonnements
        .Where(a => a.IsActief != false)
        .OrderBy(a => a.NaamAbonnement)
        .Select(a => new
        {
            AbonnementId = a.IdAbonnement,
            Naam = a.NaamAbonnement,
            Omschrijving = a.OmschrijvingAbonnement,
            Prijs = a.BedragAbonnement,
            IsActief = a.IsActief
        })
        .ToListAsync();

    return Results.Ok(subscriptions);
}).WithTags("Subscriptions");

app.MapGet("/GetAllSubscriptionsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var subscriptions = await db.Gebruikerabonnements
        .Where(ga => ga.FkGebruiker == userId)
        .Include(ga => ga.FkAbonnementNavigation)
        .OrderBy(ga => ga.PeriodeEinde)
        .Select(ga => new
        {
            AbonnementId = ga.FkAbonnement,
            Naam = ga.FkAbonnementNavigation.NaamAbonnement,
            Beschrijving = ga.FkAbonnementNavigation.OmschrijvingAbonnement,
            Prijs = ga.FkAbonnementNavigation.BedragAbonnement,
            PeriodeStart = ga.PeriodeStart,
            PeriodeEinde = ga.PeriodeEinde,
            Betaald = ga.Betaald,
            IsActief = ga.FkAbonnementNavigation.IsActief
        })
        .ToListAsync();

    return Results.Ok(subscriptions);
}).WithTags("Subscriptions");

app.MapPost("/SaveUserSubscription", async (SaveUserSubscriptionRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Gebruiker ontbreekt." });

    if (!DateOnly.TryParse(request.PeriodeStart, out var periodeStart))
        return Results.BadRequest(new { message = "Periode-start is ongeldig." });

    if (!DateOnly.TryParse(request.PeriodeEinde, out var periodeEinde))
        return Results.BadRequest(new { message = "Periode-einde is ongeldig." });

    DateOnly? originalPeriodeStart = null;
    if (!string.IsNullOrWhiteSpace(request.OriginalPeriodeStart))
    {
        if (!DateOnly.TryParse(request.OriginalPeriodeStart, out var parsedOriginalPeriodeStart))
            return Results.BadRequest(new { message = "Originele startdatum is ongeldig." });

        originalPeriodeStart = parsedOriginalPeriodeStart;
    }

    if (periodeEinde < periodeStart)
        return Results.BadRequest(new { message = "Periode-einde moet na periode-start liggen." });

    Abonnement? abonnement = null;

    if (request.ExistingAbonnementId > 0)
    {
        abonnement = await db.Abonnements.FirstOrDefaultAsync(a => a.IdAbonnement == request.ExistingAbonnementId);

        if (abonnement == null)
            return Results.NotFound(new { message = "Abonnement niet gevonden." });

        abonnement.BedragAbonnement = request.Prijs;
        abonnement.OmschrijvingAbonnement = request.Omschrijving ?? string.Empty;
        abonnement.IsActief = request.IsActief;
    }
    else
    {
        if (string.IsNullOrWhiteSpace(request.Naam))
            return Results.BadRequest(new { message = "Naam van het abonnement ontbreekt." });

        var cleanedName = request.Naam.Trim();

        abonnement = await db.Abonnements.FirstOrDefaultAsync(a => a.NaamAbonnement == cleanedName);

        if (abonnement == null)
        {
            abonnement = new Abonnement
            {
                NaamAbonnement = cleanedName,
                OmschrijvingAbonnement = request.Omschrijving ?? string.Empty,
                BedragAbonnement = request.Prijs,
                IsActief = request.IsActief
            };

            db.Abonnements.Add(abonnement);
            await db.SaveChangesAsync();
        }
        else
        {
            abonnement.BedragAbonnement = request.Prijs;
            abonnement.OmschrijvingAbonnement = request.Omschrijving ?? string.Empty;
            abonnement.IsActief = request.IsActief;
        }
    }

    Gebruikerabonnement? userSubscription;

    if (request.IsEditMode)
    {
        if (request.OriginalAbonnementId <= 0 || string.IsNullOrWhiteSpace(request.OriginalPeriodeStart))
            return Results.BadRequest(new { message = "Originele abonnementsleutel ontbreekt." });

        userSubscription = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
            ga.FkGebruiker == request.UserId &&
            ga.FkAbonnement == request.OriginalAbonnementId &&
            ga.PeriodeStart == originalPeriodeStart!.Value);

        if (userSubscription == null)
            return Results.NotFound(new { message = "Gebruikersabonnement niet gevonden." });

        var duplicate = await db.Gebruikerabonnements.AnyAsync(ga =>
            ga.FkGebruiker == request.UserId &&
            ga.FkAbonnement == abonnement.IdAbonnement &&
            ga.PeriodeStart == periodeStart &&
            !(ga.FkAbonnement == request.OriginalAbonnementId && ga.PeriodeStart == originalPeriodeStart.Value));

        if (duplicate)
            return Results.Conflict(new { message = "Er bestaat al een abonnement met dezelfde startdatum." });

        userSubscription.FkAbonnement = abonnement.IdAbonnement;
        userSubscription.PeriodeStart = periodeStart;
        userSubscription.PeriodeEinde = periodeEinde;
        userSubscription.Betaald = request.Betaald;
    }
    else
    {
        var duplicate = await db.Gebruikerabonnements.AnyAsync(ga =>
            ga.FkGebruiker == request.UserId &&
            ga.FkAbonnement == abonnement.IdAbonnement &&
            ga.PeriodeStart == periodeStart);

        if (duplicate)
            return Results.Conflict(new { message = "Dit abonnement bestaat al met deze startdatum." });

        userSubscription = new Gebruikerabonnement
        {
            FkGebruiker = request.UserId,
            FkAbonnement = abonnement.IdAbonnement,
            Betaald = request.Betaald,
            PeriodeStart = periodeStart,
            PeriodeEinde = periodeEinde
        };

        db.Gebruikerabonnements.Add(userSubscription);
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        AbonnementId = abonnement.IdAbonnement,
        Naam = abonnement.NaamAbonnement,
        Beschrijving = abonnement.OmschrijvingAbonnement,
        Prijs = abonnement.BedragAbonnement,
        PeriodeStart = userSubscription.PeriodeStart,
        PeriodeEinde = userSubscription.PeriodeEinde,
        Betaald = userSubscription.Betaald,
        IsActief = abonnement.IsActief
    });
}).WithTags("Subscriptions");

app.MapDelete("/DeleteUserSubscription", async (int userId, int abonnementId, DateOnly periodeStart, ShiftlyDbContext db) =>
{
    var item = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
        ga.FkGebruiker == userId &&
        ga.FkAbonnement == abonnementId &&
        ga.PeriodeStart == periodeStart);

    if (item == null)
        return Results.NotFound();

    db.Gebruikerabonnements.Remove(item);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).WithTags("Subscriptions");

app.MapPut("/SetUserSubscriptionPaidStatus", async (int userId, int abonnementId, DateOnly periodeStart, bool betaald, ShiftlyDbContext db) =>
{
    var item = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
        ga.FkGebruiker == userId &&
        ga.FkAbonnement == abonnementId &&
        ga.PeriodeStart == periodeStart);

    if (item == null)
        return Results.NotFound();

    item.Betaald = betaald;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        item.FkAbonnement,
        item.PeriodeStart,
        item.Betaald
    });
}).WithTags("Subscriptions");
#endregion

#region Wishlist Items
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

app.MapPut("/UpdateWishListItem", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FindAsync(id);

    if (item == null)
        return Results.NotFound();

    if (updates.TryGetProperty("name", out var name))
        item.ItemNaam = name.GetString() ?? string.Empty;

    if (updates.TryGetProperty("description", out var description))
        item.ItemOmschrijving = description.GetString();

    if (updates.TryGetProperty("price", out var price))
        item.ItemPrijs = price.GetDecimal();

    if (updates.TryGetProperty("link", out var link))
        item.ItemLink = link.GetString();

    if (updates.TryGetProperty("priority", out var priority))
        item.Prioriteit = priority.ValueKind == JsonValueKind.Number ? priority.GetInt32() : int.TryParse(priority.GetString(), out var parsedPriority) ? parsedPriority : null;

    if (updates.TryGetProperty("bought", out var bought))
        item.Gehaald = bought.GetBoolean();

    await db.SaveChangesAsync();
    return Results.Ok(item);
}).WithTags("WishList Items");

app.MapPost("/AddWishListItem", async (int userId, string name, string description, decimal price, string link, string prio, bool made, ShiftlyDbContext db) =>
{
    var item = new Wishlistitem
    {
        FkGebruiker = userId,
        ItemNaam = name,
        ItemOmschrijving = description,
        ItemPrijs = price,
        ItemLink = link,
        Prioriteit = int.TryParse(prio, out var parsedPriority) ? parsedPriority : null,
        Gehaald = made
    };

    db.Wishlistitems.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"wishlistitem", item);
}).WithTags("WishList Items");

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
app.MapGet("/GetAllShiftsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var shifts = await db.Shifts
        .Include(pbl => pbl.FkGebruikerAbbonomentNavigation)
            .ThenInclude(ga => ga.FkAfdelingNavigation)
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
            Betaaldag = pbl.FkGebruikerAbbonomentNavigation.Betaaldag,
            GebruikerAfdelingId = pbl.FkGebruikerAfdeling,
            AfdelingId = pbl.FkGebruikerAbbonomentNavigation.FkAfdeling,
            AfdelingNaam = pbl.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.AfdelingNaam,
            WerkplekId = pbl.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.FkWerkplek,
            WerkplekNaam = pbl.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.FkWerkplekNavigation.Naam
        })
        .ToListAsync();

    return Results.Ok(shifts);
}).WithTags("Other");

app.MapGet("/GetDepartmentsForUser", async (int userId, ShiftlyDbContext db) =>
{
    var departments = await db.Gebruikerafdelings
        .Where(ga => ga.FkGebruiker == userId)
        .Include(ga => ga.FkAfdelingNavigation)
            .ThenInclude(a => a.FkWerkplekNavigation)
        .OrderBy(ga => ga.FkAfdelingNavigation.FkWerkplekNavigation.Naam)
        .ThenBy(ga => ga.FkAfdelingNavigation.AfdelingNaam)
        .Select(ga => new
        {
            GebruikerAfdelingId = ga.IdGebruikerAfdeling,
            AfdelingId = ga.FkAfdeling,
            AfdelingNaam = ga.FkAfdelingNavigation.AfdelingNaam,
            Uurloon = ga.Uurloon,
            Betaaldag = ga.Betaaldag,
            WerkplekId = ga.FkAfdelingNavigation.FkWerkplek,
            WerkplekNaam = ga.FkAfdelingNavigation.FkWerkplekNavigation.Naam
        })
        .ToListAsync();

    return Results.Ok(departments);
}).WithTags("Other");

app.MapGet("/GetAllWorkplaces", async (ShiftlyDbContext db) =>
{
    var workplaces = await db.Werkpleks
        .OrderBy(w => w.Naam)
        .Select(w => new
        {
            WerkplekId = w.IdWerkplek,
            WerkplekNaam = w.Naam,
            Gemeente = w.Gemeente,
            Postcode = w.Postcode,
            StraatNr = w.StraatNr
        })
        .ToListAsync();

    return Results.Ok(workplaces);
}).WithTags("Other");


app.MapPost("/CreateWorkplace", async (CreateWorkplaceRequest request, ShiftlyDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Naam) ||
        string.IsNullOrWhiteSpace(request.Postcode) ||
        string.IsNullOrWhiteSpace(request.Gemeente) ||
        string.IsNullOrWhiteSpace(request.StraatNr))
    {
        return Results.BadRequest(new { message = "Vul alle werkplekvelden in." });
    }

    var naam = request.Naam.Trim();
    var postcode = request.Postcode.Trim();
    var gemeente = request.Gemeente.Trim();
    var straatNr = request.StraatNr.Trim();

    var existing = await db.Werkpleks.FirstOrDefaultAsync(w =>
        w.Naam == naam &&
        w.Postcode == postcode &&
        w.Gemeente == gemeente &&
        w.StraatNr == straatNr);

    if (existing != null)
    {
        return Results.Ok(new
        {
            WerkplekId = existing.IdWerkplek,
            WerkplekNaam = existing.Naam,
            Postcode = existing.Postcode,
            Gemeente = existing.Gemeente,
            StraatNr = existing.StraatNr
        });
    }

    var workplace = new Werkplek
    {
        Naam = naam,
        Postcode = postcode,
        Gemeente = gemeente,
        StraatNr = straatNr
    };

    db.Werkpleks.Add(workplace);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        WerkplekId = workplace.IdWerkplek,
        WerkplekNaam = workplace.Naam,
        Postcode = workplace.Postcode,
        Gemeente = workplace.Gemeente,
        StraatNr = workplace.StraatNr
    });
}).WithTags("Other");
app.MapPost("/CreateDepartmentForUser", async (CreateDepartmentForUserRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Ongeldige gebruiker." });

    if (string.IsNullOrWhiteSpace(request.AfdelingNaam))
        return Results.BadRequest(new { message = "Afdelingsnaam mag niet leeg zijn." });

    var trimmedName = request.AfdelingNaam.Trim();

    if (request.WerkplekId <= 0)
        return Results.BadRequest(new { message = "Kies eerst een werkplek." });

    var existingUserDepartment = await db.Gebruikerafdelings
        .Include(ga => ga.FkAfdelingNavigation)
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId
            && ga.FkAfdelingNavigation.FkWerkplek == request.WerkplekId
            && ga.FkAfdelingNavigation.AfdelingNaam == trimmedName);

    if (existingUserDepartment != null)
    {
        return Results.Ok(new
        {
            GebruikerAfdelingId = existingUserDepartment.IdGebruikerAfdeling,
            AfdelingId = existingUserDepartment.FkAfdeling,
            AfdelingNaam = existingUserDepartment.FkAfdelingNavigation.AfdelingNaam,
            Uurloon = existingUserDepartment.Uurloon
        });
    }

    var fallbackUserDepartment = await db.Gebruikerafdelings
        .Include(ga => ga.FkAfdelingNavigation)
        .Where(ga => ga.FkGebruiker == request.UserId)
        .OrderByDescending(ga => ga.IdGebruikerAfdeling)
        .FirstOrDefaultAsync();

    var workplaceExists = await db.Werkpleks.AnyAsync(w => w.IdWerkplek == request.WerkplekId);
    if (!workplaceExists)
        return Results.BadRequest(new { message = "De gekozen werkplek bestaat niet." });

    var afdeling = await db.Afdelings
        .FirstOrDefaultAsync(a => a.FkWerkplek == request.WerkplekId && a.AfdelingNaam == trimmedName);

    if (afdeling == null)
    {
        afdeling = new Afdeling
        {
            AfdelingNaam = trimmedName,
            FkWerkplek = request.WerkplekId
        };

        db.Afdelings.Add(afdeling);
        await db.SaveChangesAsync();
    }

    var gebruikerAfdeling = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId && ga.FkAfdeling == afdeling.IdAfdeling);

    if (gebruikerAfdeling == null)
    {
        gebruikerAfdeling = new Gebruikerafdeling
        {
            FkGebruiker = request.UserId,
            FkAfdeling = afdeling.IdAfdeling,
            Uurloon = request.Uurloon ?? fallbackUserDepartment?.Uurloon ?? 0,
            Betaaldag = request.Betaaldag ?? fallbackUserDepartment?.Betaaldag
        };

        db.Gebruikerafdelings.Add(gebruikerAfdeling);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        GebruikerAfdelingId = gebruikerAfdeling.IdGebruikerAfdeling,
        AfdelingId = afdeling.IdAfdeling,
        AfdelingNaam = afdeling.AfdelingNaam,
        Uurloon = gebruikerAfdeling.Uurloon,
        WerkplekId = afdeling.FkWerkplek
    });
}).WithTags("Other");

app.MapPost("/ResolveDepartmentForShift", async (ResolveDepartmentForShiftRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Ongeldige gebruiker." });

    if (request.Uurloon < 0)
        return Results.BadRequest(new { message = "Uurloon mag niet negatief zijn." });

    if (request.ExistingGebruikerAfdelingId > 0)
    {
        var current = await db.Gebruikerafdelings
            .Include(ga => ga.FkAfdelingNavigation)
            .FirstOrDefaultAsync(ga => ga.IdGebruikerAfdeling == request.ExistingGebruikerAfdelingId && ga.FkGebruiker == request.UserId);

        if (current == null)
            return Results.BadRequest(new { message = "De gekozen afdeling bestaat niet voor deze gebruiker." });

        var betaaldag = request.Betaaldag ?? current.Betaaldag;

        if (request.MakeDefaultUurloon)
        {
            current.Uurloon = request.Uurloon;
            current.Betaaldag = betaaldag;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                GebruikerAfdelingId = current.IdGebruikerAfdeling,
                AfdelingId = current.FkAfdeling,
                AfdelingNaam = current.FkAfdelingNavigation.AfdelingNaam,
                WerkplekId = current.FkAfdelingNavigation.FkWerkplek,
                Uurloon = current.Uurloon,
                Betaaldag = current.Betaaldag
            });
        }

        if (current.Uurloon == request.Uurloon && current.Betaaldag == betaaldag)
        {
            return Results.Ok(new
            {
                GebruikerAfdelingId = current.IdGebruikerAfdeling,
                AfdelingId = current.FkAfdeling,
                AfdelingNaam = current.FkAfdelingNavigation.AfdelingNaam,
                WerkplekId = current.FkAfdelingNavigation.FkWerkplek,
                Uurloon = current.Uurloon,
                Betaaldag = current.Betaaldag
            });
        }

        var existingVariant = await db.Gebruikerafdelings
            .Include(ga => ga.FkAfdelingNavigation)
            .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId
                && ga.FkAfdeling == current.FkAfdeling
                && ga.Uurloon == request.Uurloon
                && ga.Betaaldag == betaaldag);

        if (existingVariant == null)
        {
            existingVariant = new Gebruikerafdeling
            {
                FkGebruiker = request.UserId,
                FkAfdeling = current.FkAfdeling,
                Uurloon = request.Uurloon,
                Betaaldag = betaaldag
            };

            db.Gebruikerafdelings.Add(existingVariant);
            await db.SaveChangesAsync();
            await db.Entry(existingVariant).Reference(x => x.FkAfdelingNavigation).LoadAsync();
        }

        return Results.Ok(new
        {
            GebruikerAfdelingId = existingVariant.IdGebruikerAfdeling,
            AfdelingId = existingVariant.FkAfdeling,
            AfdelingNaam = existingVariant.FkAfdelingNavigation.AfdelingNaam,
            WerkplekId = existingVariant.FkAfdelingNavigation.FkWerkplek,
            Uurloon = existingVariant.Uurloon,
            Betaaldag = existingVariant.Betaaldag
        });
    }

    if (request.WerkplekId <= 0)
        return Results.BadRequest(new { message = "Kies eerst een werkplek." });

    if (string.IsNullOrWhiteSpace(request.AfdelingNaam))
        return Results.BadRequest(new { message = "Afdelingsnaam mag niet leeg zijn." });

    var afdelingNaam = request.AfdelingNaam.Trim();
    var betaaldagNieuw = request.Betaaldag;

    var workplaceExists = await db.Werkpleks.AnyAsync(w => w.IdWerkplek == request.WerkplekId);
    if (!workplaceExists)
        return Results.BadRequest(new { message = "De gekozen werkplek bestaat niet." });

    var afdeling = await db.Afdelings
        .FirstOrDefaultAsync(a => a.FkWerkplek == request.WerkplekId && a.AfdelingNaam == afdelingNaam);

    if (afdeling == null)
    {
        afdeling = new Afdeling
        {
            AfdelingNaam = afdelingNaam,
            FkWerkplek = request.WerkplekId
        };

        db.Afdelings.Add(afdeling);
        await db.SaveChangesAsync();
    }

    var sameVariant = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId
            && ga.FkAfdeling == afdeling.IdAfdeling
            && ga.Uurloon == request.Uurloon
            && ga.Betaaldag == betaaldagNieuw);

    if (sameVariant != null)
    {
        return Results.Ok(new
        {
            GebruikerAfdelingId = sameVariant.IdGebruikerAfdeling,
            AfdelingId = sameVariant.FkAfdeling,
            AfdelingNaam = afdeling.AfdelingNaam,
            WerkplekId = afdeling.FkWerkplek,
            Uurloon = sameVariant.Uurloon,
            Betaaldag = sameVariant.Betaaldag
        });
    }

    var firstForDepartment = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId && ga.FkAfdeling == afdeling.IdAfdeling);

    if (firstForDepartment != null && request.MakeDefaultUurloon)
    {
        firstForDepartment.Uurloon = request.Uurloon;
        firstForDepartment.Betaaldag = betaaldagNieuw;
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            GebruikerAfdelingId = firstForDepartment.IdGebruikerAfdeling,
            AfdelingId = firstForDepartment.FkAfdeling,
            AfdelingNaam = afdeling.AfdelingNaam,
            WerkplekId = afdeling.FkWerkplek,
            Uurloon = firstForDepartment.Uurloon,
            Betaaldag = firstForDepartment.Betaaldag
        });
    }

    var gebruikerAfdeling = new Gebruikerafdeling
    {
        FkGebruiker = request.UserId,
        FkAfdeling = afdeling.IdAfdeling,
        Uurloon = request.Uurloon,
        Betaaldag = betaaldagNieuw
    };

    db.Gebruikerafdelings.Add(gebruikerAfdeling);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        GebruikerAfdelingId = gebruikerAfdeling.IdGebruikerAfdeling,
        AfdelingId = gebruikerAfdeling.FkAfdeling,
        AfdelingNaam = afdeling.AfdelingNaam,
        WerkplekId = afdeling.FkWerkplek,
        Uurloon = gebruikerAfdeling.Uurloon,
        Betaaldag = gebruikerAfdeling.Betaaldag
    });
}).WithTags("Other");

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

public sealed class SaveUserSubscriptionRequest
{
    public int UserId { get; set; }
    public bool IsEditMode { get; set; }
    public int? OriginalAbonnementId { get; set; }
    public string? OriginalPeriodeStart { get; set; }
    public int ExistingAbonnementId { get; set; }
    public string? Naam { get; set; }
    public string? Omschrijving { get; set; }
    public decimal Prijs { get; set; }
    public bool IsActief { get; set; } = true;
    public bool Betaald { get; set; }
    public string PeriodeStart { get; set; } = string.Empty;
    public string PeriodeEinde { get; set; } = string.Empty;
}

public sealed class CreateDepartmentForUserRequest
{
    public int UserId { get; set; }
    public int WerkplekId { get; set; }
    public string AfdelingNaam { get; set; } = string.Empty;
    public decimal? Uurloon { get; set; }
    public sbyte? Betaaldag { get; set; }
}


public sealed class ResolveDepartmentForShiftRequest
{
    public int UserId { get; set; }
    public int ExistingGebruikerAfdelingId { get; set; }
    public int WerkplekId { get; set; }
    public string? AfdelingNaam { get; set; }
    public decimal Uurloon { get; set; }
    public bool MakeDefaultUurloon { get; set; }
    public sbyte? Betaaldag { get; set; }
}

public class CreateWorkplaceRequest
{
    public string Naam { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Gemeente { get; set; } = string.Empty;
    public string StraatNr { get; set; } = string.Empty;
}
