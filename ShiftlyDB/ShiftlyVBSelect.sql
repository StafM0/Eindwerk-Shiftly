USE shiftly;

-- =========================
-- 1. OVERZICHT GEBRUIKERS
-- =========================
SELECT 
    IdGebruiker,
    VoorNaamGebruiker,
    NaamGebruiker,
    EmailGebruiker,
    IsStudent,
    IsActief,
    CreatedAt
FROM Gebruiker;


-- =========================
-- 2. GEBRUIKER + AFDELING + WERKPLEK
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    a.AfdelingNaam,
    w.Naam AS Werkplek,
    ga.Uurloon,
    ga.Betaaldag
FROM GebruikerAfdeling ga
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
JOIN Afdeling a ON ga.FkAfdeling = a.IdAfdeling
JOIN Werkplek w ON a.FkWerkplek = w.IdWerkplek
ORDER BY g.IdGebruiker;


-- =========================
-- 3. ALLE SHIFTS MET INFO
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    a.AfdelingNaam,
    w.Naam AS Werkplek,
    s.Functie,
    s.StartDateTime,
    s.EindDateTime,
    s.PauzeInMinuten,
    s.Opmerking
FROM Shift s
JOIN GebruikerAfdeling ga ON s.FkGebruikerAfdeling = ga.IdGebruikerAfdeling
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
JOIN Afdeling a ON ga.FkAfdeling = a.IdAfdeling
JOIN Werkplek w ON a.FkWerkplek = w.IdWerkplek
ORDER BY s.StartDateTime;


-- =========================
-- 4. GEWERKTE UREN PER SHIFT
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    s.StartDateTime,
    s.EindDateTime,
    ROUND(
        (TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60,
        2
    ) AS GewerkteUren
FROM Shift s
JOIN GebruikerAfdeling ga ON s.FkGebruikerAfdeling = ga.IdGebruikerAfdeling
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker;


-- =========================
-- 5. TOTAAL UREN + LOON PER GEBRUIKER
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    ROUND(SUM(
        (TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60
    ),2) AS TotaalUren,

    ROUND(SUM(
        ((TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60)
        * ga.Uurloon
    ),2) AS TotaalLoon
FROM Shift s
JOIN GebruikerAfdeling ga ON s.FkGebruikerAfdeling = ga.IdGebruikerAfdeling
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
GROUP BY g.IdGebruiker;


-- =========================
-- 6. STUDENTENUREN VS LIMIET
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    sl.MaximumStudentUren,

    ROUND(SUM(
        (TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60
    ),2) AS GebruikteUren,

    sl.MaximumStudentUren - ROUND(SUM(
        (TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60
    ),2) AS Resterend
FROM Shiftly sl
JOIN Gebruiker g ON sl.FkGebruiker = g.IdGebruiker
LEFT JOIN GebruikerAfdeling ga ON g.IdGebruiker = ga.FkGebruiker
LEFT JOIN Shift s ON ga.IdGebruikerAfdeling = s.FkGebruikerAfdeling
GROUP BY g.IdGebruiker;


-- =========================
-- 7. ABONNEMENTEN + STATUS
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    a.NaamAbonnement,
    ga.Betaald,
    ga.PeriodeStart,
    ga.PeriodeEinde
FROM GebruikerAbonnement ga
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
JOIN Abonnement a ON ga.FkAbonnement = a.IdAbonnement
ORDER BY g.IdGebruiker;


-- =========================
-- 8. NIET-BETAALDE ABONNEMENTEN
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    a.NaamAbonnement,
    ga.PeriodeStart,
    ga.PeriodeEinde
FROM GebruikerAbonnement ga
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
JOIN Abonnement a ON ga.FkAbonnement = a.IdAbonnement
WHERE ga.Betaald = FALSE;


-- =========================
-- 9. WISHLIST PER GEBRUIKER
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    w.ItemNaam,
    w.ItemPrijs,
    w.Prioriteit,
    w.Gehaald
FROM WishListItem w
JOIN Gebruiker g ON w.FkGebruiker = g.IdGebruiker
ORDER BY g.IdGebruiker, w.Prioriteit DESC;


-- =========================
-- 10. SHIFTS PER MAAND
-- =========================
SELECT
    g.VoorNaamGebruiker,
    g.NaamGebruiker,
    YEAR(s.StartDateTime) AS Jaar,
    MONTH(s.StartDateTime) AS Maand,
    COUNT(*) AS AantalShifts
FROM Shift s
JOIN GebruikerAfdeling ga ON s.FkGebruikerAfdeling = ga.IdGebruikerAfdeling
JOIN Gebruiker g ON ga.FkGebruiker = g.IdGebruiker
GROUP BY g.IdGebruiker, Jaar, Maand
ORDER BY Jaar, Maand;


-- =========================
-- 11. TOTAAL PER WERKPLEK
-- =========================
SELECT
    w.Naam AS Werkplek,
    COUNT(s.IdShift) AS AantalShifts,
    ROUND(SUM(
        (TIMESTAMPDIFF(MINUTE, s.StartDateTime, s.EindDateTime) - s.PauzeInMinuten)/60
    ),2) AS TotaalUren
FROM Shift s
JOIN GebruikerAfdeling ga ON s.FkGebruikerAfdeling = ga.IdGebruikerAfdeling
JOIN Afdeling a ON ga.FkAfdeling = a.IdAfdeling
JOIN Werkplek w ON a.FkWerkplek = w.IdWerkplek
GROUP BY w.IdWerkplek;
