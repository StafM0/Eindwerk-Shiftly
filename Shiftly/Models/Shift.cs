using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Shift
{
    public int IdShift { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EindDateTime { get; set; }

    public string Functie { get; set; } = null!;

    public int PauzeInMinuten { get; set; }

    public int FkGebruikerAfdeling { get; set; }

    public string? Opmerking { get; set; }

    public virtual Gebruikerafdeling FkGebruikerAbbonomentNavigation { get; set; } = null!;
}
