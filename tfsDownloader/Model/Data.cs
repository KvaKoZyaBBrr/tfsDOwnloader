class Data
{
    public List<Unit> units { get; set; }
}

class Unit
{
    public string collection { get; set; }
    public string project { get; set; }
    public string branch { get; set; }
    public Definition definition { get; set; }
}

class Definition
{
    public string name { get; set; }
}