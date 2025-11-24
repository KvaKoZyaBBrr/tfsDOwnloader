class Data
{
    public List<Unit> Units { get; set; }
}

class Unit
{
    public string Collection { get; set; }
    public string Project { get; set; }
    public string Branch { get; set; }
    public string RepositoryName { get; set; }
    public Definition Definition { get; set; }
}

class Definition
{
    public string Name { get; set; }
}