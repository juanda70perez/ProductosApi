using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=productos.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

var productos = app.MapGroup("/productos");

productos.MapGet("/", async (AppDbContext db) => await db.Productos.ToListAsync());

productos.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Productos.FindAsync(id) is Producto p ? Results.Ok(p) : Results.NotFound());

productos.MapPost("/", async (ProductoDto dto, AppDbContext db) =>
{
    var p = new Producto { Nombre = dto.Nombre, Precio = dto.Precio };
    db.Productos.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{p.Id}", p);
});

productos.MapPut("/{id:int}", async (int id, ProductoDto dto, AppDbContext db) =>
{
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.Nombre = dto.Nombre;
    p.Precio = dto.Precio;
    await db.SaveChangesAsync();
    return Results.Ok(p);
});

productos.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Productos.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}

public record ProductoDto(string Nombre, decimal Precio);

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
}
