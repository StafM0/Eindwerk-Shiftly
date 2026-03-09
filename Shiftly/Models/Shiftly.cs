using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Shiftly
{
    public int FkGebruiker { get; set; }

    public int? MaximumStudentUren { get; set; }

    public virtual Gebruiker FkGebruikerNavigation { get; set; } = null!;
}
