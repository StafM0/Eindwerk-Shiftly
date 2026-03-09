using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Gebruiker
{
    public int IdGebruiker { get; set; }

    public string VoorNaamGebruiker { get; set; } = null!;

    public string NaamGebruiker { get; set; } = null!;

    public string EmailGebruiker { get; set; } = null!;

    public string WachtwoordGebruiker { get; set; } = null!;

    public bool IsStudent { get; set; }

    public bool? IsActief { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Gebruikerabonnement> Gebruikerabonnements { get; set; } = new List<Gebruikerabonnement>();

    public virtual ICollection<Gebruikerafdeling> Gebruikerafdelings { get; set; } = new List<Gebruikerafdeling>();

    public virtual Shiftly? Shiftly { get; set; }

    public virtual ICollection<Wishlistitem> Wishlistitems { get; set; } = new List<Wishlistitem>();
}
