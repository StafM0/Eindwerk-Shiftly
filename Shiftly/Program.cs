using Microsoft.EntityFrameworkCore;
using Shiftly.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=localhost;Database=Shiftly;User=root;Password=1234;";

builder.Services.AddDbContext<ShiftlyDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── Users ────────────────────────────────────────────────────────────────────

app.MapGet("/GetAllUsers", async (ShiftlyDbContext db) =>
{
    var users = await db.Gebruikers.Select(u => new
    {
        u.IdGebruiker,
        u.VoorNaamGebruiker,
        u.NaamGebruiker,
        u.EmailGebruiker,
        u.IsStudent,
        u.WachtwoordGebruiker,
        u.IsActief,
        u.CreatedAt,
        u.UpdatedAt
    }).ToListAsync();

    return Results.Ok(users);
}).WithTags("Users");

app.MapGet("/GetUserById", async (int id, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers
        .Where(u => u.IdGebruiker == id)
        .Select(u => new
        {
            firstName = u.VoorNaamGebruiker,
            lastName  = u.NaamGebruiker,
            email     = u.EmailGebruiker,
            isStudent = u.IsStudent
        })
        .FirstOrDefaultAsync();

    return user is null ? Results.NotFound() : Results.Ok(user);
}).WithTags("Users");

app.MapPost("/AddUser", async (string email, string firstName, string name, string password, bool isStudent, ShiftlyDbContext db) =>
{
    if (await db.Gebruikers.AnyAsync(u => u.EmailGebruiker == email))
        return Results.Conflict("User already in Database");

    var user = new Gebruiker
    {
        EmailGebruiker    = email,
        VoorNaamGebruiker = firstName,
        NaamGebruiker     = name,
        WachtwoordGebruiker = password,
        IsStudent         = isStudent,
        CreatedAt         = DateTime.Now,
        UpdatedAt         = DateTime.Now
    };

    db.Gebruikers.Add(user);
    await db.SaveChangesAsync();

    return Results.Created("user", user);
}).WithTags("Users");

app.MapPut("/UpdateUser", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(u => u.IdGebruiker == id);
    if (user is null) return Results.NotFound();

    if (updates.TryGetProperty("name",      out var name))      user.NaamGebruiker     = name.GetString();
    if (updates.TryGetProperty("firstName", out var firstName)) user.VoorNaamGebruiker = firstName.GetString();
    if (updates.TryGetProperty("email",     out var email))     user.EmailGebruiker    = email.GetString();
    if (updates.TryGetProperty("isStudent", out var isStudent)) user.IsStudent         = isStudent.GetBoolean();

    user.UpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();
    return Results.Ok(user);
}).WithTags("Users");

app.MapPut("/ChangePassword", async (int userId, string currentPassword, string newPassword, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(u => u.IdGebruiker == userId);
    if (user is null) return Results.NotFound(new { message = "Gebruiker niet gevonden." });

    if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        return Results.BadRequest(new { message = "Vul huidig en nieuw wachtwoord in." });

    if (user.WachtwoordGebruiker != currentPassword)
        return Results.BadRequest(new { message = "Huidig wachtwoord is onjuist." });

    user.WachtwoordGebruiker = newPassword;
    user.UpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Wachtwoord succesvol gewijzigd." });
}).WithTags("Users");

app.MapDelete("/DeleteUser", async (int userId, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers.FirstOrDefaultAsync(u => u.IdGebruiker == userId);
    if (user is null) return Results.NotFound();

    db.Gebruikers.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Users");

// ── Shifts ───────────────────────────────────────────────────────────────────

app.MapGet("/GetAllShifts", async (ShiftlyDbContext db) =>
{
    var shifts = await db.Shifts.Select(s => new
    {
        s.IdShift,
        s.StartDateTime,
        s.EindDateTime,
        s.PauzeInMinuten,
        s.Functie,
        s.Opmerking
    }).ToListAsync();

    return Results.Ok(shifts);
}).WithTags("Shifts");

app.MapPost("/AddShift", async (DateTime start, DateTime end, string function, int pause, int fkDepartment, string description, ShiftlyDbContext db) =>
{
    if (await db.Shifts.AnyAsync(s => s.StartDateTime == start && s.FkGebruikerAfdeling == fkDepartment))
        return Results.Conflict("Shift already in Database");

    var shift = new Shift
    {
        StartDateTime      = start,
        EindDateTime       = end,
        Functie            = function,
        PauzeInMinuten     = pause,
        FkGebruikerAfdeling = fkDepartment,
        Opmerking          = description
    };

    db.Shifts.Add(shift);
    await db.SaveChangesAsync();
    return Results.Created("shift", shift);
}).WithTags("Shifts");

app.MapPut("/UpdateShift", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var shift = await db.Shifts.FirstOrDefaultAsync(s => s.IdShift == id);
    if (shift is null) return Results.NotFound();

    if (updates.TryGetProperty("startDateTime", out var start))      shift.StartDateTime      = start.GetDateTime();
    if (updates.TryGetProperty("endDateTime",   out var end))        shift.EindDateTime       = end.GetDateTime();
    if (updates.TryGetProperty("function",      out var function))   shift.Functie            = function.GetString() ?? string.Empty;
    if (updates.TryGetProperty("pause",         out var pause))      shift.PauzeInMinuten     = pause.GetInt32();
    if (updates.TryGetProperty("description",   out var desc))       shift.Opmerking          = desc.GetString();
    if (updates.TryGetProperty("department",    out var department)) shift.FkGebruikerAfdeling = department.GetInt32();

    await db.SaveChangesAsync();
    return Results.Ok(shift);
}).WithTags("Shifts");

app.MapDelete("/DeleteShift", async (int shiftId, ShiftlyDbContext db) =>
{
    var shift = await db.Shifts.FirstOrDefaultAsync(s => s.IdShift == shiftId);
    if (shift is null) return Results.NotFound();

    db.Shifts.Remove(shift);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Shifts");

// ── Subscriptions ─────────────────────────────────────────────────────────────

app.MapGet("/GetAllSubscriptions", async (ShiftlyDbContext db) =>
{
    var subs = await db.Abonnements
        .OrderBy(a => a.NaamAbonnement)
        .Select(a => new
        {
            a.IdAbonnement,
            a.NaamAbonnement,
            a.OmschrijvingAbonnement,
            a.BedragAbonnement,
            a.IsActief
        })
        .ToListAsync();

    return Results.Ok(subs);
}).WithTags("Subscriptions");

app.MapGet("/GetSubscriptionCatalog", async (ShiftlyDbContext db) =>
{
    var catalog = await db.Abonnements
        .Where(a => a.IsActief != false)
        .OrderBy(a => a.NaamAbonnement)
        .Select(a => new
        {
            AbonnementId = a.IdAbonnement,
            Naam         = a.NaamAbonnement,
            Omschrijving = a.OmschrijvingAbonnement,
            Prijs        = a.BedragAbonnement,
            a.IsActief
        })
        .ToListAsync();

    return Results.Ok(catalog);
}).WithTags("Subscriptions");

app.MapGet("/GetAllSubscriptionsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var subs = await db.Gebruikerabonnements
        .Where(ga => ga.FkGebruiker == userId)
        .Include(ga => ga.FkAbonnementNavigation)
        .OrderBy(ga => ga.PeriodeEinde)
        .Select(ga => new
        {
            AbonnementId = ga.FkAbonnement,
            Naam         = ga.FkAbonnementNavigation.NaamAbonnement,
            Beschrijving = ga.FkAbonnementNavigation.OmschrijvingAbonnement,
            Prijs        = ga.FkAbonnementNavigation.BedragAbonnement,
            ga.PeriodeStart,
            ga.PeriodeEinde,
            ga.Betaald,
            IsActief     = ga.FkAbonnementNavigation.IsActief
        })
        .ToListAsync();

    return Results.Ok(subs);
}).WithTags("Subscriptions");

app.MapPost("/AddSubscription", async (string name, string description, decimal amount, bool actif, ShiftlyDbContext db) =>
{
    if (await db.Abonnements.AnyAsync(a => a.NaamAbonnement == name))
        return Results.Conflict("Subscription already in Database");

    var sub = new Abonnement
    {
        NaamAbonnement         = name,
        OmschrijvingAbonnement = description,
        BedragAbonnement       = amount,
        IsActief               = actif
    };

    db.Abonnements.Add(sub);
    await db.SaveChangesAsync();
    return Results.Created("subscription", sub);
}).WithTags("Subscriptions");

app.MapPost("/SaveUserSubscription", async (SaveUserSubscriptionRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Gebruiker ontbreekt." });

    if (!DateOnly.TryParse(request.PeriodeStart, out var periodeStart))
        return Results.BadRequest(new { message = "Periode-start is ongeldig." });

    if (!DateOnly.TryParse(request.PeriodeEinde, out var periodeEinde))
        return Results.BadRequest(new { message = "Periode-einde is ongeldig." });

    if (periodeEinde < periodeStart)
        return Results.BadRequest(new { message = "Periode-einde moet na periode-start liggen." });

    DateOnly? originalPeriodeStart = null;
    if (!string.IsNullOrWhiteSpace(request.OriginalPeriodeStart))
    {
        if (!DateOnly.TryParse(request.OriginalPeriodeStart, out var parsed))
            return Results.BadRequest(new { message = "Originele startdatum is ongeldig." });

        originalPeriodeStart = parsed;
    }

    Abonnement? abonnement;

    if (request.ExistingAbonnementId > 0)
    {
        abonnement = await db.Abonnements.FirstOrDefaultAsync(a => a.IdAbonnement == request.ExistingAbonnementId);
        if (abonnement is null)
            return Results.NotFound(new { message = "Abonnement niet gevonden." });

        abonnement.BedragAbonnement       = request.Prijs;
        abonnement.OmschrijvingAbonnement = request.Omschrijving ?? string.Empty;
        abonnement.IsActief               = request.IsActief;
    }
    else
    {
        if (string.IsNullOrWhiteSpace(request.Naam))
            return Results.BadRequest(new { message = "Naam van het abonnement ontbreekt." });

        var cleanedName = request.Naam.Trim();
        abonnement = await db.Abonnements.FirstOrDefaultAsync(a => a.NaamAbonnement == cleanedName);

        if (abonnement is null)
        {
            abonnement = new Abonnement
            {
                NaamAbonnement         = cleanedName,
                OmschrijvingAbonnement = request.Omschrijving ?? string.Empty,
                BedragAbonnement       = request.Prijs,
                IsActief               = request.IsActief
            };
            db.Abonnements.Add(abonnement);
            await db.SaveChangesAsync();
        }
        else
        {
            abonnement.BedragAbonnement       = request.Prijs;
            abonnement.OmschrijvingAbonnement = request.Omschrijving ?? string.Empty;
            abonnement.IsActief               = request.IsActief;
        }
    }

    Gebruikerabonnement? userSub;

    if (request.IsEditMode)
    {
        if (request.OriginalAbonnementId <= 0 || string.IsNullOrWhiteSpace(request.OriginalPeriodeStart))
            return Results.BadRequest(new { message = "Originele abonnementsleutel ontbreekt." });

        userSub = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
            ga.FkGebruiker  == request.UserId &&
            ga.FkAbonnement == request.OriginalAbonnementId &&
            ga.PeriodeStart == originalPeriodeStart!.Value);

        if (userSub is null)
            return Results.NotFound(new { message = "Gebruikersabonnement niet gevonden." });

        var duplicate = await db.Gebruikerabonnements.AnyAsync(ga =>
            ga.FkGebruiker  == request.UserId &&
            ga.FkAbonnement == abonnement.IdAbonnement &&
            ga.PeriodeStart == periodeStart &&
            !(ga.FkAbonnement == request.OriginalAbonnementId && ga.PeriodeStart == originalPeriodeStart.Value));

        if (duplicate)
            return Results.Conflict(new { message = "Er bestaat al een abonnement met dezelfde startdatum." });

        userSub.FkAbonnement = abonnement.IdAbonnement;
        userSub.PeriodeStart = periodeStart;
        userSub.PeriodeEinde = periodeEinde;
        userSub.Betaald      = request.Betaald;
    }
    else
    {
        if (await db.Gebruikerabonnements.AnyAsync(ga =>
                ga.FkGebruiker  == request.UserId &&
                ga.FkAbonnement == abonnement.IdAbonnement &&
                ga.PeriodeStart == periodeStart))
            return Results.Conflict(new { message = "Dit abonnement bestaat al met deze startdatum." });

        userSub = new Gebruikerabonnement
        {
            FkGebruiker  = request.UserId,
            FkAbonnement = abonnement.IdAbonnement,
            Betaald      = request.Betaald,
            PeriodeStart = periodeStart,
            PeriodeEinde = periodeEinde
        };
        db.Gebruikerabonnements.Add(userSub);
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        AbonnementId = abonnement.IdAbonnement,
        Naam         = abonnement.NaamAbonnement,
        Beschrijving = abonnement.OmschrijvingAbonnement,
        Prijs        = abonnement.BedragAbonnement,
        userSub.PeriodeStart,
        userSub.PeriodeEinde,
        userSub.Betaald,
        IsActief     = abonnement.IsActief
    });
}).WithTags("Subscriptions");

app.MapDelete("/DeleteSubscription", async (int subscriptionId, ShiftlyDbContext db) =>
{
    var sub = await db.Abonnements.FirstOrDefaultAsync(a => a.IdAbonnement == subscriptionId);
    if (sub is null) return Results.NotFound();

    db.Abonnements.Remove(sub);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Subscriptions");

app.MapDelete("/DeleteUserSubscription", async (int userId, int abonnementId, DateOnly periodeStart, ShiftlyDbContext db) =>
{
    var item = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
        ga.FkGebruiker  == userId &&
        ga.FkAbonnement == abonnementId &&
        ga.PeriodeStart == periodeStart);

    if (item is null) return Results.NotFound();

    db.Gebruikerabonnements.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Subscriptions");

app.MapPut("/SetUserSubscriptionPaidStatus", async (int userId, int abonnementId, DateOnly periodeStart, bool betaald, ShiftlyDbContext db) =>
{
    var item = await db.Gebruikerabonnements.FirstOrDefaultAsync(ga =>
        ga.FkGebruiker  == userId &&
        ga.FkAbonnement == abonnementId &&
        ga.PeriodeStart == periodeStart);

    if (item is null) return Results.NotFound();

    item.Betaald = betaald;
    await db.SaveChangesAsync();

    return Results.Ok(new { item.FkAbonnement, item.PeriodeStart, item.Betaald });
}).WithTags("Subscriptions");

// ── Wishlist Items ────────────────────────────────────────────────────────────

app.MapGet("/GetAllWishListItems", async (ShiftlyDbContext db) =>
{
    var items = await db.Wishlistitems.Select(w => new
    {
        idwishlist      = w.IdWishListItem,
        itemName        = w.ItemNaam,
        itemPrice       = w.ItemPrijs,
        itemDescription = w.ItemOmschrijving,
        itemLink        = w.ItemLink,
        itemPriority    = w.Prioriteit,
        itemBought      = w.Gehaald
    }).ToListAsync();

    return Results.Ok(items);
}).WithTags("WishList Items");

app.MapPost("/AddWishListItem", async (int userId, string name, string description, decimal price, string link, string prio, bool made, ShiftlyDbContext db) =>
{
    var item = new Wishlistitem
    {
        FkGebruiker     = userId,
        ItemNaam        = name,
        ItemOmschrijving = description,
        ItemPrijs       = price,
        ItemLink        = link,
        Prioriteit      = int.TryParse(prio, out var parsedPrio) ? parsedPrio : null,
        Gehaald         = made
    };

    db.Wishlistitems.Add(item);
    await db.SaveChangesAsync();
    return Results.Created("wishlistitem", item);
}).WithTags("WishList Items");

app.MapPut("/UpdateWishListItem", async (int id, JsonElement updates, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FindAsync(id);
    if (item is null) return Results.NotFound();

    if (updates.TryGetProperty("name",        out var name))     item.ItemNaam        = name.GetString() ?? string.Empty;
    if (updates.TryGetProperty("description", out var desc))     item.ItemOmschrijving = desc.GetString();
    if (updates.TryGetProperty("price",       out var price))    item.ItemPrijs       = price.GetDecimal();
    if (updates.TryGetProperty("link",        out var link))     item.ItemLink        = link.GetString();
    if (updates.TryGetProperty("bought",      out var bought))   item.Gehaald         = bought.GetBoolean();
    if (updates.TryGetProperty("priority",    out var priority))
        item.Prioriteit = priority.ValueKind == JsonValueKind.Number
            ? priority.GetInt32()
            : int.TryParse(priority.GetString(), out var p) ? p : null;

    await db.SaveChangesAsync();
    return Results.Ok(item);
}).WithTags("WishList Items");

app.MapDelete("/DeleteWishListItem", async (int itemId, ShiftlyDbContext db) =>
{
    var item = await db.Wishlistitems.FirstOrDefaultAsync(w => w.IdWishListItem == itemId);
    if (item is null) return Results.NotFound();

    db.Wishlistitems.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("WishList Items");

// ── Other / Cross-entity ──────────────────────────────────────────────────────

app.MapGet("/login", async (string email, string password, ShiftlyDbContext db) =>
{
    var user = await db.Gebruikers
        .Where(u => u.EmailGebruiker == email && u.WachtwoordGebruiker == password)
        .Select(u => new { IdUser = u.IdGebruiker, NameUser = u.NaamGebruiker, FirstNameUser = u.VoorNaamGebruiker })
        .FirstOrDefaultAsync();

    return user is null ? Results.Unauthorized() : Results.Ok(user);
}).WithTags("Other");

app.MapGet("/GetAllShiftsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var shifts = await db.Shifts
        .Include(s => s.FkGebruikerAbbonomentNavigation)
            .ThenInclude(ga => ga.FkAfdelingNavigation)
        .Where(s => s.FkGebruikerAbbonomentNavigation.FkGebruiker == userId)
        .Select(s => new
        {
            ShiftId             = s.IdShift,
            s.StartDateTime,
            s.EindDateTime,
            s.PauzeInMinuten,
            s.Functie,
            s.Opmerking,
            Uurloon             = s.FkGebruikerAbbonomentNavigation.Uurloon,
            Betaaldag           = s.FkGebruikerAbbonomentNavigation.Betaaldag,
            GebruikerAfdelingId = s.FkGebruikerAfdeling,
            AfdelingId          = s.FkGebruikerAbbonomentNavigation.FkAfdeling,
            AfdelingNaam        = s.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.AfdelingNaam,
            WerkplekId          = s.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.FkWerkplek,
            WerkplekNaam        = s.FkGebruikerAbbonomentNavigation.FkAfdelingNavigation.FkWerkplekNavigation.Naam
        })
        .ToListAsync();

    return Results.Ok(shifts);
}).WithTags("Other");

app.MapGet("/GetUserHoursOverview", async (int userId, ShiftlyDbContext db) =>
{
    var now          = DateTime.Now;
    var startOfYear  = new DateTime(now.Year, 1, 1);
    var startOfNext  = startOfYear.AddYears(1);

    var maxUren = await db.Shiftlies
        .Where(s => s.FkGebruiker == userId)
        .Select(s => s.MaximumStudentUren ?? 650)
        .FirstOrDefaultAsync();

    if (maxUren <= 0) maxUren = 650;

    var shifts = await db.Shifts
        .Where(s => s.FkGebruikerAbbonomentNavigation.FkGebruiker == userId
                 && s.StartDateTime >= startOfYear
                 && s.StartDateTime < startOfNext)
        .Select(s => new { s.StartDateTime, s.EindDateTime, s.PauzeInMinuten })
        .ToListAsync();

    decimal gewerkteUren = 0, geplandeUren = 0;

    foreach (var s in shifts)
    {
        var netto = Math.Max(0, (decimal)(s.EindDateTime - s.StartDateTime).TotalHours - s.PauzeInMinuten / 60m);
        if (s.EindDateTime <= now) gewerkteUren += netto;
        else                       geplandeUren += netto;
    }

    return Results.Ok(new
    {
        Jaar        = now.Year,
        ResetDatum  = startOfNext.ToString("yyyy-MM-dd"),
        MaximumUren = maxUren,
        GewerkteUren = Math.Round(gewerkteUren, 2),
        GeplandeUren = Math.Round(geplandeUren, 2),
        OverigeUren  = Math.Round(Math.Max(0, maxUren - gewerkteUren - geplandeUren), 2)
    });
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
            AfdelingId          = ga.FkAfdeling,
            AfdelingNaam        = ga.FkAfdelingNavigation.AfdelingNaam,
            ga.Uurloon,
            ga.Betaaldag,
            WerkplekId          = ga.FkAfdelingNavigation.FkWerkplek,
            WerkplekNaam        = ga.FkAfdelingNavigation.FkWerkplekNavigation.Naam
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
            WerkplekId   = w.IdWerkplek,
            WerkplekNaam = w.Naam,
            w.Gemeente,
            w.Postcode,
            w.StraatNr
        })
        .ToListAsync();

    return Results.Ok(workplaces);
}).WithTags("Other");

app.MapGet("/GetAllWishListItemsFromUser", async (int userId, ShiftlyDbContext db) =>
{
    var items = await db.Wishlistitems
        .Where(w => w.FkGebruiker == userId)
        .Select(w => new
        {
            idwishlist      = w.IdWishListItem,
            itemName        = w.ItemNaam,
            itemPrice       = w.ItemPrijs,
            itemDescription = w.ItemOmschrijving,
            itemLink        = w.ItemLink,
            itemPriority    = w.Prioriteit,
            itemBought      = w.Gehaald
        })
        .ToListAsync();

    return Results.Ok(items);
}).WithTags("Other");

app.MapPost("/CreateWorkplace", async (CreateWorkplaceRequest request, ShiftlyDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Naam)     ||
        string.IsNullOrWhiteSpace(request.Postcode) ||
        string.IsNullOrWhiteSpace(request.Gemeente) ||
        string.IsNullOrWhiteSpace(request.StraatNr))
        return Results.BadRequest(new { message = "Vul alle werkplekvelden in." });

    var naam     = request.Naam.Trim();
    var postcode = request.Postcode.Trim();
    var gemeente = request.Gemeente.Trim();
    var straatNr = request.StraatNr.Trim();

    var existing = await db.Werkpleks.FirstOrDefaultAsync(w =>
        w.Naam     == naam &&
        w.Postcode == postcode &&
        w.Gemeente == gemeente &&
        w.StraatNr == straatNr);

    if (existing is not null)
        return Results.Ok(ToWorkplaceDto(existing));

    var workplace = new Werkplek { Naam = naam, Postcode = postcode, Gemeente = gemeente, StraatNr = straatNr };
    db.Werkpleks.Add(workplace);
    await db.SaveChangesAsync();

    return Results.Ok(ToWorkplaceDto(workplace));

    static object ToWorkplaceDto(Werkplek w) => new
    {
        WerkplekId   = w.IdWerkplek,
        WerkplekNaam = w.Naam,
        w.Postcode,
        w.Gemeente,
        w.StraatNr
    };
}).WithTags("Other");

app.MapPost("/CreateDepartmentForUser", async (CreateDepartmentForUserRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Ongeldige gebruiker." });

    if (string.IsNullOrWhiteSpace(request.AfdelingNaam))
        return Results.BadRequest(new { message = "Afdelingsnaam mag niet leeg zijn." });

    if (request.WerkplekId <= 0)
        return Results.BadRequest(new { message = "Kies eerst een werkplek." });

    var trimmedName = request.AfdelingNaam.Trim();

    var existing = await db.Gebruikerafdelings
        .Include(ga => ga.FkAfdelingNavigation)
        .FirstOrDefaultAsync(ga =>
            ga.FkGebruiker == request.UserId &&
            ga.FkAfdelingNavigation.FkWerkplek == request.WerkplekId &&
            ga.FkAfdelingNavigation.AfdelingNaam == trimmedName);

    if (existing is not null)
        return Results.Ok(new
        {
            GebruikerAfdelingId = existing.IdGebruikerAfdeling,
            AfdelingId          = existing.FkAfdeling,
            AfdelingNaam        = existing.FkAfdelingNavigation.AfdelingNaam,
            existing.Uurloon
        });

    if (!await db.Werkpleks.AnyAsync(w => w.IdWerkplek == request.WerkplekId))
        return Results.BadRequest(new { message = "De gekozen werkplek bestaat niet." });

    var fallback = await db.Gebruikerafdelings
        .Where(ga => ga.FkGebruiker == request.UserId)
        .OrderByDescending(ga => ga.IdGebruikerAfdeling)
        .FirstOrDefaultAsync();

    var afdeling = await db.Afdelings
        .FirstOrDefaultAsync(a => a.FkWerkplek == request.WerkplekId && a.AfdelingNaam == trimmedName);

    if (afdeling is null)
    {
        afdeling = new Afdeling { AfdelingNaam = trimmedName, FkWerkplek = request.WerkplekId };
        db.Afdelings.Add(afdeling);
        await db.SaveChangesAsync();
    }

    var userDept = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId && ga.FkAfdeling == afdeling.IdAfdeling);

    if (userDept is null)
    {
        userDept = new Gebruikerafdeling
        {
            FkGebruiker = request.UserId,
            FkAfdeling  = afdeling.IdAfdeling,
            Uurloon     = request.Uurloon ?? fallback?.Uurloon ?? 0,
            Betaaldag   = request.Betaaldag ?? fallback?.Betaaldag
        };
        db.Gebruikerafdelings.Add(userDept);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        GebruikerAfdelingId = userDept.IdGebruikerAfdeling,
        AfdelingId          = afdeling.IdAfdeling,
        AfdelingNaam        = afdeling.AfdelingNaam,
        userDept.Uurloon,
        WerkplekId          = afdeling.FkWerkplek
    });
}).WithTags("Other");

app.MapPost("/ResolveDepartmentForShift", async (ResolveDepartmentForShiftRequest request, ShiftlyDbContext db) =>
{
    if (request.UserId <= 0)
        return Results.BadRequest(new { message = "Ongeldige gebruiker." });

    if (request.Uurloon < 0)
        return Results.BadRequest(new { message = "Uurloon mag niet negatief zijn." });

    // ── Path A: resolve by existing GebruikerAfdeling id ──────────────────────
    if (request.ExistingGebruikerAfdelingId > 0)
    {
        var current = await db.Gebruikerafdelings
            .Include(ga => ga.FkAfdelingNavigation)
            .FirstOrDefaultAsync(ga =>
                ga.IdGebruikerAfdeling == request.ExistingGebruikerAfdelingId &&
                ga.FkGebruiker         == request.UserId);

        if (current is null)
            return Results.BadRequest(new { message = "De gekozen afdeling bestaat niet voor deze gebruiker." });

        var betaaldag = request.Betaaldag ?? current.Betaaldag;

        if (request.MakeDefaultUurloon)
        {
            current.Uurloon   = request.Uurloon;
            current.Betaaldag = betaaldag;
            await db.SaveChangesAsync();
            return Results.Ok(ToDeptDto(current, current.FkAfdelingNavigation));
        }

        if (current.Uurloon == request.Uurloon && current.Betaaldag == betaaldag)
            return Results.Ok(ToDeptDto(current, current.FkAfdelingNavigation));

        var variant = await db.Gebruikerafdelings
            .Include(ga => ga.FkAfdelingNavigation)
            .FirstOrDefaultAsync(ga =>
                ga.FkGebruiker == request.UserId &&
                ga.FkAfdeling  == current.FkAfdeling &&
                ga.Uurloon     == request.Uurloon &&
                ga.Betaaldag   == betaaldag);

        if (variant is null)
        {
            variant = new Gebruikerafdeling
            {
                FkGebruiker = request.UserId,
                FkAfdeling  = current.FkAfdeling,
                Uurloon     = request.Uurloon,
                Betaaldag   = betaaldag
            };
            db.Gebruikerafdelings.Add(variant);
            await db.SaveChangesAsync();
            await db.Entry(variant).Reference(x => x.FkAfdelingNavigation).LoadAsync();
        }

        return Results.Ok(ToDeptDto(variant, variant.FkAfdelingNavigation));
    }

    // ── Path B: resolve by workplace + department name ─────────────────────────
    if (request.WerkplekId <= 0)
        return Results.BadRequest(new { message = "Kies eerst een werkplek." });

    if (string.IsNullOrWhiteSpace(request.AfdelingNaam))
        return Results.BadRequest(new { message = "Afdelingsnaam mag niet leeg zijn." });

    if (!await db.Werkpleks.AnyAsync(w => w.IdWerkplek == request.WerkplekId))
        return Results.BadRequest(new { message = "De gekozen werkplek bestaat niet." });

    var afdelingNaam = request.AfdelingNaam.Trim();

    var afdeling = await db.Afdelings
        .FirstOrDefaultAsync(a => a.FkWerkplek == request.WerkplekId && a.AfdelingNaam == afdelingNaam);

    if (afdeling is null)
    {
        afdeling = new Afdeling { AfdelingNaam = afdelingNaam, FkWerkplek = request.WerkplekId };
        db.Afdelings.Add(afdeling);
        await db.SaveChangesAsync();
    }

    var sameVariant = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga =>
            ga.FkGebruiker == request.UserId &&
            ga.FkAfdeling  == afdeling.IdAfdeling &&
            ga.Uurloon     == request.Uurloon &&
            ga.Betaaldag   == request.Betaaldag);

    if (sameVariant is not null)
        return Results.Ok(ToDeptNameDto(sameVariant, afdeling));

    var firstForDept = await db.Gebruikerafdelings
        .FirstOrDefaultAsync(ga => ga.FkGebruiker == request.UserId && ga.FkAfdeling == afdeling.IdAfdeling);

    if (firstForDept is not null && request.MakeDefaultUurloon)
    {
        firstForDept.Uurloon   = request.Uurloon;
        firstForDept.Betaaldag = request.Betaaldag;
        await db.SaveChangesAsync();
        return Results.Ok(ToDeptNameDto(firstForDept, afdeling));
    }

    var newUserDept = new Gebruikerafdeling
    {
        FkGebruiker = request.UserId,
        FkAfdeling  = afdeling.IdAfdeling,
        Uurloon     = request.Uurloon,
        Betaaldag   = request.Betaaldag
    };
    db.Gebruikerafdelings.Add(newUserDept);
    await db.SaveChangesAsync();

    return Results.Ok(ToDeptNameDto(newUserDept, afdeling));

    static object ToDeptDto(Gebruikerafdeling ga, Afdeling a) => new
    {
        GebruikerAfdelingId = ga.IdGebruikerAfdeling,
        AfdelingId          = ga.FkAfdeling,
        AfdelingNaam        = a.AfdelingNaam,
        WerkplekId          = a.FkWerkplek,
        ga.Uurloon,
        ga.Betaaldag
    };

    static object ToDeptNameDto(Gebruikerafdeling ga, Afdeling a) => new
    {
        GebruikerAfdelingId = ga.IdGebruikerAfdeling,
        AfdelingId          = ga.FkAfdeling,
        AfdelingNaam        = a.AfdelingNaam,
        WerkplekId          = a.FkWerkplek,
        ga.Uurloon,
        ga.Betaaldag
    };
}).WithTags("Other");

app.Run();

// ── Request records ───────────────────────────────────────────────────────────

public sealed class SaveUserSubscriptionRequest
{
    public int     UserId              { get; set; }
    public bool    IsEditMode          { get; set; }
    public int?    OriginalAbonnementId { get; set; }
    public string? OriginalPeriodeStart { get; set; }
    public int     ExistingAbonnementId { get; set; }
    public string? Naam                { get; set; }
    public string? Omschrijving        { get; set; }
    public decimal Prijs               { get; set; }
    public bool    IsActief            { get; set; } = true;
    public bool    Betaald             { get; set; }
    public string  PeriodeStart        { get; set; } = string.Empty;
    public string  PeriodeEinde        { get; set; } = string.Empty;
}

public sealed class CreateDepartmentForUserRequest
{
    public int     UserId       { get; set; }
    public int     WerkplekId   { get; set; }
    public string  AfdelingNaam { get; set; } = string.Empty;
    public decimal? Uurloon     { get; set; }
    public sbyte?  Betaaldag    { get; set; }
}

public sealed class ResolveDepartmentForShiftRequest
{
    public int     UserId                    { get; set; }
    public int     ExistingGebruikerAfdelingId { get; set; }
    public int     WerkplekId               { get; set; }
    public string? AfdelingNaam             { get; set; }
    public decimal Uurloon                  { get; set; }
    public bool    MakeDefaultUurloon       { get; set; }
    public sbyte?  Betaaldag                { get; set; }
}

public sealed class CreateWorkplaceRequest
{
    public string Naam     { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Gemeente { get; set; } = string.Empty;
    public string StraatNr { get; set; } = string.Empty;
}
