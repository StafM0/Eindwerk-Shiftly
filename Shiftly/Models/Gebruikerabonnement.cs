using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Gebruikerabonnement
{
    public int FkGebruiker { get; set; }

    public int FkAbonnement { get; set; }

    public bool Betaald { get; set; }

    public DateOnly PeriodeStart { get; set; }

    public DateOnly PeriodeEinde { get; set; }

    public virtual Abonnement FkAbonnementNavigation { get; set; } = null!;

    public virtual Gebruiker FkGebruikerNavigation { get; set; } = null!;
}
