using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Abonnement
{
    public int IdAbonnement { get; set; }

    public string NaamAbonnement { get; set; } = null!;

    public string? OmschrijvingAbonnement { get; set; }

    public decimal BedragAbonnement { get; set; }

    public bool? IsActief { get; set; }

    public virtual ICollection<Gebruikerabonnement> Gebruikerabonnements { get; set; } = new List<Gebruikerabonnement>();
}
