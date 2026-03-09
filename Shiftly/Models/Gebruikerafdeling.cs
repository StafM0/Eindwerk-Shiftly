using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Gebruikerafdeling
{
    public int IdGebruikerAfdeling { get; set; }

    public int FkGebruiker { get; set; }

    public int FkAfdeling { get; set; }

    public decimal Uurloon { get; set; }

    public sbyte? Betaaldag { get; set; }

    public virtual Afdeling FkAfdelingNavigation { get; set; } = null!;

    public virtual Gebruiker FkGebruikerNavigation { get; set; } = null!;

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
