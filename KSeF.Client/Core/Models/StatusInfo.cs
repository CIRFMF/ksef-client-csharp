﻿namespace KSeFClient.Core.Models;

public class StatusInfo
{
    public int Code { get; set; }
    public string Description { get; set; }
    public ICollection<string> Details { get; set; }
}

public class AuthStatus
{
    public StatusInfo Status { get; set; }
}