using Microsoft.EntityFrameworkCore;
using System.Text.Json;

await using var ctx = new ReproContext();
await ctx.Database.EnsureDeletedAsync();
await ctx.Database.EnsureCreatedAsync();

Console.WriteLine(ctx.Database.GenerateCreateScript());
Console.WriteLine();

ctx.Blogs.Add(new() { Name = "FooBlog", TagsList = ["tag 1", "tag 2"], TagsArray = ["tag 3", "tag 4"], RatingsList = [1, 2], RatingsArray = [3, 4] });
await ctx.SaveChangesAsync();

var q = ctx.Blogs.Select(b => new
{
    b.Name,
    b.TagsList,
    b.TagsArray,
    ListTagsJoined = string.Join(", ", b.TagsList), // the translation of this seems wrong
    ArrayTagsJoined = string.Join(", ", b.TagsArray),
    b.RatingsList,
    b.RatingsArray,
    ListRatingsJoined = string.Join(", ", b.RatingsList),
    ArrayRatingsJoined = string.Join(", ", b.RatingsArray),
});

// The projection for ListTagsJoined = string.Join(", ", b.TagsList) is left out
// because it seems to be detected as a duplicate of b.TagsList (without any shaping function).
Console.WriteLine(q.ToQueryString());
Console.WriteLine();

// One might not notice the inconsistency with this query if just looking at the resulting entities,
// because what we queried above is actually what we get here - it seems to be done client side though.
var lst = await q.ToListAsync();
Console.WriteLine(JsonSerializer.Serialize(lst));
Console.WriteLine("----");

var qWorksAsExpected = q.Where(x => x.ArrayTagsJoined.Contains("tag"));
Console.WriteLine(qWorksAsExpected.ToQueryString());
Console.WriteLine();
var lst2 = await qWorksAsExpected.ToListAsync();
Console.WriteLine(JsonSerializer.Serialize(lst2));
Console.WriteLine("----");

var qShouldWorkButThrows = q.Where(x => x.ListTagsJoined.Contains("tag"));
// This throws! If I understand https://www.npgsql.org/efcore/mapping/array.html correctly, it should be the same as qWorksAsExpected.
Console.WriteLine(qShouldWorkButThrows.ToQueryString());
Console.WriteLine();
var lst3 = await qShouldWorkButThrows.ToListAsync();
Console.WriteLine(JsonSerializer.Serialize(lst3));

public class ReproContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(@"Host=localhost;Username=postgres;Password=postgres;Database=npgsql_efcore_array_translation_repro");
}

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> TagsList { get; set; }
    public string[] TagsArray { get; set; }
    public List<int> RatingsList { get; set; }
    public int[] RatingsArray { get; set; }
}