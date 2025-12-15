public class Configuration
{
    public string TfsToken {get;set;}
    public string TfsUri {get;set;}
    public string RootFolder {get;set;}
    public string[] ProjectNames {get;set;}
    public bool DeleteTests { get; set; }
    public bool OnlyCs { get; set; }
}