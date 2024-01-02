namespace Memory.Db;

public class User {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
    public int LockoutRetryCount { get; set; }
    public DateTime? LockoutTime { get; set; }
}
