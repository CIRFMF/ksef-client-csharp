﻿
namespace KSeF.Client.Core.Models.Sessions;

public class Status
{
    public int Code { get; set; }
    public string Description { get; set; }
    public ICollection<string> Details { get; set; }
}
