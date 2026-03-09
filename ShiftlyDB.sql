DROP DATABASE IF EXISTS shiftly;
CREATE DATABASE IF NOT EXISTS shiftly;
USE shiftly;

CREATE TABLE Gebruiker (
    IdGebruiker INT AUTO_INCREMENT PRIMARY KEY,
    VoorNaamGebruiker VARCHAR(255) NOT NULL,
    NaamGebruiker VARCHAR(255) NOT NULL,
    EmailGebruiker VARCHAR(255) NOT NULL UNIQUE,
    WachtwoordGebruiker VARCHAR(26) NOT NULL,
    IsStudent BOOLEAN NOT NULL DEFAULT FALSE,
    IsActief BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE Werkplek (
    IdWerkplek INT AUTO_INCREMENT PRIMARY KEY,
    Naam VARCHAR(255) NOT NULL,
    Postcode CHAR(4) NOT NULL,
    Gemeente VARCHAR(255) NOT NULL,
    StraatNr VARCHAR(255) NOT NULL,
    UNIQUE (Postcode, Gemeente, StraatNr)
);

CREATE TABLE Afdeling (
    IdAfdeling INT AUTO_INCREMENT PRIMARY KEY,
    AfdelingNaam VARCHAR(255) NOT NULL,
    FkWerkplek INT NOT NULL,
    UNIQUE (FkWerkplek, AfdelingNaam),
    FOREIGN KEY (FkWerkplek) REFERENCES Werkplek(IdWerkplek)
);

CREATE TABLE GebruikerAfdeling (
    IdGebruikerAfdeling INT AUTO_INCREMENT PRIMARY KEY,
    FkGebruiker INT NOT NULL,
    FkAfdeling INT NOT NULL,
    Uurloon DECIMAL(10,2) NOT NULL,
    Betaaldag TINYINT CHECK (Betaaldag BETWEEN 1 AND 7),
    UNIQUE (FkGebruiker, FkAfdeling),
    FOREIGN KEY (FkGebruiker) REFERENCES Gebruiker(IdGebruiker),
    FOREIGN KEY (FkAfdeling) REFERENCES Afdeling(IdAfdeling)
);

CREATE TABLE Shift (
    IdShift INT AUTO_INCREMENT PRIMARY KEY,
    StartDateTime DATETIME NOT NULL,
    EindDateTime DATETIME NOT NULL,
    Functie VARCHAR(255) NOT NULL,
    PauzeInMinuten INT NOT NULL DEFAULT 0 CHECK (PauzeInMinuten >= 0),
    FkGebruikerAfdeling INT NOT NULL,
    Opmerking VARCHAR(255),
    CHECK (EindDateTime > StartDateTime),
    FOREIGN KEY (FkGebruikerAfdeling) REFERENCES GebruikerAfdeling(IdGebruikerAfdeling)
);

CREATE TABLE Shiftly (
    FkGebruiker INT PRIMARY KEY,
    MaximumStudentUren INT DEFAULT 650 CHECK (MaximumStudentUren >= 0),
    FOREIGN KEY (FkGebruiker) REFERENCES Gebruiker(IdGebruiker)
);

CREATE TABLE Abonnement (
    IdAbonnement INT AUTO_INCREMENT PRIMARY KEY,
    NaamAbonnement VARCHAR(255) NOT NULL UNIQUE,
    OmschrijvingAbonnement TEXT,
    BedragAbonnement DECIMAL(10,2) NOT NULL CHECK (BedragAbonnement >= 0),
    IsActief BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE GebruikerAbonnement (
    FkGebruiker INT NOT NULL,
    FkAbonnement INT NOT NULL,
    Betaald BOOLEAN NOT NULL DEFAULT FALSE,
    PeriodeStart DATE NOT NULL,
    PeriodeEinde DATE NOT NULL,
    CHECK (PeriodeEinde > PeriodeStart),
    PRIMARY KEY (FkGebruiker, FkAbonnement, PeriodeStart),
    FOREIGN KEY (FkGebruiker) REFERENCES Gebruiker(IdGebruiker),
    FOREIGN KEY (FkAbonnement) REFERENCES Abonnement(IdAbonnement)
);

CREATE TABLE WishListItem (
    IdWishListItem INT AUTO_INCREMENT PRIMARY KEY,
    FkGebruiker INT NOT NULL,
    ItemNaam VARCHAR(255) NOT NULL,
    ItemPrijs DECIMAL(10,2) CHECK (ItemPrijs IS NULL OR ItemPrijs >= 0),
    ItemOmschrijving TEXT,
    ItemLink VARCHAR(255),
    Prioriteit INT CHECK (Prioriteit IS NULL OR Prioriteit BETWEEN 1 AND 5),
    Gehaald BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (FkGebruiker) REFERENCES Gebruiker(IdGebruiker)
);