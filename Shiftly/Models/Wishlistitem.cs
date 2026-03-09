using System;
using System.Collections.Generic;

namespace Shiftly.Models;

public partial class Wishlistitem
{
    public int IdWishListItem { get; set; }

    public int FkGebruiker { get; set; }

    public string ItemNaam { get; set; } = null!;

    public decimal? ItemPrijs { get; set; }

    public string? ItemOmschrijving { get; set; }

    public string? ItemLink { get; set; }

    public int? Prioriteit { get; set; }

    public bool Gehaald { get; set; }

    public virtual Gebruiker FkGebruikerNavigation { get; set; } = null!;
}
