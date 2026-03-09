using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Werkplek
{
    public int IdWerkplek { get; set; }

    public string Naam { get; set; } = null!;

    public string Postcode { get; set; } = null!;

    public string Gemeente { get; set; } = null!;

    public string StraatNr { get; set; } = null!;

    public virtual ICollection<Afdeling> Afdelings { get; set; } = new List<Afdeling>();
}
