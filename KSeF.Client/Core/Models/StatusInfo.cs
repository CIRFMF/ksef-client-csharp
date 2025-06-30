namespace KSeFClient.Core.Models;

public class StatusInfo : BasicStatusInfo
{
    public ICollection<string> Details { get; set; }
}

public class BasicStatusInfo
{
    public int Code { get; set; }
    public string Description { get; set; }    
}

public class AuthStatus
{
    public BasicStatusInfo Status { get; set; }
}