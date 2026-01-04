using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=links.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
app.MapGet("/", () => "Hello World!");

app.MapPost("/shorten", async (UrlRequest request, AppDbContext db) =>
{
    var Random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var ShortCode = new string(Enumerable.Repeat(chars, 6)
        .Select(s => s[Random.Next(s.Length)]).ToArray());
    var shortUrl = new ShortUrl
    {
        OriginalUrl = request.Url,
        ShortCode = ShortCode
    };
    db.ShortUrls.Add(shortUrl);
    await db.SaveChangesAsync();
    return Results.Ok(new { shortUrl = $"http://localhost:5123/{ShortCode}" });

});


app.MapGet("/{code}", async (string code, AppDbContext db) =>
{
    var link = await db.ShortUrls.FirstOrDefaultAsync(x => x.ShortCode == code);

    if (link is null)
    {
        // Every path MUST return an IResult
        return Results.NotFound("Code not found");
    }

    // This returns a Redirect result, which satisfies the Task<IResult> requirement
    return Results.Redirect(link.OriginalUrl);
});
app.Run();


public class ShortUrl
{
    public int Id { get; set; }
    public string OriginalUrl { get; set; } =string.Empty;
    public string ShortCode { get; set; } =string.Empty;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShortUrl> ShortUrls { get; set; }
}

public record UrlRequest(string Url);