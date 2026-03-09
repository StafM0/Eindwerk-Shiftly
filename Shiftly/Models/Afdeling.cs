using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Afdeling
{
    public int IdAfdeling { get; set; }

    public string AfdelingNaam { get; set; } = null!;

    public int FkWerkplek { get; set; }

    public virtual Werkplek FkWerkplekNavigation { get; set; } = null!;

    public virtual ICollection<Gebruikerafdeling> Gebruikerafdelings { get; set; } = new List<Gebruikerafdeling>();
}
