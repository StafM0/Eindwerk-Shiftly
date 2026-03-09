-- =========================
-- SHIFTLY - TESTDATA INSERTS
-- =========================

USE shiftly;

-- (optioneel) eerst leegmaken zodat je opnieuw kan runnen zonder errors
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE Shift;
TRUNCATE TABLE GebruikerAfdeling;
TRUNCATE TABLE Afdeling;
TRUNCATE TABLE Werkplek;
TRUNCATE TABLE GebruikerAbonnement;
TRUNCATE TABLE Abonnement;
TRUNCATE TABLE WishListItem;
TRUNCATE TABLE Shiftly;
TRUNCATE TABLE Gebruiker;
SET FOREIGN_KEY_CHECKS = 1;

-- -------------------------
-- GEBRUIKER
-- -------------------------
INSERT INTO Gebruiker (VoorNaamGebruiker, NaamGebruiker, EmailGebruiker, WachtwoordGebruiker, IsStudent, IsActief)
VALUES
('Staf',  'Moorkens',  'staf.moorkens@shiftly.test',  'Test1234!', TRUE,  TRUE),
('Zina',  'Eerdekens', 'zina.eerdekens@shiftly.test','Test1234!', FALSE, TRUE),
('Arne',  'Janssens',  'arne.janssens@shiftly.test',  'Test1234!', FALSE, TRUE),
('Mika',  'Peeters',   'mika.peeters@shiftly.test',   'Test1234!', TRUE,  TRUE),
('Dries', 'Claes',     'dries.claes@shiftly.test',    'Test1234!', FALSE, FALSE);

-- -------------------------
-- WERKPLEK
-- -------------------------
INSERT INTO Werkplek (Naam, Postcode, Gemeente, StraatNr)
VALUES
('NetConnect Zandhoven', '2240', 'Zandhoven', 'Dorpsstraat 12'),
('NetConnect Pulderbos', '2242', 'Pulderbos', 'Industrieweg 5'),
('KOSH Campus',          '2200', 'Herentals', 'Schoolstraat 1');

-- -------------------------
-- AFDELING
-- -------------------------
INSERT INTO Afdeling (AfdelingNaam, FkWerkplek)
VALUES
('IT',            1),
('Wi-Fi',         1),
('Netwerk',       2),
('Support',       2),
('Administratie', 3);

-- -------------------------
-- GEBRUIKERAFDELING (koppelt gebruiker aan afdeling + loon + betaaldag)
-- Betaaldag: 1-7 (bv 5 = vrijdag)
-- -------------------------
INSERT INTO GebruikerAfdeling (FkGebruiker, FkAfdeling, Uurloon, Betaaldag)
VALUES
(1, 1, 14.50, 5),  -- Staf -> IT (Zandhoven)
(1, 2, 14.50, 5),  -- Staf -> Wi-Fi (Zandhoven)
(2, 5, 16.00, 1),  -- Zina -> Administratie (KOSH)
(3, 1, 21.00, 5),  -- Arne -> IT
(4, 4, 15.20, 3),  -- Mika -> Support (Pulderbos)
(5, 3, 20.00, 5);  -- Dries -> Netwerk (Pulderbos) (niet actief als gebruiker, maar mag nog gelinkt zijn)

-- -------------------------
-- SHIFT (FkGebruikerAfdeling verwijst naar IdGebruikerAfdeling, dus gebaseerd op de insert-volgorde hierboven)
-- IdGebruikerAfdeling verwacht:
-- 1=Staf-IT, 2=Staf-WiFi, 3=Zina-Admin, 4=Arne-IT, 5=Mika-Support, 6=Dries-Netwerk
-- -------------------------
INSERT INTO Shift (StartDateTime, EindDateTime, Functie, PauzeInMinuten, FkGebruikerAfdeling, Opmerking)
VALUES
('2026-02-03 09:00:00', '2026-02-03 17:00:00', 'IT Stage',        30, 1, 'FortiGate VLANs nagekeken'),
('2026-02-04 08:30:00', '2026-02-04 16:30:00', 'Wi-Fi Survey',    45, 2, 'Ekahau validation site survey'),
('2026-02-05 10:00:00', '2026-02-05 14:00:00', 'Administratie',   15, 3, 'Facturen ingegeven'),
('2026-02-06 09:00:00', '2026-02-06 18:00:00', 'Software',        30, 4, 'API endpoints getest'),
('2026-02-07 12:00:00', '2026-02-07 20:00:00', 'Helpdesk',        20, 5, 'Tickets + printers'),
('2026-02-08 07:00:00', '2026-02-08 15:00:00', 'Netwerkbeheer',   30, 6, 'Switch config + patching');

-- Extra shifts om te testen (overuren, verschillende dagen, pauze 0)
INSERT INTO Shift (StartDateTime, EindDateTime, Functie, PauzeInMinuten, FkGebruikerAfdeling, Opmerking)
VALUES
('2026-02-09 16:00:00', '2026-02-09 20:00:00', 'Wi-Fi', 0, 2, 'Extra metingen na sluiting'),
('2026-01-28 09:00:00', '2026-01-28 13:00:00', 'IT',   15, 1, NULL);

-- -------------------------
-- SHIFTLY (alleen per gebruiker 1 record)
-- -------------------------
INSERT INTO Shiftly (FkGebruiker, MaximumStudentUren)
VALUES
(1, 650),  -- Staf student
(4, 600);  -- Mika student

-- -------------------------
-- ABONNEMENT
-- -------------------------
INSERT INTO Abonnement (NaamAbonnement, OmschrijvingAbonnement, BedragAbonnement, IsActief)
VALUES
('Free',      'Basisfunctionaliteit: shifts bijhouden', 0.00,  TRUE),
('Pro',       'Extra rapporten + export',              4.99,  TRUE),
('Student+',  'Student features + limieten',           2.49,  TRUE),
('Legacy',    'Oud abonnement (niet meer actief)',     3.49,  FALSE);

-- -------------------------
-- GEBRUIKERABONNEMENT
-- PK = (FkGebruiker, FkAbonnement, PeriodeStart)
-- -------------------------
INSERT INTO GebruikerAbonnement (FkGebruiker, FkAbonnement, Betaald, PeriodeStart, PeriodeEinde)
VALUES
(1, 3, TRUE,  '2026-02-01', '2026-02-28'),  -- Staf -> Student+
(2, 2, TRUE,  '2026-02-01', '2026-02-28'),  -- Zina -> Pro
(3, 2, FALSE, '2026-02-01', '2026-02-28'),  -- Arne -> Pro (niet betaald)
(4, 1, TRUE,  '2026-01-01', '2026-01-31'),  -- Mika -> Free
(4, 3, TRUE,  '2026-02-01', '2026-02-28');  -- Mika -> Student+

-- -------------------------
-- WISHLISTITEM
-- -------------------------
INSERT INTO WishListItem (FkGebruiker, ItemNaam, ItemPrijs, ItemOmschrijving, ItemLink, Prioriteit, Gehaald)
VALUES
(1, 'Noise-cancelling headphones', 199.99, 'Voor focus tijdens studeren/werken', 'https://example.com/headphones', 5, FALSE),
(1, 'Nieuwe rugzak',                79.95, 'Laptop-proof + waterdicht',          'https://example.com/backpack', 4, TRUE),
(2, 'iPad toetsenbord',            129.00, 'Voor notities en school',            'https://example.com/keyboard', 3, FALSE),
(3, 'Monitor 27 inch',             249.00, 'Thuiswerk setup',                    NULL,                             4, FALSE),
(4, 'Treinticket Leuven',           NULL,  'Nog prijs opzoeken',                 NULL,                             2, FALSE);
