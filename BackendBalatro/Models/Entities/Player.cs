using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class Player : IPlayer
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "Player 1";

    public Player()
    {
    }

    public Player(string name)
    {
        Name = name;
    }

    public Player(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
