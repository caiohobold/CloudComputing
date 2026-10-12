using BibliotecaApi.Data;
using BibliotecaApi.Dtos;
using BibliotecaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaApi.Controllers;

[ApiController]
[Route("api/livros")]
public class LivrosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Livro>>> Listar([FromQuery] string? busca)
    {
        var query = db.Livros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(l => EF.Functions.ILike(l.Titulo, $"%{busca}%") || EF.Functions.ILike(l.Autor, $"%{busca}%"));

        return await query.OrderBy(l => l.Id).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Livro>> Obter(int id)
    {
        var livro = await db.Livros.FindAsync(id);
        return livro is null ? NotFound() : livro;
    }

    [HttpPost]
    public async Task<ActionResult<Livro>> Criar(LivroDto dto)
    {
        var livro = new Livro
        {
            Titulo = dto.Titulo,
            Autor = dto.Autor,
            AnoPublicacao = dto.AnoPublicacao,
            Genero = dto.Genero
        };

        db.Livros.Add(livro);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obter), new { id = livro.Id }, livro);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Livro>> Atualizar(int id, LivroDto dto)
    {
        var livro = await db.Livros.FindAsync(id);
        if (livro is null) return NotFound();

        livro.Titulo = dto.Titulo;
        livro.Autor = dto.Autor;
        livro.AnoPublicacao = dto.AnoPublicacao;
        livro.Genero = dto.Genero;
        livro.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return livro;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var livro = await db.Livros.FindAsync(id);
        if (livro is null) return NotFound();

        db.Livros.Remove(livro);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
